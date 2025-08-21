// Services/Interfaces/ISupabaseService.cs
// 🚫 NetTopologySuite supprimé - Utilisation coordonnées décimales uniquement

namespace SubExplore.Services.Interfaces
{
    public interface ISupabaseService
    {
        Task<bool> TestConnectionAsync();
        Task<IEnumerable<SubExplore.Models.Domain.Spot>> FindSpotsNearLocationAsync(double latitude, double longitude, int radiusKm = 50);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string bucket = "spot-media");
        Task<bool> DeleteFileAsync(string fileName, string bucket = "spot-media");
        Task<SubExplore.Models.Domain.User?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailTakenAsync(string email);
    }
}