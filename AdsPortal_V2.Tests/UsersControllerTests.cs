using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AdsPortal_V2.Controllers;
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdsPortal_V2.Tests
{
    public class UsersControllerTests
    {
        private AdsPortalContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AdsPortalContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AdsPortalContext(options);
        }

        [Fact]
        public async Task Me_ReturnsProfile_WhenAuthenticated()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var u = new User { Id = 1, Login = "u1", Email = "e@e.com", Phone = "123", PasswordHash = new byte[1], PasswordSalt = new byte[1], CreatedAt = DateTime.UtcNow };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var pwd = Microsoft.Extensions.Options.Options.Create(new AdsPortal_V2.Helpers.PasswordSettings());
            var svc = new AdsPortal_V2.Services.UserService(ctx, pwd);
            var controller = new UsersController(ctx, svc);

            // set user identity with uid claim
            var claims = new[] { new Claim("uid", u.Id.ToString()) };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var result = await controller.Me();
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserProfileDto>(ok.Value);
            Assert.Equal(u.Id, dto.Id);
            Assert.Equal(u.Login, dto.Login);
        }

        [Fact]
        public async Task UpdateMe_UpdatesEmailPhoneAndPassword()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var u = new User { Id = 1, Login = "u1", Email = "e@e.com", Phone = "123", PasswordHash = new byte[1], PasswordSalt = new byte[1], CreatedAt = DateTime.UtcNow };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var pwd = Microsoft.Extensions.Options.Options.Create(new AdsPortal_V2.Helpers.PasswordSettings());
            var svc = new AdsPortal_V2.Services.UserService(ctx, pwd);
            var controller = new UsersController(ctx, svc);

            var claims = new[] { new Claim("uid", u.Id.ToString()) };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var dto = new UpdateProfileDto { Email = "new@e.com", Phone = "999", Password = "newpass123" };
            var res = await controller.UpdateMe(dto);
            var ok = Assert.IsType<OkObjectResult>(res);
            var profile = Assert.IsType<UserProfileDto>(ok.Value);
            Assert.Equal(u.Id, profile.Id);
            Assert.Equal("new@e.com", profile.Email);
            Assert.Equal("999", profile.Phone);

            // verify password actually changed by authenticating
            var auth = await svc.AuthenticateAsync(new LoginDto { Login = u.Login, Password = "newpass123" });
            Assert.NotNull(auth);
        }
    }
}
