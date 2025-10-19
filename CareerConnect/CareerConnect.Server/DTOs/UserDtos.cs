using System.ComponentModel.DataAnnotations;

namespace CareerConnect.Server.Models
{
    // Response DTO
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nume { get; set; } = string.Empty;
        public string Prenume { get; set; } = string.Empty;
        public string? Telefon { get; set; }
        public DateTime DataNastere { get; set; }
        public string RolNume { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Create DTO
    public class CreateUserDto
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
    }

    // Update DTO
    public class UpdateUserDto
    {
        [EmailAddress(ErrorMessage = "Format email invalid")]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Nume { get; set; }

        [MaxLength(100)]
        public string? Prenume { get; set; }

        [Phone(ErrorMessage = "Format telefon invalid")]
        public string? Telefon { get; set; }

        public DateTime? DataNastere { get; set; }

        public int? RolId { get; set; }
    }

    // Login DTO
    public class LoginDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola este obligatorie")]
        public string Parola { get; set; } = string.Empty;
    }

    // Google Login DTO
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    // Auth Response DTO
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }
}