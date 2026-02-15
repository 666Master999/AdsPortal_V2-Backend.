// Backend/DTOs/HealthStatusDto.cs
namespace AdsPortal_V2.DTOs
{
    public class HealthStatusDto
    {
        public bool Backend { get; set; }
        public bool Db { get; set; }
        public string? Error { get; set; }
    }
}
