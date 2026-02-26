// Backend/Controllers/UsersController.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
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
    public class UsersController(
        AdsPortalContext db,
        AdsPortal_V2.Services.IUserService users,
        AdsPortal_V2.Services.IImageService images,
        ILogger<UsersController> logger) : ControllerBase
    {
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var uid = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out userId))
                return false;
            return true;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

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
                await images.SaveAvatarAsync(image, userId);
                var avatarUrl = images.GetAvatarPath(userId);
                return Ok(new { avatarUrl });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return StatusCode(500, "Failed to upload avatar");
            }
        }

        // Профиль текущего пользователя (полный)
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var user = await db.Users
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
                    Role = u.Role,
                    IsBlocked = u.IsBlocked,
                    AvatarUrl = null,
                    Ads = u.Ads.Where(a => !a.IsDeleted).Select(a => new AdDto
                    {
                        Id = a.Id,
                        Type = a.Type,
                        Title = a.Title,
                        Description = a.Description,
                        Price = a.Price,
                        IsNegotiable = a.IsNegotiable,
                        IsHidden = a.IsHidden,
                        IsDeleted = a.IsDeleted,
                        CreatedAt = a.CreatedAt,
                        OwnerId = a.OwnerId,
                        OwnerUserName = u.UserName
                    }).ToList()
                })
                .SingleOrDefaultAsync();

            if (user is null) return NotFound();

            user.AvatarUrl = images.AvatarExists(userId) ? images.GetAvatarPath(userId) : null;

            return Ok(user);
        }

        // Получить профиль по id
        // Гость (неавторизованный) не видит профиль (401).
        // Владелец — полный профиль. Другой авторизованный — публичная версия.
        [HttpGet("profiles/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            var isOwnerOrAdmin = requesterId == id || IsAdmin();

            if (isOwnerOrAdmin)
            {
                var full = await db.Users
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
                        Role = u.Role,
                        IsBlocked = u.IsBlocked,
                        AvatarUrl = null,
                        Ads = u.Ads.Where(a => !a.IsDeleted).Select(a => new AdDto
                        {
                            Id = a.Id,
                            Type = a.Type,
                            Title = a.Title,
                            Description = a.Description,
                            Price = a.Price,
                            IsNegotiable = a.IsNegotiable,
                            IsHidden = a.IsHidden,
                            IsDeleted = a.IsDeleted,
                            CreatedAt = a.CreatedAt,
                            OwnerId = a.OwnerId,
                            OwnerUserName = u.UserName
                        }).ToList()
                    })
                    .SingleOrDefaultAsync();

                if (full is null) return NotFound();

                full.AvatarUrl = images.AvatarExists(id) ? images.GetAvatarPath(id) : null;

                return Ok(full);
            }
            else
            {
                var pub = await db.Users
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
                        Role = u.Role,
                        AvatarUrl = null,
                        Ads = u.Ads.Where(a => !a.IsDeleted && !a.IsHidden).Select(a => new AdDto
                        {
                            Id = a.Id,
                            Type = a.Type,
                            Title = a.Title,
                            Description = a.Description,
                            Price = a.Price,
                            IsNegotiable = a.IsNegotiable,
                            IsHidden = a.IsHidden,
                            IsDeleted = a.IsDeleted,
                            CreatedAt = a.CreatedAt,
                            OwnerId = a.OwnerId,
                            OwnerUserName = u.UserName
                        }).ToList()
                    })
                    .SingleOrDefaultAsync();

                if (pub is null) return NotFound();

                pub.AvatarUrl = images.AvatarExists(id) ? images.GetAvatarPath(id) : null;

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
                var updated = await users.UpdateProfileAsync(userId, dto);

                var result = new AdsPortal_V2.DTOs.UserProfileDto
                {
                    Id = updated.Id,
                    Login = updated.Login,
                    UserName = updated.UserName,
                    Email = updated.Email,
                    Phone = updated.Phone,
                    CreatedAt = updated.CreatedAt,
                    Role = updated.Role,
                    IsBlocked = updated.IsBlocked
                };

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating profile for user {UserId}", userId);
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
                await users.ChangePasswordAsync(userId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Invalid current password for user {UserId}", userId);
                return Forbid();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return StatusCode(500, "An error occurred while changing password.");
            }
        }

        // Обновление профиля по id — только владелец
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
                var updated = await users.UpdateProfileAsync(id, dto);

                var result = new AdsPortal_V2.DTOs.UserProfileDto
                {
                    Id = updated.Id,
                    Login = updated.Login,
                    Email = updated.Email,
                    Phone = updated.Phone,
                    CreatedAt = updated.CreatedAt,
                    Role = updated.Role,
                    IsBlocked = updated.IsBlocked
                };

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating profile for user {UserId}", id);
                return StatusCode(500, "An error occurred while updating profile.");
            }
        }

        // ─── Admin endpoints ────────────────────────────────────────────────────

        // Список всех пользователей (только для админа)
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 50;

            var usersList = await db.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Login = u.Login,
                    UserName = u.UserName,
                    Email = u.Email,
                    Phone = u.Phone,
                    CreatedAt = u.CreatedAt,
                    Role = u.Role,
                    IsBlocked = u.IsBlocked
                })
                .ToListAsync();

            return Ok(usersList);
        }

        // Заблокировать пользователя (только для админа)
        [HttpPatch("{id}/block")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var target = await db.Users.FindAsync(id);
            if (target is null) return NotFound();

            if (target.Role == UserRole.Admin)
                return BadRequest(new { error = "Cannot block another admin." });

            target.IsBlocked = true;
            await db.SaveChangesAsync();
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Admin blocked user {UserId}", id);
            }

            return Ok(new { target.Id, target.Login, target.IsBlocked });
        }

        // Разблокировать пользователя (только для админа)
        [HttpPatch("{id}/unblock")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var target = await db.Users.FindAsync(id);
            if (target is null) return NotFound();

            target.IsBlocked = false;
            await db.SaveChangesAsync();
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Admin unblocked user {UserId}", id);
            }

            return Ok(new { target.Id, target.Login, target.IsBlocked });
        }
    }
}
