using System.ComponentModel.DataAnnotations;

namespace CareerConnect.Server.DTOs
{
    public class ResendCodeDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string VerificationType { get; set; } = string.Empty;
    }
}
