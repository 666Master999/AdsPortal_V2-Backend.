using Microsoft.AspNetCore.Http;

namespace AdsPortal_V2.Services
{
    public interface IImageService
    {
        Task<string> SaveAvatarAsync(IFormFile file, int userId);
        Task<List<string>> SaveAdImagesAsync(IFormFileCollection files, int userId, int adId);
        string GetAvatarPath(int userId);
        Task<List<string>> GetAdImagePaths(int userId, int adId);
        Task<List<DTOs.AdImageDto>> GetAdImagesDto(int adId);
        bool AvatarExists(int userId);
        Task<int> GetAdImagesCount(int userId, int adId);
        void DeleteAvatar(int userId);
        Task DeleteAdImages(int userId, int adId);
    }
}
