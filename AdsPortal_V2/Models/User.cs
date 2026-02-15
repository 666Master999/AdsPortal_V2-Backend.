// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace AdsPortal_V2.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Login { get; set; } = null!;

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        public byte[] PasswordHash { get; set; } = null!;

        [Required]
        public byte[] PasswordSalt { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
