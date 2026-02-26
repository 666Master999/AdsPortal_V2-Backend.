// Backend/DTOs/UserProfileDto.cs
using AdsPortal_V2.Models;

namespace AdsPortal_V2.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Login { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; }
        public bool IsBlocked { get; set; }
        public List<AdDto> Ads { get; set; } = new();
    }
}
