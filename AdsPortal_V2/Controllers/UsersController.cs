// Backend/Controllers/UsersController.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController(AdsPortalContext db) : ControllerBase
    {
        private readonly AdsPortalContext _db = db;

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var uidClaim = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(uidClaim) || !Guid.TryParse(uidClaim, out var userId))
                return Unauthorized();

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Login = u.Login,
                    Email = u.Email,
                    Phone = u.Phone,
                    CreatedAt = u.CreatedAt
                })
                .SingleOrDefaultAsync();

            return user is null ? NotFound() : Ok(user);
        }
    }
}