using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Validation;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Enhanced service for managing spot validation workflow
    /// Implements Command and Strategy patterns for extensible validation
    /// Implements requirements from section 3.1.5 - Moderation system
    /// </summary>
    public partial class SpotValidationService : ISpotValidationService, IValidationContext
    {
        private readonly SubExploreDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly IValidationStrategyFactory _strategyFactory;
        private readonly IValidationEventPublisher _eventPublisher;
        private readonly ILogger<SpotValidationService> _logger;

        public SpotValidationService(
            SubExploreDbContext context, 
            IAuthorizationService authorizationService,
            IValidationStrategyFactory strategyFactory,
            IValidationEventPublisher eventPublisher,
            ILogger<SpotValidationService> logger)
        {
            _context = context;
            _authorizationService = authorizationService;
            _strategyFactory = strategyFactory;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<ValidationResult<List<Spot>>> GetPendingValidationSpotsAsync(ValidationFilter? filter = null)
        {
            try
            {
                var query = _context.Spots
                    .Include(s => s.Creator)
                    .Include(s => s.Type)
                    .Include(s => s.Media)
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Pending);

                // Apply filters
                if (filter != null)
                {
                    query = ApplyValidationFilter(query, filter);
                }

                var spots = await query
                    .OrderBy(s => s.CreatedAt)
                    .Skip((filter?.Page - 1 ?? 0) * (filter?.PageSize ?? 20))
                    .Take(filter?.PageSize ?? 20)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} pending validation spots", spots.Count);
                return ValidationResult<List<Spot>>.CreateSuccess(spots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending validation spots");
                return ValidationResult<List<Spot>>.CreateError($"Failed to retrieve pending spots: {ex.Message}");
            }
        }

        public async Task<List<Spot>> GetSpotsUnderReviewAsync(int moderatorId)
        {
            try
            {
                return await _context.Spots
                    .Include(s => s.Creator)
                    .Include(s => s.Type)
                    .Include(s => s.Media)
                    .Where(s => s.ValidationStatus == SpotValidationStatus.UnderReview)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting spots under review: {ex.Message}");
                return new List<Spot>();
            }
        }

        public async Task<List<SpotValidationHistory>> GetSpotValidationHistoryAsync(int spotId)
        {
            try
            {
                // Pour le moment, on simule l'historique car on n'a pas encore la table
                // TODO: Implémenter une vraie table SpotValidationHistory
                var spot = await _context.Spots
                    .Include(s => s.Creator)
                    .FirstOrDefaultAsync(s => s.Id == spotId);

                if (spot == null) return new List<SpotValidationHistory>();

                var history = new List<SpotValidationHistory>();
                
                // Ajouter l'entrée de création
                history.Add(new SpotValidationHistory
                {
                    Id = 1,
                    SpotId = spotId,
                    ValidatorId = spot.CreatorId,
                    ValidatorName = spot.Creator?.DisplayName ?? "Utilisateur",
                    Status = SpotValidationStatus.Pending,
                    ValidationNotes = "Spot créé et en attente de validation",
                    ValidatedAt = spot.CreatedAt,
                    ValidatorSpecialization = ModeratorSpecialization.None
                });

                return history;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting validation history: {ex.Message}");
                return new List<SpotValidationHistory>();
            }
        }

        public async Task<ValidationResult> ExecuteValidationCommandAsync(IValidationCommand command)
        {
            try
            {
                _logger.LogInformation("Executing validation command for spot {SpotId} by user {UserId}", 
                    command.SpotId, command.UserId);

                var result = await command.ExecuteAsync(this);
                
                if (result.Success)
                {
                    _logger.LogInformation("Validation command executed successfully for spot {SpotId}", command.SpotId);
                }
                else
                {
                    _logger.LogWarning("Validation command failed for spot {SpotId}: {ErrorMessage}", 
                        command.SpotId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing validation command for spot {SpotId}", command.SpotId);
                return ValidationResult.CreateError($"Command execution failed: {ex.Message}");
            }
        }

        public async Task<ValidationResult> AssignSpotForReviewAsync(int spotId, int moderatorId)
        {
            try
            {
                var command = new AssignForReviewCommand(spotId, moderatorId, moderatorId);
                return await ExecuteValidationCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning spot {SpotId} for review to moderator {ModeratorId}", spotId, moderatorId);
                return ValidationResult.CreateError($"Failed to assign spot for review: {ex.Message}");
            }
        }

        public async Task<ValidationResult> FlagSpotForSafetyReviewAsync(int spotId, int reporterId, SafetyFlag safetyFlag)
        {
            try
            {
                var command = new FlagForSafetyReviewCommand(spotId, reporterId, safetyFlag);
                return await ExecuteValidationCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging spot {SpotId} for safety review", spotId);
                return ValidationResult.CreateError($"Failed to flag spot for safety review: {ex.Message}");
            }
        }

        public async Task<List<Spot>> GetSpotsFlaggedForSafetyAsync()
        {
            try
            {
                return await _context.Spots
                    .Include(s => s.Creator)
                    .Include(s => s.Type)
                    .Include(s => s.Media)
                    .Where(s => s.ValidationStatus == SpotValidationStatus.SafetyReview)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting safety flagged spots: {ex.Message}");
                return new List<Spot>();
            }
        }

        public async Task<bool> CompleteSafetyReviewAsync(int spotId, int reviewerId, bool isSafe, string reviewNotes)
        {
            try
            {
                var spot = await _context.Spots.FindAsync(spotId);
                if (spot == null) return false;

                var reviewer = await _context.Users.FindAsync(reviewerId);
                if (reviewer == null) return false;

                // Vérifier les permissions
                if (!_authorizationService.CanPerformSpotAction(reviewer, spot, SpotAction.SafetyReview))
                {
                    System.Diagnostics.Debug.WriteLine($"[SpotValidationService] User {reviewerId} not authorized for safety review");
                    return false;
                }

                spot.LastSafetyReview = DateTime.UtcNow;
                spot.ValidationStatus = isSafe ? SpotValidationStatus.Approved : SpotValidationStatus.Rejected;

                // Mettre à jour les notes de sécurité
                if (!string.IsNullOrEmpty(reviewNotes))
                {
                    spot.SafetyNotes = $"{spot.SafetyNotes}\n[Revue de sécurité {DateTime.UtcNow:yyyy-MM-dd}]: {reviewNotes}";
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Safety review completed for spot {spotId}: {(isSafe ? "Safe" : "Unsafe")}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error completing safety review: {ex.Message}");
                return false;
            }
        }

        public async Task<ValidationResult<Models.Validation.SpotValidationStats>> GetValidationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.Spots.AsQueryable();
                
                if (fromDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= toDate.Value);

                var totalSpots = await query.CountAsync();
                var pendingCount = await query.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Pending);
                var underReviewCount = await query.CountAsync(s => s.ValidationStatus == SpotValidationStatus.UnderReview);
                var approvedCount = await query.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Approved);
                var rejectedCount = await query.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Rejected);
                var safetyFlaggedCount = await query.CountAsync(s => s.ValidationStatus == SpotValidationStatus.SafetyReview);

                var approvalRate = totalSpots > 0 ? (double)approvedCount / totalSpots * 100 : 0;

                // Enhanced statistics with category breakdown
                var spotsByCategory = await query
                    .Include(s => s.Type)
                    .Where(s => s.Type != null)
                    .GroupBy(s => s.Type!.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                var stats = new Models.Validation.SpotValidationStats
                {
                    PendingCount = pendingCount,
                    UnderReviewCount = underReviewCount,
                    ApprovedCount = approvedCount,
                    RejectedCount = rejectedCount,
                    SafetyFlaggedCount = safetyFlaggedCount,
                    TotalSpots = totalSpots,
                    ApprovalRate = approvalRate,
                    AverageReviewTime = TimeSpan.FromHours(24), // TODO: Calculate from history
                    ModeratorStats = await GetModeratorPerformanceAsync(),
                    SpotsByCategory = spotsByCategory,
                    SafetyFlagsByType = new Dictionary<SafetyFlagType, int>(), // TODO: Implement
                    Trends = new ValidationTrends() // TODO: Implement trend analysis
                };

                _logger.LogInformation("Generated validation statistics: {TotalSpots} total, {ApprovalRate:F1}% approval rate", 
                    totalSpots, approvalRate);
                
                return ValidationResult<Models.Validation.SpotValidationStats>.CreateSuccess(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting validation statistics");
                return ValidationResult<Models.Validation.SpotValidationStats>.CreateError($"Failed to get validation statistics: {ex.Message}");
            }
        }

        public async Task<bool> CanModerateSpotTypeAsync(ModeratorSpecialization moderatorSpecialization, SpotType spotType)
        {
            try
            {
                var strategy = _strategyFactory.GetStrategy(spotType.Category);
                
                // Create a mock user with the specialization for validation
                var mockValidator = new User { ModeratorSpecialization = moderatorSpecialization };
                var mockSpot = new Spot { Type = spotType };
                
                var result = await strategy.CanValidateSpotAsync(mockSpot, mockValidator);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking moderation capability for specialization {Specialization} and spot type {SpotType}", 
                    moderatorSpecialization, spotType.Name);
                return false;
            }
        }

        // IValidationContext implementation
        public async Task<Spot?> GetSpotAsync(int spotId)
        {
            return await _context.Spots
                .Include(s => s.Creator)
                .Include(s => s.Type)
                .Include(s => s.Media)
                .FirstOrDefaultAsync(s => s.Id == spotId);
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> SaveSpotAsync(Spot spot)
        {
            try
            {
                _context.Spots.Update(spot);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving spot {SpotId}", spot.Id);
                return false;
            }
        }

        public async Task<bool> SaveValidationHistoryAsync(SpotValidationHistory history)
        {
            try
            {
                // TODO: Implement proper validation history table and repository
                // For now, we log the history entry
                _logger.LogInformation("Validation history: Spot {SpotId} {Status} by {ValidatorName} at {Timestamp}", 
                    history.SpotId, history.Status, history.ValidatorName, history.ValidatedAt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving validation history for spot {SpotId}", history.SpotId);
                return false;
            }
        }

        public async Task<bool> CanUserPerformActionAsync(int userId, int spotId, ValidationActionType actionType)
        {
            var user = await GetUserAsync(userId);
            var spot = await GetSpotAsync(spotId);
            
            if (user == null || spot == null) return false;

            // Convert ValidationActionType to SpotAction for existing authorization service
            var spotAction = actionType switch
            {
                ValidationActionType.Approve => SpotAction.Moderate,
                ValidationActionType.Reject => SpotAction.Moderate,
                ValidationActionType.AssignReview => SpotAction.Moderate,
                ValidationActionType.FlagSafety => SpotAction.Report,
                ValidationActionType.CompleteSafetyReview => SpotAction.SafetyReview,
                _ => SpotAction.View
            };

            return _authorizationService.CanPerformSpotAction(user, spot, spotAction);
        }

        public async Task PublishEventAsync(IValidationEvent validationEvent)
        {
            try
            {
                await _eventPublisher.PublishAsync(validationEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing validation event {EventType} for spot {SpotId}", 
                    validationEvent.EventType, validationEvent.SpotId);
            }
        }

        private IQueryable<Spot> ApplyValidationFilter(IQueryable<Spot> query, ValidationFilter filter)
        {
            if (filter.Status.HasValue)
                query = query.Where(s => s.ValidationStatus == filter.Status.Value);

            if (filter.ActivityCategory.HasValue)
                query = query.Where(s => s.Type != null && s.Type.Category == filter.ActivityCategory.Value);

            if (filter.MinDifficulty.HasValue)
                query = query.Where(s => s.DifficultyLevel >= filter.MinDifficulty.Value);

            if (filter.MaxDifficulty.HasValue)
                query = query.Where(s => s.DifficultyLevel <= filter.MaxDifficulty.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(s => s.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(s => s.CreatedAt <= filter.ToDate.Value);

            if (filter.HasSafetyFlags.HasValue)
            {
                if (filter.HasSafetyFlags.Value)
                    query = query.Where(s => !string.IsNullOrEmpty(s.SafetyFlags));
                else
                    query = query.Where(s => string.IsNullOrEmpty(s.SafetyFlags));
            }

            return query;
        }

        public async Task<ValidationResult<List<ValidationAction>>> GetAvailableActionsAsync(int spotId, int userId)
        {
            try
            {
                var spot = await GetSpotAsync(spotId);
                var user = await GetUserAsync(userId);

                if (spot == null || user == null)
                {
                    return ValidationResult<List<ValidationAction>>.CreateError("Spot or user not found");
                }

                var actions = new List<ValidationAction>();

                // Check available actions based on spot status and user permissions
                switch (spot.ValidationStatus)
                {
                    case SpotValidationStatus.Pending:
                        if (await CanUserPerformActionAsync(userId, spotId, ValidationActionType.Approve))
                        {
                            actions.Add(new ValidationAction
                            {
                                ActionId = "approve",
                                DisplayName = "Approuver",
                                Description = "Approuver ce spot pour publication",
                                Type = ValidationActionType.Approve,
                                RequiresNotes = false
                            });
                        }

                        if (await CanUserPerformActionAsync(userId, spotId, ValidationActionType.Reject))
                        {
                            actions.Add(new ValidationAction
                            {
                                ActionId = "reject",
                                DisplayName = "Rejeter",
                                Description = "Rejeter ce spot avec commentaires",
                                Type = ValidationActionType.Reject,
                                RequiresNotes = true
                            });
                        }

                        if (await CanUserPerformActionAsync(userId, spotId, ValidationActionType.AssignReview))
                        {
                            actions.Add(new ValidationAction
                            {
                                ActionId = "assign_review",
                                DisplayName = "Assigner pour révision",
                                Description = "Assigner ce spot pour une révision détaillée",
                                Type = ValidationActionType.AssignReview,
                                RequiresNotes = false
                            });
                        }
                        break;

                    case SpotValidationStatus.SafetyReview:
                        if (await CanUserPerformActionAsync(userId, spotId, ValidationActionType.CompleteSafetyReview))
                        {
                            actions.Add(new ValidationAction
                            {
                                ActionId = "complete_safety_review",
                                DisplayName = "Compléter révision sécurité",
                                Description = "Finaliser la révision de sécurité",
                                Type = ValidationActionType.CompleteSafetyReview,
                                RequiresNotes = true
                            });
                        }
                        break;
                }

                // Safety flagging is always available to authorized users
                if (await CanUserPerformActionAsync(userId, spotId, ValidationActionType.FlagSafety))
                {
                    actions.Add(new ValidationAction
                    {
                        ActionId = "flag_safety",
                        DisplayName = "Signaler problème sécurité",
                        Description = "Signaler un problème de sécurité sur ce spot",
                        Type = ValidationActionType.FlagSafety,
                        RequiresNotes = true
                    });
                }

                return ValidationResult<List<ValidationAction>>.CreateSuccess(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available actions for spot {SpotId} and user {UserId}", spotId, userId);
                return ValidationResult<List<ValidationAction>>.CreateError($"Failed to get available actions: {ex.Message}");
            }
        }

        private async Task<List<ModeratorPerformance>> GetModeratorPerformanceAsync()
        {
            try
            {
                var moderators = await _context.Users
                    .Where(u => u.AccountType == AccountType.ExpertModerator || 
                               u.AccountType == AccountType.Administrator)
                    .ToListAsync();

                var performance = new List<Models.Validation.ModeratorPerformance>();
                
                foreach (var moderator in moderators)
                {
                    // Enhanced performance calculation would go here
                    // For now, using placeholder values
                    performance.Add(new Models.Validation.ModeratorPerformance
                    {
                        ModeratorId = moderator.Id,
                        ModeratorName = moderator.DisplayName,
                        Specialization = moderator.ModeratorSpecialization,
                        SpotsReviewed = 0,
                        SpotsApproved = 0,
                        SpotsRejected = 0,
                        SafetyReviewsCompleted = 0,
                        AverageReviewTime = TimeSpan.FromHours(24),
                        ApprovalRate = 0,
                        QualityScore = 0.85, // Placeholder
                        Achievements = new List<string>()
                    });
                }

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting moderator performance");
                return new List<ModeratorPerformance>();
            }
        }
    }
}