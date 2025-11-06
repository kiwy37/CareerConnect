using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerConnect.Server.Models
{
    [Table("EmailVerificationCodes")]
    public class EmailVerificationCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Pentru securitate - limităm numărul de încercări
        public int AttemptCount { get; set; } = 0;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        // Tip de verificare: Login sau Register
        [Required]
        [MaxLength(20)]
        public string VerificationType { get; set; } = string.Empty; // "Login" sau "Register"
    }
}