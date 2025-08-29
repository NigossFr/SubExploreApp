using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    public interface ISharingService
    {
        /// <summary>
        /// Share spot information using native sharing capabilities
        /// </summary>
        /// <param name="spot">The spot to share</param>
        /// <param name="includePhotos">Whether to include photos in the share</param>
        /// <returns>True if sharing was successful</returns>
        Task<bool> ShareSpotAsync(Spot spot, bool includePhotos = false);

        /// <summary>
        /// Share text content using native sharing
        /// </summary>
        /// <param name="title">Share title</param>
        /// <param name="text">Share content</param>
        /// <param name="uri">Optional URI to share</param>
        /// <returns>True if sharing was successful</returns>
        Task<bool> ShareTextAsync(string title, string text, string? uri = null);

        /// <summary>
        /// Share file using native sharing
        /// </summary>
        /// <param name="title">Share title</param>
        /// <param name="filePath">Path to file to share</param>
        /// <returns>True if sharing was successful</returns>
        Task<bool> ShareFileAsync(string title, string filePath);

        /// <summary>
        /// Check if native sharing is available on the platform
        /// </summary>
        /// <returns>True if sharing is supported</returns>
        bool IsNativeSharingAvailable { get; }

        /// <summary>
        /// Generate a shareable link for a spot
        /// </summary>
        /// <param name="spotId">The spot ID</param>
        /// <returns>Shareable deep link</returns>
        string GenerateSpotShareLink(Guid spotId);
    }
}