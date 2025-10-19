using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerConnect.Server.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Parola { get; set; } // null pentru Google login

        [Required]
        [MaxLength(100)]
        public string Nume { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Prenume { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefon { get; set; }

        public DateTime DataNastere { get; set; }

        public int RolId { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; } = null!;

        // Pentru autentificare Google
        [MaxLength(255)]
        public string? GoogleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}