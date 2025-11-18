using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CareerConnect.Server.DTOs
{
    // ==================== Social Login DTOs ====================
    public class SocialLoginDto
    {
        [Required]
        public string Provider { get; set; } = string.Empty; // "Google", "Facebook", "Twitter", "LinkedIn"

        [Required]
        public string AccessToken { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProviderId { get; set; }
    }

    public class FacebookUserData
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string First_Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = string.Empty;
    }

    public class TwitterUserData
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // ==================== LinkedIn DTOs ====================
    public class LinkedInLoginDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class LinkedInUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string Picture { get; set; } = string.Empty;
    }

    // ==================== Registration DTOs ====================
    public class CreateUserWithCodeDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress(ErrorMessage = "Format email invalid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola este obligatorie")]
        [MinLength(6, ErrorMessage = "Parola trebuie să conțină minim 6 caractere")]
        public string Parola { get; set; } = string.Empty;

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [MaxLength(100)]
        public string Nume { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [MaxLength(100)]
        public string Prenume { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format telefon invalid")]
        public string? Telefon { get; set; }

        [Required(ErrorMessage = "Data nașterii este obligatorie")]
        public DateTime DataNastere { get; set; }

        [Required(ErrorMessage = "Rolul este obligatoriu")]
        public int RolId { get; set; }

        [Required(ErrorMessage = "Codul de verificare este obligatoriu")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Codul trebuie să conțină exact 6 cifre")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Codul trebuie să conțină doar cifre")]
        public string Code { get; set; } = string.Empty;
    }

    // ==================== Forgot Password DTOs ====================
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress(ErrorMessage = "Format email invalid")]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyResetCodeDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Codul este obligatoriu")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Codul trebuie să conțină exact 6 cifre")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Codul trebuie să conțină doar cifre")]
        public string Code { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Codul este obligatoriu")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Codul trebuie să conțină exact 6 cifre")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola nouă este obligatorie")]
        [MinLength(6, ErrorMessage = "Parola trebuie să conțină minim 6 caractere")]
        public string NewPassword { get; set; } = string.Empty;
    }

}