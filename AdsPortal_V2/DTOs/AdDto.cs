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
        public bool IsNegotiable { get; set; }
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AdImageDto> Images { get; set; } = [];
        public int OwnerId { get; set; }
        public string OwnerUserName { get; set; } = string.Empty;
    }
}
