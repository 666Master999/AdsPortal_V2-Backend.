using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using AdsPortal_V2.Helpers;
using AdsPortal_V2.Models;
using AdsPortal_V2.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AdsPortal_V2.Tests
{
    public class JwtServiceTests
    {
        [Fact]
        public void GenerateToken_IncludesExpectedClaimsAndAudience()
        {
            var settings = new JwtSettings
            {
                // Key must be at least 256 bits (32 bytes) for HS256
                Key = "test_key_0123456789_test_key_012345",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiresInMinutes = 60
            };

            var svc = new JwtService(Options.Create(settings));
            var user = new User { Id = 123, Login = "jdoe" };

            var token = svc.GenerateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(settings.Issuer, jwt.Issuer);
            Assert.Contains(settings.Audience, jwt.Audiences);
            Assert.Contains(jwt.Claims, c => c.Type == "uid" && c.Value == user.Id.ToString());
            Assert.Contains(jwt.Claims, c => c.Type == "login" && c.Value == user.Login);
        }
    }
}
