using System;

namespace AdsPortal_V2.DTOs
{
    public class PublicUserProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AdDto> Ads { get; set; } = new();
    }
}
