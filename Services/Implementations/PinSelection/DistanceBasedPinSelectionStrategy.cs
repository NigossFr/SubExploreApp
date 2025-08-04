using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations.PinSelection
{
    /// <summary>
    /// Optimized distance-based pin selection strategy with dynamic tolerance
    /// Improved version of the original algorithm with better performance and accuracy
    /// </summary>
    public class DistanceBasedPinSelectionStrategy : IPinSelectionStrategy
    {
        public string StrategyName => "Distance-Based Selection";

        private readonly ILogger<DistanceBasedPinSelectionStrategy>? _logger;

        public DistanceBasedPinSelectionStrategy(ILogger<DistanceBasedPinSelectionStrategy>? logger = null)
        {
            _logger = logger;
        }

        public async Task<Spot?> SelectPinAsync(Location clickLocation, IEnumerable<Pin> visiblePins, MapSpan? mapViewport = null)
        {
            try
            {
                if (clickLocation == null || !visiblePins.Any())
                {
                    _logger?.LogDebug("No click location or visible pins provided");
                    return null;
                }

                var tolerance = CalculateOptimalTolerance(mapViewport);
                var candidates = new List<PinCandidate>();

                _logger?.LogDebug("Evaluating {PinCount} pins with {Tolerance}km tolerance", 
                    visiblePins.Count(), tolerance);

                // Find all pins within tolerance and calculate their scores
                foreach (var pin in visiblePins)
                {
                    if (pin?.Location == null || pin.BindingContext is not Spot spot)
                        continue;

                    var distance = CalculateHaversineDistance(
                        clickLocation.Latitude, clickLocation.Longitude,
                        pin.Location.Latitude, pin.Location.Longitude);

                    if (distance <= tolerance)
                    {
                        var score = CalculateSelectionScore(distance, tolerance, spot);
                        candidates.Add(new PinCandidate(spot, distance, score));

                        _logger?.LogDebug("Candidate: {SpotName} at {Distance:F3}km (score: {Score:F2})", 
                            spot.Name, distance, score);
                    }
                }

                if (!candidates.Any())
                {
                    _logger?.LogDebug("No pins found within {Tolerance}km tolerance", tolerance);
                    return null;
                }

                // Return the highest scoring candidate (closest distance, best priority)
                var selectedCandidate = candidates.OrderByDescending(c => c.Score).First();
                
                _logger?.LogDebug("Selected: {SpotName} at {Distance:F3}km with score {Score:F2}", 
                    selectedCandidate.Spot.Name, selectedCandidate.Distance, selectedCandidate.Score);

                return selectedCandidate.Spot;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in distance-based pin selection");
                return null;
            }
        }

        public bool IsApplicable(DevicePlatform platform, PinSelectionContext context)
        {
            // Distance-based selection is universally applicable but not always optimal
            // Less suitable for high-density areas where native hit testing would be better
            return !context.IsHighDensityArea || context.TotalPinCount < 100;
        }

        /// <summary>
        /// Calculate optimal tolerance based on map viewport and zoom level
        /// </summary>
        private double CalculateOptimalTolerance(MapSpan? viewport)
        {
            if (viewport == null)
                return 2.0; // Default fallback

            // Calculate tolerance based on visible area
            var avgSpan = (viewport.LatitudeDegrees + viewport.LongitudeDegrees) / 2;

            return avgSpan switch
            {
                > 1.0 => 5.0,   // Very zoomed out - generous tolerance
                > 0.5 => 3.0,   // Moderately zoomed out
                > 0.1 => 2.0,   // Medium zoom - standard tolerance
                > 0.05 => 1.0,  // Good zoom - precise tolerance
                _ => 0.5        // Very zoomed in - high precision
            };
        }

        /// <summary>
        /// Calculate Haversine distance between two coordinates
        /// Optimized version with validation
        /// </summary>
        private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Input validation
            if (double.IsNaN(lat1) || double.IsNaN(lon1) || double.IsNaN(lat2) || double.IsNaN(lon2))
                return double.MaxValue;

            const double earthRadiusKm = 6371.0;

            // Convert to radians
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var lat1Rad = lat1 * Math.PI / 180.0;
            var lat2Rad = lat2 * Math.PI / 180.0;

            // Haversine formula
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        /// <summary>
        /// Calculate selection score for ranking candidates
        /// Higher score = better candidate
        /// </summary>
        private static double CalculateSelectionScore(double distance, double tolerance, Spot spot)
        {
            // Base score inversely proportional to distance (closer = higher score)
            var distanceScore = 1.0 - (distance / tolerance);

            // Priority modifiers based on spot characteristics
            var priorityModifier = 1.0;

            // Boost approved spots
            if (spot.ValidationStatus == SpotValidationStatus.Approved)
                priorityModifier += 0.1;

            // Boost spots with photos
            // if (spot.HasMedia) priorityModifier += 0.05;

            return distanceScore * priorityModifier;
        }

        /// <summary>
        /// Internal class to represent a pin candidate with selection metadata
        /// </summary>
        private class PinCandidate
        {
            public Spot Spot { get; }
            public double Distance { get; }
            public double Score { get; }

            public PinCandidate(Spot spot, double distance, double score)
            {
                Spot = spot;
                Distance = distance;
                Score = score;
            }
        }
    }
}