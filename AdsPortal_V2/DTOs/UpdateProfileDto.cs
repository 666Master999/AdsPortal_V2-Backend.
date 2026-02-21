// DTOs/UpdateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace AdsPortal_V2.DTOs
{
    public class UpdateProfileDto
    {
        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }
    }
}
