using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using AdsPortal_V2.Data;
using AdsPortal_V2.Models;

namespace AdsPortal_V2.Services
{

public class ImageService(
    IWebHostEnvironment env,
    ILogger<ImageService> logger,
    AdsPortalContext db
) : IImageService
{
    private const int MaxBytes = 100 * 1024; // 100 KB

    public async Task<List<DTOs.AdImageDto>> GetAdImagesDto(int adId)
    {
        return await db.AdImages
            .Where(i => i.AdId == adId)
            .OrderBy(i => i.Order)
            .Select(i => new DTOs.AdImageDto
            {
                Id = i.Id,
                Url = i.FilePath,
                IsMain = i.IsMain,
                Order = i.Order
            })
            .ToListAsync();
    }

    public async Task<string> SaveAvatarAsync(IFormFile file, int userId)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.ContentType.StartsWith("image/")) throw new ArgumentException("File is not an image.");

        var avatarFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "files", userId.ToString(), "avatar");
        if (!Directory.Exists(avatarFolder)) Directory.CreateDirectory(avatarFolder);

        var fileName = "av.jpeg";
        var outputPath = Path.Combine(avatarFolder, fileName);
        var relativeUrl = $"/files/{userId}/avatar/{fileName}";

        await CompressAndSaveImageAsync(file, outputPath);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Saved avatar for user {UserId} at {Path}", userId, outputPath);
        }
        return relativeUrl;
    }

    public async Task<List<string>> SaveAdImagesAsync(IFormFileCollection files, int userId, int adId)
    {
        if (files == null || files.Count == 0) return [];

        var adFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "files", userId.ToString(), "userAds", adId.ToString());
        if (!Directory.Exists(adFolder)) Directory.CreateDirectory(adFolder);

        var urls = new List<string>();
        int order = 0;

        foreach (var file in files)
        {
            if (!file.ContentType.StartsWith("image/"))
            {
                logger.LogWarning("Skipped non-image file: {FileName}", file.FileName);
                continue;
            }

            var fileName = $"{order + 1}.jpeg";
            var outputPath = Path.Combine(adFolder, fileName);
            var relativeUrl = $"/files/{userId}/userAds/{adId}/{fileName}";

            await CompressAndSaveImageAsync(file, outputPath);

            // Сохраняем в БД
            var adImage = new AdImage
            {
                AdId = adId,
                FilePath = relativeUrl,
                IsMain = order == 0,  // Первое изображение = главное
                Order = order
            };

            db.AdImages.Add(adImage);
            urls.Add(relativeUrl);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Saved ad image {Order} for ad {AdId} at {Path}", order + 1, adId, outputPath);
            }
            order++;
        }

        await db.SaveChangesAsync();
        return urls;
    }

    private async Task CompressAndSaveImageAsync(IFormFile file, string outputPath)
    {
        using var image = await Image.LoadAsync(file.OpenReadStream());

        var maxDimension = 2000;
        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            var ratio = Math.Min((float)maxDimension / image.Width, (float)maxDimension / image.Height);
            var newW = Math.Max(1, (int)(image.Width * ratio));
            var newH = Math.Max(1, (int)(image.Height * ratio));
            image.Mutate(x => x.Resize(newW, newH));
        }

        var quality = 90;
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new JpegEncoder { Quality = quality });

        while (ms.Length > MaxBytes && quality >= 30)
        {
            ms.SetLength(0);
            quality -= 10;
            await image.SaveAsync(ms, new JpegEncoder { Quality = quality });

            if (ms.Length > MaxBytes && image.Width > 100 && image.Height > 100)
            {
                image.Mutate(x => x.Resize(image.Width * 9 / 10, image.Height * 9 / 10));
            }
        }

        while (ms.Length > MaxBytes && image.Width > 100 && image.Height > 100)
        {
            ms.SetLength(0);
            image.Mutate(x => x.Resize(image.Width * 9 / 10, image.Height * 9 / 10));
            await image.SaveAsync(ms, new JpegEncoder { Quality = quality });
        }

        await File.WriteAllBytesAsync(outputPath, ms.ToArray());
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Compressed image to {Size} bytes at quality {Quality}", ms.Length, quality);
        }
    }

        public string GetAvatarPath(int userId)
        {
            return $"/files/{userId}/avatar/av.jpeg";
        }

    public async Task<List<string>> GetAdImagePaths(int userId, int adId)
    {
        // Читаем из БД вместо файловой системы
        return await db.AdImages
            .Where(i => i.AdId == adId)
            .OrderBy(i => i.Order)
            .Select(i => i.FilePath)
            .ToListAsync();
    }

    public bool AvatarExists(int userId)
    {
        var avatarPath = Path.Combine(env.WebRootPath ?? "wwwroot", "files", userId.ToString(), "avatar", "av.jpeg");
        return File.Exists(avatarPath);
    }

    public async Task<int> GetAdImagesCount(int userId, int adId)
    {
        return await db.AdImages.CountAsync(i => i.AdId == adId);
    }

    public void DeleteAvatar(int userId)
    {
        var avatarPath = Path.Combine(env.WebRootPath ?? "wwwroot", "files", userId.ToString(), "avatar", "av.jpeg");
        if (File.Exists(avatarPath))
        {
            File.Delete(avatarPath);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Deleted avatar for user {UserId}", userId);
            }
        }
    }

    public async Task DeleteAdImages(int userId, int adId)
    {
        var adFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "files", userId.ToString(), "userAds", adId.ToString());
        if (Directory.Exists(adFolder))
        {
            Directory.Delete(adFolder, true);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Deleted images folder for ad {AdId}", adId);
            }
        }

        // Удаляем из БД (если каскадное удаление не сработало)
        var imagesList = await db.AdImages.Where(i => i.AdId == adId).ToListAsync();
        db.AdImages.RemoveRange(imagesList);
        await db.SaveChangesAsync();
    }
    }
}

