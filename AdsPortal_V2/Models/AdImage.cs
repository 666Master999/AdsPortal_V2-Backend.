// Models/AdImage.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdsPortal_V2.Models
{
    public class AdImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AdId { get; set; }

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = null!;  // "/files/45/userAds/123/1.jpeg"

        public bool IsMain { get; set; } = false;

        public int Order { get; set; } = 0;

        // Navigation
        public Ad Ad { get; set; } = null!;
    }
}
