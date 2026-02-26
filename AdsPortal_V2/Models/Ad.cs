using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdsPortal_V2.Models
{
    public enum AdType
    {
        Sell = 0,    // Продам
        Buy = 1,     // Куплю
        Services = 2 // Услуги
    }

    public class Ad
    {
        public int Id { get; set; }
        public AdType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsNegotiable { get; set; } = false;  // Договорная цена
        public bool IsHidden { get; set; } = false;       // Скрыто владельцем/админом
        public bool IsDeleted { get; set; } = false;      // Soft-delete: помечено удалённым
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public User Owner { get; set; } = null!;

        // Images collection
        public ICollection<AdImage> Images { get; set; } = [];
    }
}
