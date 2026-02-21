// Helpers/PasswordSettings.cs
namespace AdsPortal_V2.Helpers
{
    public class PasswordSettings
    {
        // Defaults provide secure sensible values if configuration is absent
        public int SaltSize { get; set; } = 16; // bytes
        public int KeySize { get; set; } = 32;  // bytes
        public int Iterations { get; set; } = 100_000;
    }
}
