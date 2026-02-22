using System.ComponentModel.DataAnnotations;
using AdsPortal_V2.Models;

namespace AdsPortal_V2.DTOs
{
    public class CreateAdDto
    {
        [Required]
        [EnumDataType(typeof(AdType))]
        public AdType Type { get; set; }  // 0=Продам, 1=Куплю, 2=Услуги

        [Required, MinLength(3), MaxLength(20)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(3000)]
        public string? Description { get; set; }

        [Required, Range(0, 99999999999999)]  // 14 знаков макс
        public decimal Price { get; set; }  // 0 = бесплатно (если !IsNegotiable)

        public bool IsNegotiable { get; set; } = false;  // Договорная цена

        // Images: min 0, max 10 (через IFormFileCollection)
    }
}
