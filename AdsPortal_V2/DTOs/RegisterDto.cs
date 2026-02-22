// DTOs/RegisterDto.cs
using System.ComponentModel.DataAnnotations;

namespace AdsPortal_V2.DTOs
{
    public class RegisterDto
    {
        [Required, MinLength(3), MaxLength(50)]
        public string Login { get; set; } = null!;

        [Required, MinLength(3), MaxLength(50)]
        public string Password { get; set; } = null!;
    }
}
