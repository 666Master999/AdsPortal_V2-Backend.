// Services/JwtService.cs
using AdsPortal_V2.Helpers;
using AdsPortal_V2.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdsPortal_V2.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;
        private readonly SymmetricSecurityKey _signingKey;

        public JwtService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        }

        public string GenerateToken(User user)
        {
            var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("login", user.Login),
                new Claim("uid", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
