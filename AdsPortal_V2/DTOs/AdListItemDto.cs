using System;

namespace AdsPortal_V2.DTOs
{
    public class AdListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MainImageUrl { get; set; } = string.Empty;
        public int Type { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int OwnerId { get; set; }
        public string OwnerUserName { get; set; } = string.Empty;
    }
}
