using AdsPortal_V2.Data;
using AdsPortal_V2.DTOs;
using AdsPortal_V2.Models;
using AdsPortal_V2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdsController : ControllerBase
    {
        private readonly AdsPortalContext _db;
        private readonly IImageService _images;

        public AdsController(AdsPortalContext db, IImageService images)
        {
            _db = db;
            _images = images;
        }

        // Create ad with optional images (multipart/form-data)
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateAdDto dto, IFormFileCollection? images)
        {
            var uid = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var ownerId))
                return Unauthorized();

            var ad = new Ad
            {
                Type = dto.Type,
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.IsNegotiable ? 0 : dto.Price,  // Договорная → Price = 0
                IsNegotiable = dto.IsNegotiable,
                CreatedAt = DateTime.UtcNow,
                OwnerId = ownerId
            };

            _db.Ads.Add(ad);
            await _db.SaveChangesAsync();

            List<string> imageUrls = new();
            if (images != null && images.Count > 0)
            {
                try
                {
                    imageUrls = await _images.SaveAdImagesAsync(images, ownerId, ad.Id);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = "Image processing failed", detail = ex.Message });
                }
            }

            var owner = await _db.Users.AsNoTracking().Where(u => u.Id == ownerId).Select(u => u.UserName).FirstOrDefaultAsync();

            var result = new AdDto
            {
                Id = ad.Id,
                Type = ad.Type,
                Title = ad.Title,
                Description = ad.Description,
                Price = ad.Price,
                IsNegotiable = ad.IsNegotiable,
                CreatedAt = ad.CreatedAt,
                ImageUrls = imageUrls,
                OwnerId = ad.OwnerId,
                OwnerUserName = owner ?? string.Empty
            };

            return CreatedAtAction(nameof(GetById), new { id = ad.Id }, result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var ad = await _db.Ads.AsNoTracking().Include(a => a.Owner).Where(a => a.Id == id).Select(a => new AdDto
            {
                Id = a.Id,
                Type = a.Type,
                Title = a.Title,
                Description = a.Description,
                Price = a.Price,
                IsNegotiable = a.IsNegotiable,
                CreatedAt = a.CreatedAt,
                ImageUrls = new List<string>(),
                OwnerId = a.OwnerId,
                OwnerUserName = a.Owner.UserName
            }).SingleOrDefaultAsync();

            if (ad is null) return NotFound();

            ad.ImageUrls = _images.GetAdImagePaths(ad.OwnerId, ad.Id);

            return Ok(ad);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 50;

            var ads = await _db.Ads
                .AsNoTracking()
                .Include(a => a.Owner)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new AdDto
                {
                    Id = a.Id,
                    Type = a.Type,
                    Title = a.Title,
                    Description = a.Description,
                    Price = a.Price,
                    IsNegotiable = a.IsNegotiable,
                    CreatedAt = a.CreatedAt,
                    ImageUrls = new List<string>(),
                    OwnerId = a.OwnerId,
                    OwnerUserName = a.Owner.UserName
                })
                .ToListAsync();

            foreach (var ad in ads)
            {
                ad.ImageUrls = _images.GetAdImagePaths(ad.OwnerId, ad.Id);
            }

            return Ok(ads);
        }
    }
}
