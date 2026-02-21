using System;
using System.Threading.Tasks;
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdsPortal_V2.Tests
{
    public class UserServiceTests
    {
        private AdsPortalContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AdsPortalContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AdsPortalContext(options);
        }

        [Fact]
        public async Task RegisterAndAuthenticate_User_Succeeds()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var ctx = CreateContext(dbName);

            var pwd = Microsoft.Extensions.Options.Options.Create(new AdsPortal_V2.Helpers.PasswordSettings());
            var svc = new UserService(ctx, pwd);

            var reg = new RegisterDto { Login = "user1", Password = "password123" };
            var user = await svc.RegisterAsync(reg);

            Assert.NotNull(user);
            Assert.Equal(reg.Login, user.Login);

            var auth = await svc.AuthenticateAsync(new LoginDto { Login = "user1", Password = "password123" });
            Assert.NotNull(auth);
            Assert.Equal(user.Login, auth!.Login);

            var wrong = await svc.AuthenticateAsync(new LoginDto { Login = "user1", Password = "wrong" });
            Assert.Null(wrong);
        }
    }
}
