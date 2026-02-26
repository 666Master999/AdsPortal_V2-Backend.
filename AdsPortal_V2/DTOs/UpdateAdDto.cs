using System.ComponentModel.DataAnnotations;
using AdsPortal_V2.Models;

namespace AdsPortal_V2.DTOs
{
    public class UpdateAdDto
    {
        [Required]
        [EnumDataType(typeof(AdType))]
        public AdType Type { get; set; }

        [Required, MinLength(3), MaxLength(20)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(3000)]
        public string? Description { get; set; }

        [Required, Range(0, 99999999999999)]
        public decimal Price { get; set; }

        public bool IsNegotiable { get; set; } = false;

        // --- Изображения ---
        // Новые изображения для добавления
        public List<IFormFile>? NewImages { get; set; }
        // Id изображений для удаления
        public List<int>? DeleteImageIds { get; set; }
        // Id главного изображения
        public int? MainImageId { get; set; }
        // Новый порядок изображений
        public List<int>? ImageOrder { get; set; }
    }
}
