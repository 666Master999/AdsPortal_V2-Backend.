// Services/IJwtService.cs
using AdsPortal_V2.Models;

namespace AdsPortal_V2.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
