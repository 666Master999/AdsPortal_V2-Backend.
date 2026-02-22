using Microsoft.AspNetCore.Http;

namespace AdsPortal_V2.Services
{
    public interface IImageService
    {
        Task<string> SaveAvatarAsync(IFormFile file, int userId);
        Task<List<string>> SaveAdImagesAsync(IFormFileCollection files, int userId, int adId);
        string GetAvatarPath(int userId);
        List<string> GetAdImagePaths(int userId, int adId);
        bool AvatarExists(int userId);
        int GetAdImagesCount(int userId, int adId);
        void DeleteAvatar(int userId);
        void DeleteAdImages(int userId, int adId);
    }
}
