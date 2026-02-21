using System.Threading.Tasks;
using AdsPortal_V2.Controllers;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
using AdsPortal_V2.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace AdsPortal_V2.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task Register_ReturnsToken_OnSuccess()
        {
            var users = Substitute.For<IUserService>();
            var jwt = Substitute.For<IJwtService>();

            var createdUser = new User { Id = 1, Login = "newuser" };
            users.RegisterAsync(Arg.Any<RegisterDto>()).Returns(Task.FromResult(createdUser));
            users.UserExistsAsync(Arg.Any<string>()).Returns(false);

            jwt.GenerateToken(createdUser).Returns("token-123");

            var controller = new AuthController(users, jwt);
            var result = await controller.Register(new RegisterDto { Login = "newuser", Password = "password" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var tokenProp = ok.Value?.GetType().GetProperty("token")?.GetValue(ok.Value) as string;
            Assert.Equal("token-123", tokenProp);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
        {
            var users = Substitute.For<IUserService>();
            var jwt = Substitute.For<IJwtService>();

            users.AuthenticateAsync(Arg.Any<LoginDto>()).Returns((User?)null);

            var controller = new AuthController(users, jwt);
            var result = await controller.Login(new LoginDto { Login = "x", Password = "bad" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
