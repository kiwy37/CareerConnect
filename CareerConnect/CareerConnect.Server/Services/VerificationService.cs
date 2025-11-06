using CareerConnect.Server.Data;
using CareerConnect.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CareerConnect.Server.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerificationService> _logger;
        private const int CODE_LENGTH = 6;
        private const int CODE_EXPIRATION_MINUTES = 10;
        private const int MAX_ATTEMPTS = 5;
        private const int MAX_CODES_PER_EMAIL_PER_HOUR = 3;

        public VerificationService(
            AppDbContext context,
            IEmailService emailService,
            ILogger<VerificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<string> GenerateAndSendCodeAsync(string email, string verificationType, string? ipAddress = null)
        {
            // Verificăm rate limiting - max 3 coduri pe oră per email
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentCodesCount = await _context.EmailVerificationCodes
                .CountAsync(c => c.Email == email && c.CreatedAt > oneHourAgo);

            if (recentCodesCount >= MAX_CODES_PER_EMAIL_PER_HOUR)
            {
                throw new InvalidOperationException(
                    "Ai depășit limita de coduri de verificare. Te rugăm să încerci din nou în 1 oră.");
            }

            // Invalidăm toate codurile vechi nefolosite pentru acest email și tip
            var oldCodes = await _context.EmailVerificationCodes
                .Where(c => c.Email == email
                    && c.VerificationType == verificationType
                    && !c.IsUsed)
                .ToListAsync();

            foreach (var oldCode in oldCodes)
            {
                oldCode.IsUsed = true;
            }

            // Generăm un cod securizat
            var code = GenerateSecureCode();

            // Creăm noul cod de verificare
            var verificationCode = new EmailVerificationCode
            {
                Email = email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CODE_EXPIRATION_MINUTES),
                VerificationType = verificationType,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailVerificationCodes.Add(verificationCode);
            await _context.SaveChangesAsync();

            // Trimitem email-ul
            await _emailService.SendVerificationCodeAsync(email, code, verificationType);

            _logger.LogInformation($"Verification code generated for {email} - Type: {verificationType}");

            return code;
        }

        public async Task<bool> ValidateCodeAsync(string email, string code, string verificationType)
        {
            // Găsim codul cel mai recent pentru acest email și tip
            var verificationCode = await _context.EmailVerificationCodes
                .Where(c => c.Email == email
                    && c.VerificationType == verificationType
                    && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (verificationCode == null)
            {
                _logger.LogWarning($"No verification code found for {email}");
                return false;
            }

            // Verificăm numărul de încercări
            verificationCode.AttemptCount++;
            await _context.SaveChangesAsync();

            if (verificationCode.AttemptCount > MAX_ATTEMPTS)
            {
                verificationCode.IsUsed = true;
                await _context.SaveChangesAsync();

                _logger.LogWarning($"Max attempts exceeded for {email}");
                throw new InvalidOperationException(
                    "Ai depășit numărul maxim de încercări. Te rugăm să soliciți un cod nou.");
            }

            // Verificăm expirarea
            if (DateTime.UtcNow > verificationCode.ExpiresAt)
            {
                verificationCode.IsUsed = true;
                await _context.SaveChangesAsync();

                _logger.LogWarning($"Expired verification code for {email}");
                throw new InvalidOperationException("Codul de verificare a expirat. Te rugăm să soliciți unul nou.");
            }

            // Verificăm codul
            if (verificationCode.Code != code)
            {
                _logger.LogWarning($"Invalid code attempt for {email}");
                return false;
            }

            // Marcăm codul ca folosit
            verificationCode.IsUsed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Code validated successfully for {email}");
            return true;
        }

        public async Task CleanupExpiredCodesAsync()
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var expiredCodes = await _context.EmailVerificationCodes
                .Where(c => c.CreatedAt < oneDayAgo)
                .ToListAsync();

            if (expiredCodes.Any())
            {
                _context.EmailVerificationCodes.RemoveRange(expiredCodes);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cleaned up {expiredCodes.Count} expired verification codes");
            }
        }

        private string GenerateSecureCode()
        {
            // Generăm un cod de 6 cifre folosind RNG criptografic
            var code = string.Empty;

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];

                while (code.Length < CODE_LENGTH)
                {
                    rng.GetBytes(bytes);
                    var number = BitConverter.ToUInt32(bytes, 0);
                    var digit = (number % 10).ToString();
                    code += digit;
                }
            }

            return code;
        }
    }
}