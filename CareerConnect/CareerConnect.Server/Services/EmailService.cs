using System.Net;
using System.Net.Mail;

namespace CareerConnect.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationCodeAsync(string email, string code, string verificationType)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"]
                    ?? throw new InvalidOperationException("SMTP Server not configured");
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"]
                    ?? throw new InvalidOperationException("Sender Email not configured");
                var senderPassword = _configuration["Email:SenderPassword"]
                    ?? throw new InvalidOperationException("Sender Password not configured");
                var senderName = _configuration["Email:SenderName"] ?? "CareerConnect";

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    Timeout = 30000 // 30 secunde
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"Codul tău de verificare CareerConnect - {code}",
                    Body = GetEmailBody(code, verificationType),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Verification code sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification code to {email}");
                throw new InvalidOperationException("Nu s-a putut trimite email-ul de verificare. Vă rugăm încercați din nou.");
            }
        }

        private string GetEmailBody(string code, string verificationType)
        {
            var actionText = verificationType == "Login" ? "autentificarea" : "înregistrarea";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .code-box {{ background-color: white; border: 2px solid #4F46E5; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0; }}
        .code {{ font-size: 32px; font-weight: bold; color: #4F46E5; letter-spacing: 8px; }}
        .warning {{ background-color: #FEF3C7; border-left: 4px solid #F59E0B; padding: 12px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6B7280; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CareerConnect</h1>
        </div>
        <div class='content'>
            <h2>Cod de Verificare</h2>
            <p>Bună ziua,</p>
            <p>Ai solicitat {actionText} pe platforma CareerConnect. Folosește codul de mai jos pentru a continua:</p>
            
            <div class='code-box'>
                <div class='code'>{code}</div>
            </div>
            
            <p><strong>Codul este valabil 10 minute.</strong></p>
            
            <div class='warning'>
                <strong>⚠️ Important:</strong> Dacă nu ai solicitat acest cod, te rugăm să ignori acest email. 
                Nu distribui niciodată codul tău de verificare cu alte persoane.
            </div>
            
            <p>Dacă ai întâmpina probleme, contactează echipa noastră de suport.</p>
            
            <p>Cu respect,<br>Echipa CareerConnect</p>
        </div>
        <div class='footer'>
            <p>Acest email a fost generat automat. Te rugăm să nu răspunzi la acest mesaj.</p>
            <p>&copy; 2025 CareerConnect. Toate drepturile rezervate.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}