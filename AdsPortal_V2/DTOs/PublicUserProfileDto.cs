using System;

namespace AdsPortal_V2.DTOs
{
    public class PublicUserProfileDto
    {
        public int Id { get; set; }
        public string Login { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
