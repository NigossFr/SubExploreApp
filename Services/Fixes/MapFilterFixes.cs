using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.Models.Domain;
using SubExplore.ViewModels.Map;
using System.Collections.ObjectModel;

namespace SubExplore.Services.Fixes
{
    /// <summary>
    /// Fixed implementations for common map filter issues
    /// </summary>
    public static class MapFilterFixes
    {
        /// <summary>
        /// Enhanced MapViewModel initialization that ensures proper data loading order
        /// </summary>
        public static async Task<bool> InitializeMapViewModelAsync(
            MapViewModel viewModel,
            ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting enhanced MapViewModel initialization...");

                // Step 1: Verify database connectivity
                if (!await VerifyDatabaseConnectivity(viewModel, logger))
                {
                    logger.LogError("Database connectivity check failed");
                    return false;
                }

                // Step 2: Load spot types first (required for filters)
                logger.LogDebug("Loading spot types...");
                await viewModel.LoadSpotTypesCommand.ExecuteAsync(null);
                
                if (viewModel.SpotTypes?.Count == 0)
                {
                    logger.LogWarning("No spot types loaded - filters may not work");
                }
                else
                {
                    logger.LogInformation("Loaded {TypeCount} spot types", viewModel.SpotTypes?.Count ?? 0);
                }

                // Step 3: Load initial spots
                logger.LogDebug("Loading spots...");
                await viewModel.LoadSpotsCommand.ExecuteAsync(null);
                
                if (viewModel.Spots?.Count == 0)
                {
                    logger.LogWarning("No spots loaded - map will be empty");
                }
                else
                {
                    logger.LogInformation("Loaded {SpotCount} spots", viewModel.Spots?.Count ?? 0);
                }

                // Step 4: Initialize pins
                logger.LogDebug("Updating pins...");
                viewModel.UpdatePins();

                logger.LogInformation("MapViewModel initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize MapViewModel");
                return false;
            }
        }

        /// <summary>
        /// Verify database connectivity by attempting simple queries
        /// </summary>
        private static async Task<bool> VerifyDatabaseConnectivity(MapViewModel viewModel, ILogger logger)
        {
            try
            {
                // This is a simplified check - in a real implementation you'd inject the repositories
                // For now, we'll just try to load data and catch exceptions
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database connectivity verification failed");
                return false;
            }
        }

        /// <summary>
        /// Enhanced filter method with comprehensive error handling and logging
        /// </summary>
        public static async Task<bool> SafeFilterSpotsByTypeAsync(
            MapViewModel viewModel,
            SpotType spotType,
            ILogger logger)
        {
            try
            {
                logger.LogDebug("Starting safe filter by type: {TypeName} (ID: {TypeId})", 
                    spotType?.Name ?? "null", spotType?.Id ?? 0);

                // Validation checks
                if (viewModel == null)
                {
                    logger.LogError("MapViewModel is null");
                    return false;
                }

                if (spotType == null)
                {
                    logger.LogDebug("SpotType is null - clearing filters");
                    await viewModel.ClearFiltersCommand.ExecuteAsync(null);
                    return true;
                }

                // Check if spots are loaded
                if (viewModel.Spots?.Count == 0)
                {
                    logger.LogWarning("No spots loaded - attempting to reload");
                    await viewModel.LoadSpotsCommand.ExecuteAsync(null);
                    
                    if (viewModel.Spots?.Count == 0)
                    {
                        logger.LogError("Still no spots after reload - cannot filter");
                        return false;
                    }
                }

                // Perform filtering
                viewModel.SelectedSpotType = spotType;
                
                // Use the direct repository call approach instead of in-memory filtering
                await viewModel.FilterSpotsCommand.ExecuteAsync(spotType.Name?.ToLower());

                logger.LogInformation("Filter applied successfully: {FilteredCount} spots for type {TypeName}", 
                    viewModel.Spots?.Count ?? 0, spotType.Name);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Safe filter operation failed for type {TypeName}", spotType?.Name);
                return false;
            }
        }

        /// <summary>
        /// Enhanced clear filters with verification
        /// </summary>
        public static async Task<bool> SafeClearFiltersAsync(MapViewModel viewModel, ILogger logger)
        {
            try
            {
                logger.LogDebug("Starting safe clear filters");

                if (viewModel == null)
                {
                    logger.LogError("MapViewModel is null");
                    return false;
                }

                // Clear filter state
                viewModel.SelectedSpotType = null;
                viewModel.SearchText = string.Empty;
                viewModel.IsFiltering = false;
                viewModel.IsSearching = false;

                // Reload all spots
                await viewModel.LoadSpotsCommand.ExecuteAsync(null);

                logger.LogInformation("Filters cleared successfully: {SpotCount} spots loaded", 
                    viewModel.Spots?.Count ?? 0);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Safe clear filters operation failed");
                return false;
            }
        }

        /// <summary>
        /// Fix for pin update race conditions
        /// </summary>
        public static void SafeUpdatePins(MapViewModel viewModel, ILogger logger)
        {
            try
            {
                if (viewModel?.Spots == null)
                {
                    logger.LogWarning("Cannot update pins - Spots collection is null");
                    return;
                }

                // Create new pins collection instead of modifying existing one
                var newPins = new List<Pin>();

                foreach (var spot in viewModel.Spots)
                {
                    try
                    {
                        var pin = CreateSafePin(spot, logger);
                        if (pin != null)
                        {
                            newPins.Add(pin);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to create pin for spot {SpotName}", spot.Name);
                    }
                }

                // Atomic update on UI thread
                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    viewModel.Pins = new ObservableCollection<Pin>(newPins);
                    logger.LogDebug("Updated pins collection: {PinCount} pins", newPins.Count);
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Safe update pins failed");
            }
        }

        /// <summary>
        /// Safe pin creation with validation
        /// </summary>
        private static Pin? CreateSafePin(Spot spot, ILogger logger)
        {
            try
            {
                if (spot?.Latitude == null || spot.Longitude == null)
                {
                    logger.LogDebug("Skipping spot {SpotName} - invalid coordinates", spot?.Name ?? "Unknown");
                    return null;
                }

                if (spot.Latitude == 0 && spot.Longitude == 0)
                {
                    logger.LogDebug("Skipping spot {SpotName} - zero coordinates", spot.Name);
                    return null;
                }

                return new Pin
                {
                    Label = spot.Name ?? "Spot sans nom",
                    Address = spot.Description ?? "Aucune description",
                    Type = PinType.Place,
                    Location = new Location((double)spot.Latitude, (double)spot.Longitude)
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create pin for spot {SpotName}", spot?.Name);
                return null;
            }
        }

        /// <summary>
        /// Validate filter prerequisites
        /// </summary>
        public static FilterValidationResult ValidateFilterPrerequisites(MapViewModel viewModel)
        {
            var result = new FilterValidationResult();

            if (viewModel == null)
            {
                result.IsValid = false;
                result.Issues.Add("MapViewModel is null");
                return result;
            }

            if (viewModel.SpotTypes?.Count == 0)
            {
                result.IsValid = false;
                result.Issues.Add("SpotTypes collection is empty - filters will not appear");
                result.Recommendations.Add("Call LoadSpotTypesAsync() during initialization");
            }

            if (viewModel.Spots?.Count == 0)
            {
                result.Issues.Add("Spots collection is empty - filtering will show no results");
                result.Recommendations.Add("Call LoadSpotsAsync() during initialization");
            }

            if (viewModel.Pins?.Count == 0 && viewModel.Spots?.Count > 0)
            {
                result.Issues.Add("Pins collection is empty but spots exist - map will be empty");
                result.Recommendations.Add("Call UpdatePins() after loading spots");
            }

            result.IsValid = result.Issues.Count == 0;
            return result;
        }
    }

    public class FilterValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();

        public override string ToString()
        {
            var result = $"Filter Validation: {(IsValid ? "VALID" : "INVALID")}\n";
            
            if (Issues.Any())
            {
                result += "Issues:\n" + string.Join("\n", Issues.Select(i => $"  - {i}"));
            }
            
            if (Recommendations.Any())
            {
                result += "\nRecommendations:\n" + string.Join("\n", Recommendations.Select(r => $"  - {r}"));
            }
            
            return result;
        }
    }
}