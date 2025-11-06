using CareerConnect.Server.DTOs;
using CareerConnect.Server.Helpers;
using CareerConnect.Server.Models;
using CareerConnect.Server.Repositories;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

namespace CareerConnect.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVerificationService _verificationService;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IVerificationService verificationService,
            JwtHelper jwtHelper,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _verificationService = verificationService;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PendingVerificationDto> InitiateLoginAsync(LoginDto loginDto, string? ipAddress = null)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Email sau parolă incorectă");

            if (user.Parola == null)
                throw new UnauthorizedAccessException("Acest cont este asociat cu Google. Vă rugăm autentificați-vă cu Google.");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Parola, user.Parola))
                throw new UnauthorizedAccessException("Email sau parolă incorectă");

            // Generăm și trimitem codul de verificare
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
            // Validăm codul
            var isValid = await _verificationService.ValidateCodeAsync(
                verifyCodeDto.Email,
                verifyCodeDto.Code,
                "Login");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid");

            // Obținem userul și generăm token
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

            // Generăm și trimitem codul de verificare
            await _verificationService.GenerateAndSendCodeAsync(createUserDto.Email, "Register", ipAddress);

            _logger.LogInformation($"Registration initiated for {createUserDto.Email}");

            return new PendingVerificationDto
            {
                Email = createUserDto.Email,
                Message = "Un cod de verificare a fost trimis pe email. Vă rugăm introduceți codul pentru a finaliza înregistrarea.",
                RequiresVerification = true
            };
        }

        public async Task<AuthResponseDto> CompleteRegisterAsync(VerifyCodeDto verifyCodeDto)
        {
            // Validăm codul
            var isValid = await _verificationService.ValidateCodeAsync(
                verifyCodeDto.Email,
                verifyCodeDto.Code,
                "Register");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid");

            // IMPORTANT: Trebuie să stocăm temporar datele de înregistrare
            // În acest caz, vom cere frontend-ul să trimită din nou datele complete
            // sau să le stocăm într-un cache/sesiune temporară

            // Pentru moment, vom returna un răspuns care indică succesul verificării
            // Frontend-ul va trebui să apeleze un endpoint final cu toate datele

            throw new NotImplementedException(
                "Pentru a finaliza înregistrarea, frontend-ul trebuie să trimită datele complete după verificarea codului. " +
                "Consideră crearea unui endpoint separat pentru finalizarea înregistrării.");
        }

        // Metodă pentru finalizarea înregistrării cu verificarea codului
        public async Task<AuthResponseDto> FinalizeRegisterWithVerificationAsync(CreateUserWithCodeDto createUserDto)
        {
            // Validăm codul
            var isValid = await _verificationService.ValidateCodeAsync(
                createUserDto.Email,
                createUserDto.Code,
                "Register");

            if (!isValid)
                throw new UnauthorizedAccessException("Cod de verificare invalid");

            // Verificăm dacă email-ul există deja
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
                        RolId = 2, // default: angajat
                        DataNastere = DateTime.UtcNow.AddYears(-18),
                        CreatedAt = DateTime.UtcNow
                    };

                    user = await _userRepository.CreateAsync(user);
                    user = await _userRepository.GetByIdAsync(user.Id);
                }
            }

            var token = _jwtHelper.GenerateToken(user!);

            // Google login nu necesită verificare prin email suplimentară
            _logger.LogInformation($"Google login completed for {payload.Email}");

            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user!)
            };
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