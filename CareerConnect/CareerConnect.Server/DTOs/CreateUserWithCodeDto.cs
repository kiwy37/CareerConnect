using System.ComponentModel.DataAnnotations;

namespace CareerConnect.Server.DTOs
{
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

    public class LinkedInUserData
    {
        public string Id { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
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
}
