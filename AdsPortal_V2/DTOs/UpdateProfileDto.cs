// DTOs/UpdateProfileDto.cs
using System.ComponentModel.DataAnnotations;

namespace AdsPortal_V2.DTOs
{
    public class UpdateProfileDto
    {
        [MinLength(3), MaxLength(50)]
        public string? UserName { get; set; }

        [MaxLength(256)]
        [RegularExpression(@"^$|^([a-zA-Z0-9_\.\-]+)@([a-zA-Z0-9\.\-]+)\.([a-zA-Z\.]{2,6})$", ErrorMessage = "Некорректный email")]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MinLength(3)]
        public string? Password { get; set; }
    }
}
