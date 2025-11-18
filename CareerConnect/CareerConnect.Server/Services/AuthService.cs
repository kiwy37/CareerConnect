using CareerConnect.Server.DTOs;
using CareerConnect.Server.Helpers;
using CareerConnect.Server.Models;
using CareerConnect.Server.Repositories;
using Google.Apis.Auth;
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

        // ==================== LOGIN ====================
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

        // ==================== REGISTER ====================
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

        // ==================== FORGOT PASSWORD ====================
        public async Task<PendingVerificationDto> InitiateForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? ipAddress = null)
        {
            var user = await _userRepository.GetByEmailAsync(forgotPasswordDto.Email);

            if (user == null)
                throw new KeyNotFoundException("Nu există niciun cont asociat cu acest email");

            if (user.Parola == null)
                throw new InvalidOperationException("Acest cont folosește autentificare socială și nu are parolă. Vă rugăm să vă autentificați prin provider-ul social.");

            await _verificationService.GenerateAndSendCodeAsync(forgotPasswordDto.Email, "ResetPassword", ipAddress);

            _logger.LogInformation($"Password reset initiated for {forgotPasswordDto.Email}");

            return new PendingVerificationDto
            {
                Email = forgotPasswordDto.Email,
                Message = "Un cod de verificare a fost trimis pe email pentru resetarea parolei.",
                RequiresVerification = true
            };
        }

        public async Task<bool> VerifyResetCodeAsync(VerifyResetCodeDto verifyResetCodeDto)
        {
            var isValid = await _verificationService.ValidateCodeAsync(
                verifyResetCodeDto.Email,
                verifyResetCodeDto.Code,
                "ResetPassword");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid sau expirat");

            _logger.LogInformation($"Reset code verified for {verifyResetCodeDto.Email}");

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var isValid = await _verificationService.ValidateCodeAsync(
                resetPasswordDto.Email,
                resetPasswordDto.Code,
                "ResetPassword");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid sau expirat");

            var user = await _userRepository.GetByEmailAsync(resetPasswordDto.Email);

            if (user == null)
                throw new KeyNotFoundException("Utilizatorul nu a fost găsit");

            user.Parola = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation($"Password reset completed for {resetPasswordDto.Email}");

            return true;
        }

        // ==================== GOOGLE LOGIN ====================
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

        // ==================== LINKEDIN LOGIN ====================
        public async Task<AuthResponseDto> LinkedInLoginAsync(LinkedInLoginDto linkedInLoginDto)
        {
            try
            {
                var clientId = _configuration["Authentication:LinkedIn:ClientId"];
                var clientSecret = _configuration["Authentication:LinkedIn:ClientSecret"];
                var redirectUri = _configuration["Authentication:LinkedIn:RedirectUri"];

                _logger.LogInformation($"LinkedIn login attempt with code: {linkedInLoginDto.Code.Substring(0, Math.Min(20, linkedInLoginDto.Code.Length))}...");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("LinkedIn authentication is not configured properly");
                    throw new InvalidOperationException("LinkedIn authentication is not configured");
                }

                var httpClient = _httpClientFactory.CreateClient();

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
                    _logger.LogError($"LinkedIn token error ({tokenResponse.StatusCode}): {errorContent}");
                    throw new UnauthorizedAccessException($"Failed to get access token from LinkedIn: {errorContent}");
                }

                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tokenContent);

                if (tokenData == null || !tokenData.ContainsKey("access_token"))
                {
                    _logger.LogError("Invalid token response - no access_token found");
                    throw new UnauthorizedAccessException("Invalid token response from LinkedIn");
                }

                var accessToken = tokenData["access_token"].GetString();

                var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
                profileRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var profileResponse = await httpClient.SendAsync(profileRequest);

                if (!profileResponse.IsSuccessStatusCode)
                {
                    var errorContent = await profileResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"LinkedIn profile error ({profileResponse.StatusCode}): {errorContent}");
                    throw new UnauthorizedAccessException($"Failed to get user profile from LinkedIn: {errorContent}");
                }

                var profileContent = await profileResponse.Content.ReadAsStringAsync();
                var profileData = JsonSerializer.Deserialize<LinkedInUserInfo>(profileContent);

                if (profileData == null)
                {
                    _logger.LogError("Failed to deserialize LinkedIn profile data");
                    throw new UnauthorizedAccessException("Invalid profile data from LinkedIn");
                }

                var email = profileData.Email;
                if (string.IsNullOrEmpty(email))
                {
                    email = $"linkedin_{profileData.Sub}@careerconnect.temp";
                    _logger.LogWarning($"No email from LinkedIn, using temporary: {email}");
                }

                var user = await _userRepository.GetByLinkedInIdAsync(profileData.Sub);

                if (user == null)
                {
                    user = await _userRepository.GetByEmailAsync(email);

                    if (user != null)
                    {
                        user.LinkedInId = profileData.Sub;
                        await _userRepository.UpdateAsync(user);
                    }
                    else
                    {
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

                _logger.LogInformation($"LinkedIn login completed successfully for {email}");

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

        // ==================== SOCIAL LOGIN ====================
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

        // ==================== FACEBOOK LOGIN HANDLER ====================
        private async Task<User> HandleFacebookLoginAsync(SocialLoginDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Email) &&
                    !string.IsNullOrEmpty(dto.FirstName) &&
                    !string.IsNullOrEmpty(dto.LastName) &&
                    !string.IsNullOrEmpty(dto.ProviderId))
                {
                    _logger.LogInformation($"Facebook login with provided data: {dto.Email}");

                    var user = await _userRepository.GetByFacebookIdAsync(dto.ProviderId);

                    if (user == null)
                    {
                        user = await _userRepository.GetByEmailAsync(dto.Email);

                        if (user != null)
                        {
                            user.FacebookId = dto.ProviderId;
                            await _userRepository.UpdateAsync(user);
                        }
                        else
                        {
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
                var userData = JsonSerializer.Deserialize<FacebookUserData>(
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

        // ==================== TWITTER LOGIN HANDLER ====================
        private async Task<User> HandleTwitterLoginAsync(SocialLoginDto dto)
        {
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

        // ==================== LINKEDIN LOGIN HANDLER ====================
        private async Task<User> HandleLinkedInLoginAsync(SocialLoginDto dto)
        {
            try
            {
                _logger.LogInformation($"LinkedIn login attempt");

                if (!string.IsNullOrEmpty(dto.Email) &&
                    !string.IsNullOrEmpty(dto.FirstName) &&
                    !string.IsNullOrEmpty(dto.LastName) &&
                    !string.IsNullOrEmpty(dto.ProviderId))
                {
                    _logger.LogInformation($"LinkedIn login with provided data: {dto.Email}");

                    var user = await _userRepository.GetByLinkedInIdAsync(dto.ProviderId);

                    if (user == null)
                    {
                        user = await _userRepository.GetByEmailAsync(dto.Email);

                        if (user != null)
                        {
                            _logger.LogInformation($"Linking LinkedIn to existing user: {dto.Email}");
                            user.LinkedInId = dto.ProviderId;
                            await _userRepository.UpdateAsync(user);
                        }
                        else
                        {
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

                throw new InvalidOperationException("LinkedIn user data is required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleLinkedInLoginAsync");
                throw new UnauthorizedAccessException("LinkedIn authentication failed: " + ex.Message);
            }
        }

        // ==================== RESEND CODE ====================
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