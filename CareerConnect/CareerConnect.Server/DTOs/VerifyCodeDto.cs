using System.ComponentModel.DataAnnotations;

namespace CareerConnect.Server.DTOs
{
    public class VerifyCodeDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Codul este obligatoriu")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Codul trebuie să conțină exact 6 cifre")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Codul trebuie să conțină doar cifre")]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string VerificationType { get; set; } = string.Empty; // "Login", "Register", sau "ResetPassword"
    }
}