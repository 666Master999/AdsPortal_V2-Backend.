// Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdsPortal_V2.Models
{
    public enum UserRole
    {
        User = 0,
        Admin = 1
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MinLength(3), MaxLength(50)]
        public string Login { get; set; } = null!;

        [Required, MinLength(3), MaxLength(50)]
        public string UserName { get; set; } = null!;

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

        public UserRole Role { get; set; } = UserRole.User;

        public bool IsBlocked { get; set; } = false;

        public ICollection<Ad> Ads { get; set; } = [];
    }
}
