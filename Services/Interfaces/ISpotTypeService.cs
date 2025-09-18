using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;
using System.Collections.ObjectModel;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing spot types in the Add Spot workflow
    /// Handles loading, caching, and UI preparation of spot types
    /// </summary>
    public interface ISpotTypeService
    {
        /// <summary>
        /// Loads available spot types from API with caching
        /// </summary>
        Task<SpotTypeLoadResult> LoadSpotTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts spot types to UI-ready items for selection
        /// </summary>
        ObservableCollection<SpotTypeItem> CreateSpotTypeItems(List<SpotType> spotTypes);

        /// <summary>
        /// Validates spot type data integrity
        /// </summary>
        Task<bool> ValidateSpotTypeDataAsync(List<SpotType> spotTypes);

        /// <summary>
        /// Clears cached spot types (for testing/debugging)
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets cached spot types if available
        /// </summary>
        List<SpotType>? GetCachedSpotTypes();
    }

    /// <summary>
    /// Result of spot type loading operation
    /// </summary>
    public class SpotTypeLoadResult
    {
        public bool IsSuccess { get; set; }
        public List<SpotType> SpotTypes { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsFromCache { get; set; }
        public int LoadedCount { get; set; }
        public int FilteredCount { get; set; }

        public static SpotTypeLoadResult Success(List<SpotType> spotTypes, bool isFromCache = false)
        {
            return new SpotTypeLoadResult
            {
                IsSuccess = true,
                SpotTypes = spotTypes,
                IsFromCache = isFromCache,
                LoadedCount = spotTypes.Count,
                FilteredCount = spotTypes.Count
            };
        }

        public static SpotTypeLoadResult Failure(string errorMessage)
        {
            return new SpotTypeLoadResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}