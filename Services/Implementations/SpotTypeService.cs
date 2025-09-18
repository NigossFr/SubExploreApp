using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;
using SubExplore.Services.Interfaces;
using System.Collections.ObjectModel;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implementation of spot type management for Add Spot workflow
    /// Handles caching, API coordination, and UI preparation
    /// </summary>
    public class SpotTypeService : ISpotTypeService
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILogger<SpotTypeService> _logger;
        private List<SpotType>? _cachedSpotTypes;
        private DateTime? _cacheTimestamp;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

        public SpotTypeService(ISupabaseApiService apiService, ILogger<SpotTypeService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<SpotTypeLoadResult> LoadSpotTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Force visible diagnostic that won't be dropped
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeService.LoadSpotTypesAsync() CALLED");

                // Check cache first
                if (IsCacheValid())
                {
                    System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeService returning cached spot types: " + _cachedSpotTypes!.Count);
                    _logger.LogInformation("🎯 SpotTypeService: Returning cached spot types ({Count})", _cachedSpotTypes!.Count);
                    return SpotTypeLoadResult.Success(_cachedSpotTypes, isFromCache: true);
                }

                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeService loading spot types from API...");
                _logger.LogInformation("🎯 SpotTypeService: Loading spot types from API...");
                _logger.LogInformation("🔍 DIAGNOSTIC: About to call GetSpotTypesAsync()");

                // Load from API
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: About to call _apiService.GetSpotTypesAsync()");
                var rawSpotTypes = await _apiService.GetSpotTypesAsync();
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: _apiService.GetSpotTypesAsync() returned " + (rawSpotTypes?.Count ?? -1) + " types");

                _logger.LogInformation("🔍 DIAGNOSTIC: GetSpotTypesAsync() returned {Count} types", rawSpotTypes?.Count ?? -1);
                _logger.LogInformation("🔍 DIAGNOSTIC: RawSpotTypes is null: {IsNull}", rawSpotTypes == null);
                if (rawSpotTypes == null || !rawSpotTypes.Any())
                {
                    const string errorMsg = "Aucun type de spot retourné par l'API";
                    _logger.LogError("❌ SpotTypeService: API returned empty - {Error}", errorMsg);
                    return SpotTypeLoadResult.Failure(errorMsg);
                }
                _logger.LogInformation("🎯 SpotTypeService: Raw API returned {Count} spot types", rawSpotTypes.Count);

                // Filter for activity types only (as per existing logic)
                var filteredSupabaseTypes = rawSpotTypes.Where(st => st.Name != "Organization" && st.Name != "Business").ToList();
                _logger.LogInformation("🎯 SpotTypeService: Filtered to {Count} activity types", filteredSupabaseTypes.Count);

                // Convert to domain SpotType objects
                var activityTypes = filteredSupabaseTypes.Select(st => ConvertToDomainSpotType(st)).ToList();

                // Validate data integrity
                var isValid = await ValidateSpotTypeDataAsync(activityTypes);
                if (!isValid)
                {
                    const string errorMsg = "Les données des types de spots sont corrompues";
                    _logger.LogError("❌ SpotTypeService: Data validation failed");
                    return SpotTypeLoadResult.Failure(errorMsg);
                }

                // Update cache
                _cachedSpotTypes = activityTypes;
                _cacheTimestamp = DateTime.UtcNow;

                _logger.LogInformation("✅ SpotTypeService: Successfully loaded and cached {Count} spot types", activityTypes.Count);

                var result = SpotTypeLoadResult.Success(activityTypes);
                result.LoadedCount = rawSpotTypes.Count;
                result.FilteredCount = activityTypes.Count;
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ SpotTypeService: Load operation was cancelled");
                return SpotTypeLoadResult.Failure("Opération annulée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SpotTypeService: Unexpected error loading spot types");
                return SpotTypeLoadResult.Failure($"Erreur inattendue: {ex.Message}");
            }
        }

        public ObservableCollection<SpotTypeItem> CreateSpotTypeItems(List<SpotType> spotTypes)
        {
            System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeService.CreateSpotTypeItems called with " + spotTypes.Count + " types");
            _logger.LogInformation("🎯 SpotTypeService: Creating {Count} SpotTypeItems for UI", spotTypes.Count);

            var items = new ObservableCollection<SpotTypeItem>();

            foreach (var spotType in spotTypes)
            {
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: Creating SpotTypeItem for: " + spotType.Name);
                var item = new SpotTypeItem(spotType);
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeItem created - Name: " + item.Name + ", Description: " + item.Description);
                items.Add(item);
                System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeItem added to collection, total: " + items.Count);
                _logger.LogDebug("🎯 SpotTypeService: Created SpotTypeItem for '{Name}' (ID: {Id})", spotType.Name, spotType.Id);
            }

            System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: CreateSpotTypeItems returning " + items.Count + " items");
            _logger.LogInformation("✅ SpotTypeService: Successfully created {Count} SpotTypeItems", items.Count);
            return items;
        }

        public async Task<bool> ValidateSpotTypeDataAsync(List<SpotType> spotTypes)
        {
            if (spotTypes == null || !spotTypes.Any())
            {
                _logger.LogWarning("⚠️ SpotTypeService: Validation failed - no spot types provided");
                return false;
            }

            var invalidItems = spotTypes.Where(st =>
                st.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(st.Name) ||
                string.IsNullOrWhiteSpace(st.Description)).ToList();

            if (invalidItems.Any())
            {
                _logger.LogError("❌ SpotTypeService: Found {Count} invalid spot types", invalidItems.Count);
                foreach (var invalid in invalidItems.Take(3)) // Log first 3 for debugging
                {
                    _logger.LogError("❌ Invalid SpotType: ID={Id}, Name='{Name}', Description='{Description}'",
                        invalid.Id, invalid.Name, invalid.Description);
                }
                return false;
            }

            _logger.LogInformation("✅ SpotTypeService: All {Count} spot types are valid", spotTypes.Count);
            return true;
        }

        public void ClearCache()
        {
            _logger.LogInformation("🧹 SpotTypeService: Clearing cache");
            _cachedSpotTypes = null;
            _cacheTimestamp = null;
        }

        public List<SpotType>? GetCachedSpotTypes()
        {
            if (IsCacheValid())
            {
                _logger.LogDebug("🎯 SpotTypeService: Returning cached spot types ({Count})", _cachedSpotTypes!.Count);
                return _cachedSpotTypes;
            }

            _logger.LogDebug("🎯 SpotTypeService: No valid cache available");
            return null;
        }

        private bool IsCacheValid()
        {
            return _cachedSpotTypes != null &&
                   _cacheTimestamp.HasValue &&
                   DateTime.UtcNow - _cacheTimestamp.Value < _cacheExpiry;
        }

        private SpotType ConvertToDomainSpotType(Models.Supabase.SupabaseSpotType supabaseType)
        {
            return new SpotType
            {
                Id = supabaseType.Id,
                Name = supabaseType.Name,
                Description = supabaseType.Description,
                IconPath = supabaseType.IconPath,
                ColorCode = supabaseType.ColorCode,
                RequiresExpertValidation = supabaseType.RequiresExpertValidation,
                Category = ParseActivityCategory(supabaseType.Category),
                IsActive = supabaseType.IsActive,
                CreatedAt = supabaseType.CreatedAt,
                UpdatedAt = supabaseType.UpdatedAt
            };
        }

        private Models.Enums.ActivityCategory ParseActivityCategory(string category)
        {
            return category?.ToLowerInvariant() switch
            {
                "structure" => Models.Enums.ActivityCategory.Structure,
                "shop" => Models.Enums.ActivityCategory.Shop,
                "other" => Models.Enums.ActivityCategory.Other,
                _ => Models.Enums.ActivityCategory.Activity
            };
        }
    }
}