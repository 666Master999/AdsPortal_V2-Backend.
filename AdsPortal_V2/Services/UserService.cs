// Services/UserService.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Helpers;
using AdsPortal_V2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AdsPortal_V2.Services
{
    public class UserService(AdsPortalContext db, IOptions<JwtSettings> jwtOptions) : IUserService
    {
        private readonly AdsPortalContext _db = db;
        private readonly JwtSettings _jwt = jwtOptions.Value;

        public async Task<User?> AuthenticateAsync(LoginDto dto)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Login == dto.Login);
            if (user is null) return null;
            return VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt) ? user : null;
        }

        public async Task<User> RegisterAsync(RegisterDto dto)
        {
            CreatePasswordHash(dto.Password, out var hash, out var salt);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = dto.Login,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsAsync(string login)
        {
            return await _db.Users.AnyAsync(u => u.Login == login);
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("login", user.Login),
                new Claim("uid", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(storedHash);
        }
    }
}