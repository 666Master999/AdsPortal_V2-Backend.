using System;

namespace AdsPortal_V2.DTOs
{
    public class AdImageDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int Order { get; set; }
    }
}
