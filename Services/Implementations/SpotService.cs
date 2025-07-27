using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Comprehensive spot management service with advanced business logic
    /// Handles spot analytics, safety validation, recommendations, and data enrichment
    /// </summary>
    public class SpotService : ISpotService
    {
        private readonly ISpotRepository _spotRepository;
        private readonly ISpotMediaRepository _spotMediaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SpotService> _logger;
        
        public SpotService(
            ISpotRepository spotRepository,
            ISpotMediaRepository spotMediaRepository,
            IUserRepository userRepository,
            ILogger<SpotService> logger)
        {
            _spotRepository = spotRepository;
            _spotMediaRepository = spotMediaRepository;
            _userRepository = userRepository;
            _logger = logger;
        }
        
        #region Core Business Operations
        
        public async Task<Spot?> GetSpotWithFullDetailsAsync(int spotId)
        {
            try
            {
                _logger.LogInformation("Loading full details for spot {SpotId}", spotId);
                
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null)
                {
                    _logger.LogWarning("Spot {SpotId} not found", spotId);
                    return null;
                }
                
                // Load related data
                var media = await _spotMediaRepository.GetBySpotIdAsync(spotId);
                spot.Media = media.ToList();
                
                _logger.LogInformation("Successfully loaded spot {SpotId} with {MediaCount} media items", 
                    spotId, media.Count());
                
                return spot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading full details for spot {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetSpotsWithinRadiusAsync(double latitude, double longitude, double radiusKm, SpotValidationStatus? status = null)
        {
            try
            {
                _logger.LogInformation("Searching spots within {Radius}km of {Lat}, {Lon}", radiusKm, latitude, longitude);
                
                var allSpots = await _spotRepository.GetAllAsync();
                
                var spotsInRadius = allSpots.Where(spot =>
                {
                    if (status.HasValue && spot.ValidationStatus != status.Value)
                        return false;
                        
                    var distance = CalculateDistance(latitude, longitude, (double)spot.Latitude, (double)spot.Longitude);
                    return distance <= radiusKm;
                }).ToList();
                
                _logger.LogInformation("Found {Count} spots within radius", spotsInRadius.Count);
                return spotsInRadius;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching spots within radius");
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetSpotsByDifficultyAsync(DifficultyLevel difficulty)
        {
            try
            {
                var spots = await _spotRepository.GetAllAsync();
                return spots.Where(s => s.DifficultyLevel == difficulty && s.ValidationStatus == SpotValidationStatus.Approved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting spots by difficulty {Difficulty}", difficulty);
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetRecommendedSpotsAsync(int userId, int limit = 10)
        {
            try
            {
                _logger.LogInformation("Generating recommendations for user {UserId}", userId);
                
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for recommendations", userId);
                    return Array.Empty<Spot>();
                }
                
                var allSpots = await _spotRepository.GetAllAsync();
                var approvedSpots = allSpots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved);
                
                // Simple recommendation algorithm based on user expertise level
                var recommendedSpots = approvedSpots
                    .Where(spot => IsSpotSuitableForUser(spot, user))
                    .OrderByDescending(spot => CalculateRecommendationScore(spot, user))
                    .Take(limit)
                    .ToList();
                
                _logger.LogInformation("Generated {Count} recommendations for user {UserId}", recommendedSpots.Count, userId);
                return recommendedSpots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for user {UserId}", userId);
                throw;
            }
        }
        
        #endregion
        
        #region Safety and Validation
        
        public async Task<bool> ValidateSpotSafetyAsync(int spotId)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null) return false;
                
                // Safety validation logic
                var hasRequiredSafetyInfo = !string.IsNullOrEmpty(spot.SafetyNotes) &&
                                          !string.IsNullOrEmpty(spot.RequiredEquipment);
                
                var hasSafetyReview = spot.LastSafetyReview.HasValue &&
                                    spot.LastSafetyReview.Value > DateTime.UtcNow.AddMonths(-6);
                
                return hasRequiredSafetyInfo && hasSafetyReview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating safety for spot {SpotId}", spotId);
                return false;
            }
        }
        
        public async Task<SpotSafetyReport> GenerateSafetyReportAsync(int spotId)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null)
                    throw new ArgumentException($"Spot {spotId} not found");
                
                var report = new SpotSafetyReport
                {
                    SpotId = spotId,
                    SpotName = spot.Name,
                    SafetyScore = CalculateSafetyScore(spot),
                    LastReviewDate = spot.LastSafetyReview ?? DateTime.MinValue,
                    ReviewedBy = "System", // TODO: Track actual reviewer
                    RequiredEquipment = ParseEquipmentList(spot.RequiredEquipment),
                    RecommendedMaxCurrent = spot.CurrentStrength ?? CurrentStrength.None,
                    MinimumRequiredLevel = spot.DifficultyLevel,
                    RequiresGuide = spot.DifficultyLevel >= DifficultyLevel.Expert,
                    SafetyWarnings = GenerateSafetyWarnings(spot)
                };
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating safety report for spot {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<bool> IsSpotSafeForUserLevelAsync(int spotId, DifficultyLevel userLevel)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null) return false;
                
                // User level must be >= spot difficulty
                return userLevel >= spot.DifficultyLevel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking spot safety for user level");
                return false;
            }
        }
        
        public async Task UpdateSpotSafetyReviewAsync(int spotId, string reviewNotes, int reviewerId)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null)
                    throw new ArgumentException($"Spot {spotId} not found");
                
                spot.LastSafetyReview = DateTime.UtcNow;
                spot.SafetyNotes = reviewNotes;
                
                await _spotRepository.UpdateAsync(spot);
                
                _logger.LogInformation("Updated safety review for spot {SpotId} by reviewer {ReviewerId}", spotId, reviewerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating safety review for spot {SpotId}", spotId);
                throw;
            }
        }
        
        #endregion
        
        #region Visit Tracking and Analytics
        
        public async Task RecordSpotVisitAsync(int spotId, int userId)
        {
            try
            {
                // TODO: Implement visit tracking in database
                // For now, just log the visit
                _logger.LogInformation("Recording visit to spot {SpotId} by user {UserId}", spotId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording visit for spot {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<SpotStatistics> GetSpotStatisticsAsync(int spotId)
        {
            try
            {
                // TODO: Implement proper statistics from visit tracking
                // For now, return mock data
                return new SpotStatistics
                {
                    SpotId = spotId,
                    TotalVisits = 0,
                    UniqueVisitors = 0,
                    VisitsThisMonth = 0,
                    VisitsThisWeek = 0,
                    AverageRating = 0.0,
                    TotalReviews = 0,
                    LastVisitDate = DateTime.MinValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for spot {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetMostPopularSpotsAsync(int limit = 10, TimeSpan? period = null)
        {
            try
            {
                // TODO: Implement proper popularity ranking
                // For now, return approved spots ordered by creation date
                var spots = await _spotRepository.GetAllAsync();
                return spots
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most popular spots");
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetRecentlyAddedSpotsAsync(int limit = 10)
        {
            try
            {
                var spots = await _spotRepository.GetAllAsync();
                return spots
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Approved)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently added spots");
                throw;
            }
        }
        
        #endregion
        
        #region Rating and Reviews
        
        public async Task<double> GetSpotAverageRatingAsync(int spotId)
        {
            // TODO: Implement rating system
            return 0.0;
        }
        
        public async Task<int> GetSpotReviewCountAsync(int spotId)
        {
            // TODO: Implement review system
            return 0;
        }
        
        public async Task<bool> HasUserVisitedSpotAsync(int spotId, int userId)
        {
            // TODO: Implement visit tracking
            return false;
        }
        
        #endregion
        
        #region Search and Discovery
        
        public async Task<IEnumerable<Spot>> SearchSpotsAsync(string searchTerm, double? userLat = null, double? userLon = null)
        {
            try
            {
                var spots = await _spotRepository.GetAllAsync();
                var filteredSpots = spots.Where(spot =>
                    spot.ValidationStatus == SpotValidationStatus.Approved &&
                    (spot.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(spot.Description) && spot.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                );
                
                // Sort by distance if user location provided
                if (userLat.HasValue && userLon.HasValue)
                {
                    filteredSpots = filteredSpots.OrderBy(spot =>
                        CalculateDistance(userLat.Value, userLon.Value, (double)spot.Latitude, (double)spot.Longitude));
                }
                
                return filteredSpots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching spots with term {SearchTerm}", searchTerm);
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetSimilarSpotsAsync(int spotId, int limit = 5)
        {
            try
            {
                var targetSpot = await _spotRepository.GetByIdAsync(spotId);
                if (targetSpot == null) return Array.Empty<Spot>();
                
                var allSpots = await _spotRepository.GetAllAsync();
                
                var similarSpots = allSpots
                    .Where(s => s.Id != spotId && s.ValidationStatus == SpotValidationStatus.Approved)
                    .Where(s => s.DifficultyLevel == targetSpot.DifficultyLevel || s.TypeId == targetSpot.TypeId)
                    .OrderBy(s => CalculateDistance((double)targetSpot.Latitude, (double)targetSpot.Longitude,
                                                  (double)s.Latitude, (double)s.Longitude))
                    .Take(limit);
                
                return similarSpots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar spots for {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<IEnumerable<Spot>> GetSpotsNearUserAsync(int userId, double radiusKm = 50)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user?.Preferences == null) return Array.Empty<Spot>();
                
                // TODO: Get user's current location or saved location
                // For now, return all approved spots
                var spots = await _spotRepository.GetAllAsync();
                return spots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting spots near user {UserId}", userId);
                throw;
            }
        }
        
        #endregion
        
        #region Favorites and Collections
        
        public async Task<bool> ToggleFavoriteSpotAsync(int spotId, int userId)
        {
            // TODO: Implement favorites system
            return false;
        }
        
        public async Task<bool> IsSpotFavoriteAsync(int spotId, int userId)
        {
            // TODO: Implement favorites system
            return false;
        }
        
        public async Task<IEnumerable<Spot>> GetUserFavoriteSpotsAsync(int userId)
        {
            // TODO: Implement favorites system
            return Array.Empty<Spot>();
        }
        
        #endregion
        
        #region Data Enrichment
        
        public async Task<Models.Domain.WeatherInfo?> GetSpotCurrentWeatherAsync(int spotId)
        {
            // TODO: Integrate with weather API
            return null;
        }
        
        public async Task<TideInfo?> GetSpotTideInfoAsync(int spotId)
        {
            // TODO: Integrate with tide API
            return null;
        }
        
        public async Task<IEnumerable<Spot>> GetSpotsWithGoodConditionsAsync()
        {
            // TODO: Implement weather/condition filtering
            var spots = await _spotRepository.GetAllAsync();
            return spots.Where(s => s.ValidationStatus == SpotValidationStatus.Approved);
        }
        
        #endregion
        
        #region Quality Assurance
        
        public async Task<SpotQualityScore> CalculateSpotQualityScoreAsync(int spotId)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null)
                    throw new ArgumentException($"Spot {spotId} not found");
                
                var score = new SpotQualityScore
                {
                    SpotId = spotId,
                    CalculatedAt = DateTime.UtcNow
                };
                
                // Data completeness (40%)
                score.DataCompletenessScore = CalculateDataCompletenessScore(spot);
                
                // Safety information (30%)
                score.SafetyInformationScore = CalculateSafetyInformationScore(spot);
                
                // Media quality (20%)
                score.MediaQualityScore = await CalculateMediaQualityScoreAsync(spotId);
                
                // Community engagement (10%)
                score.CommunityEngagementScore = await CalculateCommunityEngagementScoreAsync(spotId);
                
                // Overall score calculation
                score.OverallScore = (int)(
                    score.DataCompletenessScore * 0.4 +
                    score.SafetyInformationScore * 0.3 +
                    score.MediaQualityScore * 0.2 +
                    score.CommunityEngagementScore * 0.1
                );
                
                score.IsRecommendedSpot = score.OverallScore >= 80;
                score.ImprovementSuggestions = GenerateImprovementSuggestions(spot, score);
                
                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating quality score for spot {SpotId}", spotId);
                throw;
            }
        }
        
        public async Task<bool> RequiresExpertValidationAsync(int spotId)
        {
            try
            {
                var spot = await _spotRepository.GetByIdAsync(spotId);
                if (spot == null) return false;
                
                return spot.DifficultyLevel >= DifficultyLevel.Expert ||
                       spot.CurrentStrength >= CurrentStrength.Strong ||
                       spot.MaxDepth > 30;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expert validation requirement for spot {SpotId}", spotId);
                return false;
            }
        }
        
        public async Task FlagSpotForReviewAsync(int spotId, string reason, int reporterId)
        {
            try
            {
                // TODO: Implement flagging system
                _logger.LogWarning("Spot {SpotId} flagged for review by user {ReporterId}: {Reason}", 
                    spotId, reporterId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging spot {SpotId} for review", spotId);
                throw;
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadiusKm * c;
        }
        
        private bool IsSpotSuitableForUser(Spot spot, User user)
        {
            // Check if spot difficulty matches user expertise
            // Convert ExpertiseLevel to DifficultyLevel for comparison
            if (!user.ExpertiseLevel.HasValue) return false;
            
            var userDifficultyLevel = ConvertExpertiseToDifficulty(user.ExpertiseLevel.Value);
            return spot.DifficultyLevel <= userDifficultyLevel;
        }
        
        private DifficultyLevel ConvertExpertiseToDifficulty(ExpertiseLevel expertise)
        {
            return expertise switch
            {
                ExpertiseLevel.Beginner => DifficultyLevel.Beginner,
                ExpertiseLevel.Intermediate => DifficultyLevel.Intermediate,
                ExpertiseLevel.Advanced => DifficultyLevel.Advanced,
                ExpertiseLevel.Expert => DifficultyLevel.Expert,
                ExpertiseLevel.Professional => DifficultyLevel.TechnicalOnly,
                _ => DifficultyLevel.Beginner
            };
        }
        
        private double CalculateRecommendationScore(Spot spot, User user)
        {
            double score = 0;
            
            if (!user.ExpertiseLevel.HasValue) return score;
            
            var userDifficultyLevel = ConvertExpertiseToDifficulty(user.ExpertiseLevel.Value);
            
            // Difficulty match (higher score for exact match)
            if (spot.DifficultyLevel == userDifficultyLevel)
                score += 50;
            else if (spot.DifficultyLevel < userDifficultyLevel)
                score += 30;
            
            // Recent safety review
            if (spot.LastSafetyReview.HasValue && spot.LastSafetyReview.Value > DateTime.UtcNow.AddMonths(-3))
                score += 20;
            
            // Has media
            if (spot.Media?.Any() == true)
                score += 15;
            
            // Complete safety information
            if (!string.IsNullOrEmpty(spot.SafetyNotes) && !string.IsNullOrEmpty(spot.RequiredEquipment))
                score += 15;
            
            return score;
        }
        
        private int CalculateSafetyScore(Spot spot)
        {
            int score = 0;
            
            // Has safety notes
            if (!string.IsNullOrEmpty(spot.SafetyNotes))
                score += 30;
            
            // Has required equipment list
            if (!string.IsNullOrEmpty(spot.RequiredEquipment))
                score += 25;
            
            // Recent safety review
            if (spot.LastSafetyReview.HasValue && spot.LastSafetyReview.Value > DateTime.UtcNow.AddMonths(-6))
                score += 25;
            
            // Appropriate difficulty rating
            if (spot.DifficultyLevel != DifficultyLevel.TechnicalOnly)
                score += 10;
            
            // Current strength documented
            if (spot.CurrentStrength != CurrentStrength.None)
                score += 10;
            
            return Math.Min(score, 100);
        }
        
        private List<string> ParseEquipmentList(string? equipment)
        {
            if (string.IsNullOrEmpty(equipment))
                return new List<string>();
                
            return equipment.Split(',', ';')
                          .Select(e => e.Trim())
                          .Where(e => !string.IsNullOrEmpty(e))
                          .ToList();
        }
        
        private List<string> GenerateSafetyWarnings(Spot spot)
        {
            var warnings = new List<string>();
            
            if (spot.CurrentStrength >= CurrentStrength.Strong)
                warnings.Add("Fort courant - Réservé aux plongeurs expérimentés");
            
            if (spot.MaxDepth > 30)
                warnings.Add("Plongée profonde - Certification avancée requise");
            
            if (spot.DifficultyLevel >= DifficultyLevel.Expert)
                warnings.Add("Niveau expert requis");
            
            if (!spot.LastSafetyReview.HasValue || spot.LastSafetyReview.Value < DateTime.UtcNow.AddMonths(-12))
                warnings.Add("Informations de sécurité à vérifier");
            
            return warnings;
        }
        
        private int CalculateDataCompletenessScore(Spot spot)
        {
            int score = 0;
            int totalFields = 10;
            
            if (!string.IsNullOrEmpty(spot.Name)) score++;
            if (!string.IsNullOrEmpty(spot.Description)) score++;
            if (spot.MaxDepth.HasValue) score++;
            if (spot.DifficultyLevel != 0) score++;
            if (!string.IsNullOrEmpty(spot.SafetyNotes)) score++;
            if (!string.IsNullOrEmpty(spot.RequiredEquipment)) score++;
            if (!string.IsNullOrEmpty(spot.BestConditions)) score++;
            if (spot.CurrentStrength != CurrentStrength.None) score++;
            if (spot.TypeId > 0) score++;
            if (spot.LastSafetyReview.HasValue) score++;
            
            return (score * 100) / totalFields;
        }
        
        private int CalculateSafetyInformationScore(Spot spot)
        {
            int score = 0;
            
            if (!string.IsNullOrEmpty(spot.SafetyNotes)) score += 40;
            if (!string.IsNullOrEmpty(spot.RequiredEquipment)) score += 30;
            if (spot.LastSafetyReview.HasValue) score += 30;
            
            return Math.Min(score, 100);
        }
        
        private async Task<int> CalculateMediaQualityScoreAsync(int spotId)
        {
            try
            {
                var media = await _spotMediaRepository.GetBySpotIdAsync(spotId);
                var mediaList = media.ToList();
                
                if (!mediaList.Any()) return 0;
                
                int score = Math.Min(mediaList.Count * 25, 100);
                return score;
            }
            catch
            {
                return 0;
            }
        }
        
        private async Task<int> CalculateCommunityEngagementScoreAsync(int spotId)
        {
            // TODO: Implement based on visits, reviews, favorites
            return 50; // Default score
        }
        
        private List<string> GenerateImprovementSuggestions(Spot spot, SpotQualityScore score)
        {
            var suggestions = new List<string>();
            
            if (score.DataCompletenessScore < 80)
                suggestions.Add("Complétez les informations manquantes (description, équipement requis, etc.)");
            
            if (score.SafetyInformationScore < 70)
                suggestions.Add("Ajoutez des notes de sécurité détaillées");
            
            if (score.MediaQualityScore < 50)
                suggestions.Add("Ajoutez des photos ou vidéos de qualité");
            
            if (!spot.LastSafetyReview.HasValue || spot.LastSafetyReview.Value < DateTime.UtcNow.AddMonths(-6))
                suggestions.Add("Effectuez une vérification de sécurité récente");
            
            return suggestions;
        }
        
        #endregion
    }
}