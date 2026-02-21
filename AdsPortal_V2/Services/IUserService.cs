// Services/IUserService.cs
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;

namespace AdsPortal_V2.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(LoginDto dto);
        Task<User> RegisterAsync(RegisterDto dto);
        Task<bool> UserExistsAsync(string login);
        Task<User> UpdateProfileAsync(int userId, AdsPortal_V2.DTOs.UpdateProfileDto dto);
        Task ChangePasswordAsync(int userId, AdsPortal_V2.DTOs.ChangePasswordDto dto);
    }
}
