// Services/UserService.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
using AdsPortal_V2.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AdsPortal_V2.Services
{
    public class UserService(AdsPortalContext db, IOptions<PasswordSettings> pwdOptions, ILogger<UserService> logger) : IUserService
    {
        private readonly AdsPortalContext _db = db;
        private readonly PasswordSettings _pwd = pwdOptions.Value;
        private readonly ILogger<UserService> _logger = logger;

        public async Task<User> UpdateProfileAsync(int userId, AdsPortal_V2.DTOs.UpdateProfileDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("UpdateProfileAsync: user {UserId} not found", userId);
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                CreatePasswordHash(dto.Password, out var hash, out var salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
            }

            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} profile updated", userId);
            return user;
        }

        public async Task ChangePasswordAsync(int userId, AdsPortal_V2.DTOs.ChangePasswordDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("ChangePasswordAsync: user {UserId} not found", userId);
                throw new KeyNotFoundException("User not found");
            }

            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("ChangePasswordAsync: invalid current password for user {UserId}", userId);
                throw new UnauthorizedAccessException("Current password is invalid.");
            }

            CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} changed password", userId);
        }

        public async Task<User?> AuthenticateAsync(LoginDto dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Login == dto.Login);
            if (user is null)
            {
                _logger.LogWarning("AuthenticateAsync: login not found {Login}", dto.Login);
                return null;
            }

            var ok = VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt);
            if (!ok) _logger.LogWarning("AuthenticateAsync: invalid password for {Login}", dto.Login);
            return ok ? user : null;
        }

        public async Task<User> RegisterAsync(RegisterDto dto)
        {
            CreatePasswordHash(dto.Password, out var hash, out var salt);
            var user = new User
            {
                // Id is database-generated identity
                Login = dto.Login,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("New user registered: {Login} (Id: {UserId})", user.Login, user.Id);
            return user;
        }

        public async Task<bool> UserExistsAsync(string login)
        {
            return await _db.Users.AnyAsync(u => u.Login == login);
        }

        // JWT generation moved to a dedicated service (IJwtService)

        private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(_pwd.SaltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _pwd.Iterations, HashAlgorithmName.SHA256);
            hash = pbkdf2.GetBytes(_pwd.KeySize);
        }

        private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, storedSalt, _pwd.Iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(storedHash.Length);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
