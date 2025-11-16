using CareerConnect.Server.DTOs;
using CareerConnect.Server.Helpers;
using CareerConnect.Server.Models;
using CareerConnect.Server.Repositories;
using Google.Apis.Auth;
using System.Net.Http;
using System.Text.Json;

namespace CareerConnect.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVerificationService _verificationService;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(
            IUserRepository userRepository,
            IVerificationService verificationService,
            JwtHelper jwtHelper,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository;
            _verificationService = verificationService;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PendingVerificationDto> InitiateLoginAsync(LoginDto loginDto, string? ipAddress = null)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Email sau parolă incorectă");

            if (user.Parola == null)
                throw new UnauthorizedAccessException("Acest cont este asociat cu un provider social. Vă rugăm autentificați-vă prin acel provider.");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Parola, user.Parola))
                throw new UnauthorizedAccessException("Email sau parolă incorectă");

            await _verificationService.GenerateAndSendCodeAsync(loginDto.Email, "Login", ipAddress);

            _logger.LogInformation($"Login initiated for {loginDto.Email}");

            return new PendingVerificationDto
            {
                Email = loginDto.Email,
                Message = "Un cod de verificare a fost trimis pe email. Vă rugăm introduceți codul pentru a continua.",
                RequiresVerification = true
            };
        }

        public async Task<AuthResponseDto> CompleteLoginAsync(VerifyCodeDto verifyCodeDto)
        {
            var isValid = await _verificationService.ValidateCodeAsync(
                verifyCodeDto.Email,
                verifyCodeDto.Code,
                "Login");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid");

            var user = await _userRepository.GetByEmailAsync(verifyCodeDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Utilizatorul nu a fost găsit");

            var token = _jwtHelper.GenerateToken(user);

            _logger.LogInformation($"Login completed for {verifyCodeDto.Email}");

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<PendingVerificationDto> InitiateRegisterAsync(CreateUserDto createUserDto, string? ipAddress = null)
        {
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new InvalidOperationException("Email-ul este deja înregistrat");

            await _verificationService.GenerateAndSendCodeAsync(createUserDto.Email, "Register", ipAddress);

            _logger.LogInformation($"Registration initiated for {createUserDto.Email}");

            return new PendingVerificationDto
            {
                Email = createUserDto.Email,
                Message = "Un cod de verificare a fost trimis pe email. Vă rugăm introduceți codul pentru a finaliza înregistrarea.",
                RequiresVerification = true
            };
        }

        public async Task<AuthResponseDto> FinalizeRegisterWithVerificationAsync(CreateUserWithCodeDto createUserDto)
        {
            var isValid = await _verificationService.ValidateCodeAsync(
                createUserDto.Email,
                createUserDto.Code,
                "Register");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid");

            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new InvalidOperationException("Email-ul este deja înregistrat");

            var user = new User
            {
                Email = createUserDto.Email,
                Parola = BCrypt.Net.BCrypt.HashPassword(createUserDto.Parola),
                Nume = createUserDto.Nume,
                Prenume = createUserDto.Prenume,
                Telefon = createUserDto.Telefon,
                DataNastere = createUserDto.DataNastere,
                RolId = createUserDto.RolId,
                CreatedAt = DateTime.UtcNow
            };

            user = await _userRepository.CreateAsync(user);
            user = await _userRepository.GetByIdAsync(user.Id);

            var token = _jwtHelper.GenerateToken(user!);

            _logger.LogInformation($"Registration completed for {createUserDto.Email}");

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user!)
            };
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken, settings);
            }
            catch
            {
                throw new UnauthorizedAccessException("Token Google invalid");
            }

            var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);

            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(payload.Email);

                if (user != null)
                {
                    user.GoogleId = payload.Subject;
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    user = new User
                    {
                        Email = payload.Email,
                        GoogleId = payload.Subject,
                        Nume = payload.FamilyName ?? "",
                        Prenume = payload.GivenName ?? "",
                        RolId = 2,
                        DataNastere = DateTime.UtcNow.AddYears(-18),
                        CreatedAt = DateTime.UtcNow
                    };

                    user = await _userRepository.CreateAsync(user);
                    user = await _userRepository.GetByIdAsync(user.Id);
                }
            }

            var token = _jwtHelper.GenerateToken(user!);

            _logger.LogInformation($"Google login completed for {payload.Email}");

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user!)
            };
        }

        public async Task<AuthResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto)
        {
            User? user = null;

            switch (socialLoginDto.Provider)
            {
                case "Facebook":
                    user = await HandleFacebookLoginAsync(socialLoginDto);
                    break;
                case "Twitter":
                    user = await HandleTwitterLoginAsync(socialLoginDto);
                    break;
                case "LinkedIn":
                    user = await HandleLinkedInLoginAsync(socialLoginDto);
                    break;
                default:
                    throw new InvalidOperationException("Provider necunoscut");
            }

            var token = _jwtHelper.GenerateToken(user);

            _logger.LogInformation($"{socialLoginDto.Provider} login completed for {user.Email}");

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user)
            };
        }

        private async Task<User> HandleFacebookLoginAsync(SocialLoginDto dto)
        {
            try
            {
                // Check if basic data is provided from the frontend
                if (!string.IsNullOrEmpty(dto.Email) &&
                    !string.IsNullOrEmpty(dto.FirstName) &&
                    !string.IsNullOrEmpty(dto.LastName) &&
                    !string.IsNullOrEmpty(dto.ProviderId))
                {
                    _logger.LogInformation($"Facebook login with provided data: {dto.Email}");

                    // Use data directly from DTO (already validated by Angular)
                    var user = await _userRepository.GetByFacebookIdAsync(dto.ProviderId);

                    if (user == null)
                    {
                        // Check if user exists by email
                        user = await _userRepository.GetByEmailAsync(dto.Email);

                        if (user != null)
                        {
                            // Link Facebook to existing account
                            user.FacebookId = dto.ProviderId;
                            await _userRepository.UpdateAsync(user);
                        }
                        else
                        {
                            // Create new user
                            user = new User
                            {
                                Email = dto.Email,
                                FacebookId = dto.ProviderId,
                                Nume = dto.LastName,
                                Prenume = dto.FirstName,
                                RolId = 2,
                                DataNastere = DateTime.UtcNow.AddYears(-18),
                                CreatedAt = DateTime.UtcNow
                            };

                            user = await _userRepository.CreateAsync(user);
                            user = await _userRepository.GetByIdAsync(user.Id);
                        }
                    }

                    return user!;
                }

                // Fallback: Verify token with Facebook API
                _logger.LogInformation("Verifying Facebook token with Facebook API");
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(
                    $"https://graph.facebook.com/me?access_token={dto.AccessToken}&fields=id,email,first_name,last_name"
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Facebook API error: {errorContent}");
                    throw new UnauthorizedAccessException("Token Facebook invalid");
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Facebook API response: {content}");

                var userData = await JsonSerializer.DeserializeAsync<FacebookUserData>(
                    await response.Content.ReadAsStreamAsync()
                );

                if (userData == null || string.IsNullOrEmpty(userData.Email))
                {
                    throw new UnauthorizedAccessException("Nu s-au putut obține datele de la Facebook");
                }

                var existingUser = await _userRepository.GetByFacebookIdAsync(userData.Id);

                if (existingUser == null)
                {
                    existingUser = await _userRepository.GetByEmailAsync(userData.Email);

                    if (existingUser != null)
                    {
                        existingUser.FacebookId = userData.Id;
                        await _userRepository.UpdateAsync(existingUser);
                    }
                    else
                    {
                        existingUser = new User
                        {
                            Email = userData.Email,
                            FacebookId = userData.Id,
                            Nume = userData.Last_Name,
                            Prenume = userData.First_Name,
                            RolId = 2,
                            DataNastere = DateTime.UtcNow.AddYears(-18),
                            CreatedAt = DateTime.UtcNow
                        };

                        existingUser = await _userRepository.CreateAsync(existingUser);
                        existingUser = await _userRepository.GetByIdAsync(existingUser.Id);
                    }
                }

                return existingUser!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleFacebookLoginAsync");
                throw;
            }
        }

        private async Task<User> HandleTwitterLoginAsync(SocialLoginDto dto)
        {
            // Similar cu Facebook, dar cu Twitter API
            var user = await _userRepository.GetByEmailAsync(dto.Email!);

            if (user == null)
            {
                user = new User
                {
                    Email = dto.Email!,
                    TwitterId = dto.ProviderId,
                    Nume = dto.LastName ?? "",
                    Prenume = dto.FirstName ?? "",
                    RolId = 2,
                    DataNastere = DateTime.UtcNow.AddYears(-18),
                    CreatedAt = DateTime.UtcNow
                };

                user = await _userRepository.CreateAsync(user);
            }
            else if (string.IsNullOrEmpty(user.TwitterId))
            {
                user.TwitterId = dto.ProviderId;
                await _userRepository.UpdateAsync(user);
            }

            return user;
        }

        public string GetTwitterOAuthUrl()
        {
            var clientId = _configuration["Authentication:Twitter:ClientId"];
            var redirectUri = _configuration["Authentication:Twitter:RedirectUri"];
            return $"https://twitter.com/i/oauth2/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=tweet.read%20users.read&state=state";
        }

        public async Task<AuthResponseDto> HandleTwitterCallbackAsync(string oauthToken, string oauthVerifier)
        {
            // Implementează logica OAuth pentru Twitter
            // Acesta este un exemplu simplificat
            throw new NotImplementedException("Twitter OAuth callback trebuie implementat complet");
        }

        public string GetLinkedInOAuthUrl()
        {
            var clientId = _configuration["Authentication:LinkedIn:ClientId"];
            var redirectUri = _configuration["Authentication:LinkedIn:RedirectUri"];
            return $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=r_liteprofile%20r_emailaddress";
        }

        private async Task<User> HandleLinkedInLoginAsync(SocialLoginDto dto)
        {
            try
            {
                _logger.LogInformation($"LinkedIn login attempt");

                // If we have all user data from frontend, use it directly
                if (!string.IsNullOrEmpty(dto.Email) &&
                    !string.IsNullOrEmpty(dto.FirstName) &&
                    !string.IsNullOrEmpty(dto.LastName) &&
                    !string.IsNullOrEmpty(dto.ProviderId))
                {
                    _logger.LogInformation($"LinkedIn login with provided data: {dto.Email}");

                    var user = await _userRepository.GetByLinkedInIdAsync(dto.ProviderId);

                    if (user == null)
                    {
                        // Check if user exists by email
                        user = await _userRepository.GetByEmailAsync(dto.Email);

                        if (user != null)
                        {
                            // Link LinkedIn to existing account
                            _logger.LogInformation($"Linking LinkedIn to existing user: {dto.Email}");
                            user.LinkedInId = dto.ProviderId;
                            await _userRepository.UpdateAsync(user);
                        }
                        else
                        {
                            // Create new user
                            _logger.LogInformation($"Creating new user from LinkedIn: {dto.Email}");
                            user = new User
                            {
                                Email = dto.Email,
                                LinkedInId = dto.ProviderId,
                                Nume = dto.LastName ?? "User",
                                Prenume = dto.FirstName ?? "LinkedIn",
                                RolId = 2,
                                DataNastere = DateTime.UtcNow.AddYears(-18),
                                CreatedAt = DateTime.UtcNow
                            };

                            user = await _userRepository.CreateAsync(user);
                            user = await _userRepository.GetByIdAsync(user.Id);
                        }
                    }

                    _logger.LogInformation($"LinkedIn login successful for: {user!.Email}");
                    return user!;
                }

                // If no data provided, we need to exchange the access token
                // This requires making API calls to LinkedIn
                throw new InvalidOperationException("LinkedIn user data is required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleLinkedInLoginAsync");
                throw new UnauthorizedAccessException("LinkedIn authentication failed: " + ex.Message);
            }
        }

        public async Task<AuthResponseDto> LinkedInLoginAsync(LinkedInLoginDto linkedInLoginDto)
        {
            try
            {
                var clientId = _configuration["Authentication:LinkedIn:ClientId"];
                var clientSecret = _configuration["Authentication:LinkedIn:ClientSecret"];
                var redirectUri = _configuration["Authentication:LinkedIn:RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("LinkedIn authentication is not configured");
                }

                var httpClient = _httpClientFactory.CreateClient();

                // Step 1: Exchange authorization code for access token
                var tokenRequestData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", linkedInLoginDto.Code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri }
        };

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
                {
                    Content = new FormUrlEncodedContent(tokenRequestData)
                };

                var tokenResponse = await httpClient.SendAsync(tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"LinkedIn token error: {errorContent}");
                    throw new UnauthorizedAccessException("Failed to get access token from LinkedIn");
                }

                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tokenContent);

                if (tokenData == null || !tokenData.ContainsKey("access_token"))
                {
                    throw new UnauthorizedAccessException("Invalid token response from LinkedIn");
                }

                var accessToken = tokenData["access_token"].GetString();

                // Step 2: Get user profile using the access token
                var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
                profileRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var profileResponse = await httpClient.SendAsync(profileRequest);

                if (!profileResponse.IsSuccessStatusCode)
                {
                    var errorContent = await profileResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"LinkedIn profile error: {errorContent}");
                    throw new UnauthorizedAccessException("Failed to get user profile from LinkedIn");
                }

                var profileContent = await profileResponse.Content.ReadAsStringAsync();
                _logger.LogInformation($"LinkedIn profile response: {profileContent}");

                var profileData = JsonSerializer.Deserialize<LinkedInUserInfo>(profileContent);

                if (profileData == null)
                {
                    throw new UnauthorizedAccessException("Invalid profile data from LinkedIn");
                }

                // Step 3: Create or find user
                var email = profileData.Email;
                if (string.IsNullOrEmpty(email))
                {
                    email = $"linkedin_{profileData.Sub}@careerconnect.temp";
                    _logger.LogInformation($"No email from LinkedIn, using temporary: {email}");
                }

                var user = await _userRepository.GetByLinkedInIdAsync(profileData.Sub);

                if (user == null)
                {
                    user = await _userRepository.GetByEmailAsync(email);

                    if (user != null)
                    {
                        // Link LinkedIn to existing account
                        user.LinkedInId = profileData.Sub;
                        await _userRepository.UpdateAsync(user);
                    }
                    else
                    {
                        // Create new user
                        user = new User
                        {
                            Email = email,
                            LinkedInId = profileData.Sub,
                            Nume = profileData.FamilyName ?? "User",
                            Prenume = profileData.GivenName ?? "LinkedIn",
                            RolId = 2,
                            DataNastere = DateTime.UtcNow.AddYears(-18),
                            CreatedAt = DateTime.UtcNow
                        };

                        user = await _userRepository.CreateAsync(user);
                        user = await _userRepository.GetByIdAsync(user.Id);
                    }
                }

                var token = _jwtHelper.GenerateToken(user!);

                _logger.LogInformation($"LinkedIn login completed for {email}");

                return new AuthResponseDto
                {
                    Token = token,
                    User = MapToUserDto(user!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkedIn login error");
                throw new UnauthorizedAccessException($"LinkedIn authentication failed: {ex.Message}");
            }
        }

        public async Task ResendVerificationCodeAsync(ResendCodeDto resendCodeDto, string? ipAddress = null)
        {
            await _verificationService.GenerateAndSendCodeAsync(
                resendCodeDto.Email,
                resendCodeDto.VerificationType,
                ipAddress);

            _logger.LogInformation($"Verification code resent for {resendCodeDto.Email}");
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Nume = user.Nume,
                Prenume = user.Prenume,
                Telefon = user.Telefon,
                DataNastere = user.DataNastere,
                RolNume = user.Rol.Nume,
                CreatedAt = user.CreatedAt
            };
        }
    }
}