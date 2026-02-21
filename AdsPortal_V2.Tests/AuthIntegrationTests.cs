using System;
using System.Threading.Tasks;
using AdsPortal_V2.Controllers;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Helpers;
using AdsPortal_V2.Models;
using AdsPortal_V2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AdsPortal_V2.Tests
{
    public class AuthIntegrationTests
    {
        private AdsPortal_V2.Data.AdsPortalContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AdsPortal_V2.Data.AdsPortalContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AdsPortal_V2.Data.AdsPortalContext(options);
        }

        private (IUserService userService, IJwtService jwtService) CreateServices(AdsPortal_V2.Data.AdsPortalContext ctx)
        {
            var pwdSettings = Options.Create(new PasswordSettings { SaltSize = 16, KeySize = 32, Iterations = 100000 });
            var jwtSettings = Options.Create(new JwtSettings
            {
                Key = "test_key_0123456789_test_key_012345",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiresInMinutes = 60
            });

            var userService = new UserService(ctx, pwdSettings);
            var jwtService = new JwtService(jwtSettings);
            return (userService, jwtService);
        }

        [Fact]
        public async Task Register_Then_Login_Works()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var (userService, jwtService) = CreateServices(ctx);
            var controller = new AuthController(userService, jwtService);

            var registerDto = new RegisterDto { Login = "integuser", Password = "Password123" };
            var regResult = await controller.Register(registerDto);
            var okReg = Assert.IsType<OkObjectResult>(regResult);
            var tokenReg = okReg.Value?.GetType().GetProperty("token")?.GetValue(okReg.Value) as string;
            Assert.False(string.IsNullOrWhiteSpace(tokenReg));

            // Now login with same credentials
            var loginDto = new LoginDto { Login = "integuser", Password = "Password123" };
            var loginResult = await controller.Login(loginDto);
            var okLogin = Assert.IsType<OkObjectResult>(loginResult);
            var tokenLogin = okLogin.Value?.GetType().GetProperty("token")?.GetValue(okLogin.Value) as string;
            Assert.False(string.IsNullOrWhiteSpace(tokenLogin));

            // Ensure user persisted
            var persisted = await ctx.Users.SingleOrDefaultAsync(u => u.Login == "integuser");
            Assert.NotNull(persisted);
        }

        [Fact]
        public async Task Register_Conflict_When_UserExists()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var (userService, jwtService) = CreateServices(ctx);
            var controller = new AuthController(userService, jwtService);

            // first register
            await controller.Register(new RegisterDto { Login = "dupuser", Password = "pwd12345" });

            // second register should conflict
            var second = await controller.Register(new RegisterDto { Login = "dupuser", Password = "pwd12345" });
            Assert.IsType<ConflictObjectResult>(second);
        }

        [Fact]
        public async Task Login_Returns_Unauthorized_On_WrongPassword()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var (userService, jwtService) = CreateServices(ctx);
            var controller = new AuthController(userService, jwtService);

            // register
            await controller.Register(new RegisterDto { Login = "userx", Password = "correct" });

            // attempt login with wrong password
            var res = await controller.Login(new LoginDto { Login = "userx", Password = "wrong" });
            Assert.IsType<UnauthorizedObjectResult>(res);
        }
    }
}
