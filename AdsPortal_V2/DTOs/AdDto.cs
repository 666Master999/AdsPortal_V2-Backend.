using System;
using AdsPortal_V2.Models;

namespace AdsPortal_V2.DTOs
{
    public class AdDto
    {
        public int Id { get; set; }
        public AdType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsNegotiable { get; set; }  // Договорная цена
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public int OwnerId { get; set; }
        public string OwnerUserName { get; set; } = string.Empty;
    }
}
