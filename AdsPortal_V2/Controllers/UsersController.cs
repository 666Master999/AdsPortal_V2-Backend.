// Backend/Controllers/UsersController.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AdsPortalContext _db;
        private readonly AdsPortal_V2.Services.IUserService _users;
        private readonly AdsPortal_V2.Services.IImageService _images;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AdsPortalContext db, AdsPortal_V2.Services.IUserService users, AdsPortal_V2.Services.IImageService images, ILogger<UsersController> logger)
        {
            _db = db;
            _users = users;
            _images = images;
            _logger = logger;
        }

        // Upload avatar
        [HttpPost("profile/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile image)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (image == null)
                return BadRequest(new { error = "No image provided" });

            try
            {
                await _images.SaveAvatarAsync(image, userId);
                var avatarUrl = _images.GetAvatarPath(userId);
                return Ok(new { avatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return StatusCode(500, "Failed to upload avatar");
            }
        }

        // Возвращает профиль текущего пользователя (полный)
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Ads)
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Login = u.Login,
                    UserName = u.UserName,
                    Email = u.Email,
                    Phone = u.Phone,
                    CreatedAt = u.CreatedAt,
                    AvatarUrl = null,
                    Ads = u.Ads.Select(a => new AdDto
                    {
                        Id = a.Id,
                        Type = a.Type,
                        Title = a.Title,
                        Description = a.Description,
                        Price = a.Price,
                        CreatedAt = a.CreatedAt,
                        ImageUrls = new List<string>(),
                        OwnerId = a.OwnerId,
                        OwnerUserName = u.UserName
                    }).ToList()
                })
                .SingleOrDefaultAsync();

            if (user is null) return NotFound();

            user.AvatarUrl = _images.AvatarExists(userId) ? _images.GetAvatarPath(userId) : null;
            foreach (var ad in user.Ads)
            {
                ad.ImageUrls = _images.GetAdImagePaths(userId, ad.Id);
            }

            return Ok(user);
        }

        // Получить профиль по id
        // Гость (неавторизованный) не видит профиль (401).
        // Авторизованный, если владелец — получает полный профиль.
        // Авторизованный, если не владелец — получает публичную версию (без email/phone).
        [HttpGet("profiles/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Если нет токена — гость не видит профиль
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            var isOwner = requesterId == id;

            if (isOwner)
            {
                var full = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Ads)
                    .Where(u => u.Id == id)
                    .Select(u => new UserProfileDto
                    {
                        Id = u.Id,
                        Login = u.Login,
                        UserName = u.UserName,
                        Email = u.Email,
                        Phone = u.Phone,
                        CreatedAt = u.CreatedAt,
                        AvatarUrl = null,
                        Ads = u.Ads.Select(a => new AdDto
                        {
                            Id = a.Id,
                            Type = a.Type,
                            Title = a.Title,
                            Description = a.Description,
                            Price = a.Price,
                            CreatedAt = a.CreatedAt,
                            ImageUrls = new List<string>(),
                            OwnerId = a.OwnerId,
                            OwnerUserName = u.UserName
                        }).ToList()
                    })
                    .SingleOrDefaultAsync();

                if (full is null) return NotFound();

                full.AvatarUrl = _images.AvatarExists(id) ? _images.GetAvatarPath(id) : null;
                foreach (var ad in full.Ads)
                {
                    ad.ImageUrls = _images.GetAdImagePaths(id, ad.Id);
                }

                return Ok(full);
            }
            else
            {
                var pub = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Ads)
                    .Where(u => u.Id == id)
                    .Select(u => new PublicUserProfileDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        Phone = u.Phone,
                        CreatedAt = u.CreatedAt,
                        AvatarUrl = null,
                        Ads = u.Ads.Select(a => new AdDto
                        {
                            Id = a.Id,
                            Type = a.Type,
                            Title = a.Title,
                            Description = a.Description,
                            Price = a.Price,
                            CreatedAt = a.CreatedAt,
                            ImageUrls = new List<string>(),
                            OwnerId = a.OwnerId,
                            OwnerUserName = u.UserName
                        }).ToList()
                    })
                    .SingleOrDefaultAsync();

                if (pub is null) return NotFound();

                pub.AvatarUrl = _images.AvatarExists(id) ? _images.GetAvatarPath(id) : null;
                foreach (var ad in pub.Ads)
                {
                    ad.ImageUrls = _images.GetAdImagePaths(id, ad.Id);
                }

                return Ok(pub);
            }
        }

        // Обновление своего профиля (владелец)
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMe([FromBody] AdsPortal_V2.DTOs.UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _users.UpdateProfileAsync(userId, dto);

                var result = new AdsPortal_V2.DTOs.UserProfileDto
                {
                    Id = updated.Id,
                    Login = updated.Login,
                    UserName = updated.UserName,
                    Email = updated.Email,
                    Phone = updated.Phone,
                    CreatedAt = updated.CreatedAt
                };

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return StatusCode(500, "An error occurred while updating profile.");
            }
        }

        // Смена пароля (владелец)
        [HttpPost("profile/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] AdsPortal_V2.DTOs.ChangePasswordDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _users.ChangePasswordAsync(userId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Invalid current password for user {UserId}", userId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return StatusCode(500, "An error occurred while changing password.");
            }
        }

        // Альтернатива: обновление по id — только владелец может менять свой профиль
        [HttpPut("profiles/{id}")]
        public async Task<IActionResult> UpdateById(int id, [FromBody] AdsPortal_V2.DTOs.UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            if (requesterId != id)
                return Forbid();

            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _users.UpdateProfileAsync(id, dto);

                var result = new AdsPortal_V2.DTOs.UserProfileDto
                {
                    Id = updated.Id,
                    Login = updated.Login,
                    Email = updated.Email,
                    Phone = updated.Phone,
                    CreatedAt = updated.CreatedAt
                };

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", id);
                return StatusCode(500, "An error occurred while updating profile.");
            }
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var uid = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out userId))
                return false;

            return true;
        }
    }
}
