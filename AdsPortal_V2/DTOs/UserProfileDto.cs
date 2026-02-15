// Backend/DTOs/UserProfileDto.cs
namespace AdsPortal_V2.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
