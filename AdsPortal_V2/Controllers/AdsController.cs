using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
using AdsPortal_V2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdsController(
        AdsPortalContext db,
        IImageService images,
        IWebHostEnvironment env
    ) : ControllerBase
    {
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var uid = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(uid) && int.TryParse(uid, out userId);
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        // Create ad with optional images (multipart/form-data)
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateAdDto dto, IFormFileCollection? imagesCollection)
        {
            if (!TryGetUserId(out var ownerId))
                return Unauthorized();

            var owner = await db.Users.FindAsync(ownerId);
            if (owner is null) return Unauthorized();

            if (owner.IsBlocked)
                return StatusCode(403, new { error = "Your account is blocked. You cannot publish ads." });

            var ad = new Ad
            {
                Type = dto.Type,
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.IsNegotiable ? 0 : dto.Price,
                IsNegotiable = dto.IsNegotiable,
                CreatedAt = DateTime.UtcNow,
                OwnerId = ownerId
            };

            db.Ads.Add(ad);
            await db.SaveChangesAsync();

            List<string> imageUrls = [];
            if (imagesCollection != null && imagesCollection.Count > 0)
            {
                try
                {
                    imageUrls = await images.SaveAdImagesAsync(imagesCollection, ownerId, ad.Id);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = "Image processing failed", detail = ex.Message });
                }
            }

            var ownerName = await db.Users.AsNoTracking().Where(u => u.Id == ownerId).Select(u => u.UserName).FirstOrDefaultAsync();
            var imagesDto = await images.GetAdImagesDto(ad.Id);
            var result = new AdDto
            {
                Id = ad.Id,
                Type = ad.Type,
                Title = ad.Title,
                Description = ad.Description,
                Price = ad.Price,
                IsNegotiable = ad.IsNegotiable,
                IsHidden = ad.IsHidden,
                IsDeleted = ad.IsDeleted,
                CreatedAt = ad.CreatedAt,
                UpdatedAt = ad.UpdatedAt,
                Images = imagesDto,
                OwnerId = ad.OwnerId,
                OwnerUserName = ownerName ?? string.Empty
            };
            return CreatedAtAction(nameof(GetById), new { id = ad.Id }, result);
        }

        // Edit ad — owner or admin
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateAdDto dto)
        {
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ad = await db.Ads.FindAsync(id);
            if (ad is null || ad.IsDeleted) return NotFound();

            var isAdmin = IsAdmin();
            if (ad.OwnerId != requesterId && !isAdmin)
                return Forbid();

            // Проверка блокировки только для владельца
            if (!isAdmin)
            {
                var user = await db.Users.FindAsync(requesterId);
                if (user?.IsBlocked == true)
                    return StatusCode(403, new { error = "Your account is blocked. You cannot edit ads." });
            }

            ad.Type = dto.Type;
            ad.Title = dto.Title;
            ad.Description = dto.Description;
            ad.Price = dto.IsNegotiable ? 0 : dto.Price;
            ad.IsNegotiable = dto.IsNegotiable;
            ad.UpdatedAt = DateTime.UtcNow;

            // --- Обработка изображений ---
            // Удаление изображений (и файлов)
            if (dto.DeleteImageIds != null && dto.DeleteImageIds.Count > 0)
            {
                var toDelete = db.AdImages.Where(img => img.AdId == ad.Id && dto.DeleteImageIds.Contains(img.Id)).ToList();
                foreach (var img in toDelete)
                {
                    if (!string.IsNullOrWhiteSpace(img.FilePath))
                    {
                        var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", img.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                }
                db.AdImages.RemoveRange(toDelete);
            }

            // Добавление новых изображений
            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                try
                {
                    var files = new FormFileCollection();
                    files.AddRange(dto.NewImages);
                    await images.SaveAdImagesAsync(files, ad.OwnerId, ad.Id);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = "Image processing failed", detail = ex.Message });
                }
            }

            // Установка главного изображения
            if (dto.MainImageId.HasValue)
            {
                var imgs = db.AdImages.Where(img => img.AdId == ad.Id).ToList();
                foreach (var img in imgs)
                    img.IsMain = img.Id == dto.MainImageId.Value;
            }

            // Обновление порядка изображений
            if (dto.ImageOrder != null && dto.ImageOrder.Count > 0)
            {
                var imgs = db.AdImages.Where(img => img.AdId == ad.Id).ToList();
                foreach (var img in imgs)
                {
                    var idx = dto.ImageOrder.IndexOf(img.Id);
                    img.Order = idx >= 0 ? idx : img.Order;
                }
            }

            await db.SaveChangesAsync();

            var ownerName = await db.Users.AsNoTracking().Where(u => u.Id == ad.OwnerId).Select(u => u.UserName).FirstOrDefaultAsync();
            var imagesDto = await images.GetAdImagesDto(ad.Id);
            return Ok(new AdDto
            {
                Id = ad.Id,
                Type = ad.Type,
                Title = ad.Title,
                Description = ad.Description,
                Price = ad.Price,
                IsNegotiable = ad.IsNegotiable,
                IsHidden = ad.IsHidden,
                IsDeleted = ad.IsDeleted,
                CreatedAt = ad.CreatedAt,
                UpdatedAt = ad.UpdatedAt,
                Images = imagesDto,
                OwnerId = ad.OwnerId,
                OwnerUserName = ownerName ?? string.Empty
            });
        }

        // Hide / unhide ad — owner or admin
        [HttpPatch("{id}/visibility")]
        public async Task<IActionResult> SetVisibility(int id, [FromBody] SetVisibilityDto dto)
        {
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            var ad = await db.Ads.FindAsync(id);
            if (ad is null || ad.IsDeleted) return NotFound();

            var isAdmin = IsAdmin();
            if (ad.OwnerId != requesterId && !isAdmin)
                return Forbid();

            // Проверка блокировки только для владельца
            if (!isAdmin)
            {
                var user = await db.Users.FindAsync(requesterId);
                if (user?.IsBlocked == true)
                    return StatusCode(403, new { error = "Your account is blocked. You cannot hide/unhide ads." });
            }

            ad.IsHidden = dto.IsHidden;
            await db.SaveChangesAsync();

            return Ok(new { ad.Id, ad.IsHidden });
        }

        // Soft-delete ad — owner or admin
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetUserId(out var requesterId))
                return Unauthorized();

            var ad = await db.Ads.FindAsync(id);
            if (ad is null || ad.IsDeleted) return NotFound();

            var isAdmin = IsAdmin();
            if (ad.OwnerId != requesterId && !isAdmin)
                return Forbid();

            // Проверка блокировки только для владельца
            if (!isAdmin)
            {
                var user = await db.Users.FindAsync(requesterId);
                if (user?.IsBlocked == true)
                    return StatusCode(403, new { error = "Your account is blocked. You cannot delete ads." });
            }

            ad.IsDeleted = true;
            await db.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var isAdmin = IsAdmin();

            var adEntity = await db.Ads.AsNoTracking().Include(a => a.Owner)
                .Where(a => a.Id == id && !a.IsDeleted && (!a.IsHidden || isAdmin))
                .SingleOrDefaultAsync();

            if (adEntity is null) return NotFound();

            // Владелец видит своё скрытое объявление
            if (adEntity.IsHidden && !isAdmin)
            {
                if (!TryGetUserId(out var requesterId) || requesterId != adEntity.OwnerId)
                    return NotFound();
            }

            var imagesDto = await images.GetAdImagesDto(adEntity.Id);
            var ad = new AdDto
            {
                Id = adEntity.Id,
                Type = adEntity.Type,
                Title = adEntity.Title,
                Description = adEntity.Description,
                Price = adEntity.Price,
                IsNegotiable = adEntity.IsNegotiable,
                IsHidden = adEntity.IsHidden,
                IsDeleted = adEntity.IsDeleted,
                CreatedAt = adEntity.CreatedAt,
                UpdatedAt = adEntity.UpdatedAt,
                Images = imagesDto,
                OwnerId = adEntity.OwnerId,
                OwnerUserName = adEntity.Owner?.UserName ?? string.Empty
            };

            return Ok(ad);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 50;

            var isAdmin = IsAdmin();
            TryGetUserId(out var currentUserId);

            var adEntities = await db.Ads
                .AsNoTracking()
                .Include(a => a.Owner)
                .Where(a => !a.IsDeleted && (!a.IsHidden || isAdmin || a.OwnerId == currentUserId))
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var ads = new List<AdDto>();
            foreach (var a in adEntities)
            {
                var imagesDto = await images.GetAdImagesDto(a.Id);
                ads.Add(new AdDto
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
                    UpdatedAt = a.UpdatedAt,
                    Images = imagesDto,
                    OwnerId = a.OwnerId,
                    OwnerUserName = a.Owner?.UserName ?? string.Empty
                });
            }

            return Ok(ads);
        }
    }
}
