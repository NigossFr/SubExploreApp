using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for managing spot validation workflow
    /// Implements requirements from section 3.1.5 - Moderation system
    /// </summary>
    public class SpotValidationService : ISpotValidationService
    {
        private readonly SubExploreDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public SpotValidationService(SubExploreDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<List<Spot>> GetPendingValidationSpotsAsync()
        {
            try
            {
                return await _context.Spots
                    .Include(s => s.Creator)
                    .Include(s => s.Type)
                    .Include(s => s.Media)
                    .Where(s => s.ValidationStatus == SpotValidationStatus.Pending)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting pending spots: {ex.Message}");
                return new List<Spot>();
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

        public async Task<bool> ValidateSpotAsync(int spotId, int validatorId, SpotValidationStatus status, string validationNotes)
        {
            try
            {
                var spot = await _context.Spots.FindAsync(spotId);
                if (spot == null) return false;

                var validator = await _context.Users.FindAsync(validatorId);
                if (validator == null) return false;

                // Vérifier les permissions du validateur
                if (!_authorizationService.CanPerformSpotAction(validator, spot, SpotAction.Moderate))
                {
                    System.Diagnostics.Debug.WriteLine($"[SpotValidationService] User {validatorId} not authorized to validate spots");
                    return false;
                }

                // Mettre à jour le statut du spot
                spot.ValidationStatus = status;
                
                if (status == SpotValidationStatus.Approved)
                {
                    spot.LastSafetyReview = DateTime.UtcNow;
                }

                // TODO: Ajouter l'entrée dans l'historique de validation
                // Ici on devrait créer une entrée SpotValidationHistory

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Spot {spotId} validated by {validatorId} with status {status}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error validating spot: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AssignSpotForReviewAsync(int spotId, int moderatorId)
        {
            try
            {
                var spot = await _context.Spots.FindAsync(spotId);
                if (spot == null) return false;

                var moderator = await _context.Users.FindAsync(moderatorId);
                if (moderator == null) return false;

                // Vérifier que le modérateur peut traiter ce type de spot
                var spotType = await _context.SpotTypes.FindAsync(spot.TypeId);
                if (spotType != null && !CanModerateSpotType(moderator.ModeratorSpecialization, spotType))
                {
                    System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Moderator {moderatorId} cannot moderate this spot type");
                    return false;
                }

                spot.ValidationStatus = SpotValidationStatus.UnderReview;
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Spot {spotId} assigned to moderator {moderatorId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error assigning spot for review: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FlagSpotForSafetyReviewAsync(int spotId, int reporterId, string safetyNotes)
        {
            try
            {
                var spot = await _context.Spots.FindAsync(spotId);
                if (spot == null) return false;

                // Mettre à jour les flags de sécurité
                var safetyFlags = new List<string>();
                if (!string.IsNullOrEmpty(spot.SafetyFlags))
                {
                    // TODO: Désérialiser les flags existants
                }
                
                safetyFlags.Add($"Signalé par utilisateur {reporterId}: {safetyNotes}");
                
                // TODO: Sérialiser les flags en JSON
                spot.SafetyFlags = string.Join("; ", safetyFlags);
                spot.ValidationStatus = SpotValidationStatus.SafetyReview;

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Spot {spotId} flagged for safety review");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error flagging spot for safety: {ex.Message}");
                return false;
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

        public async Task<SpotValidationStats> GetValidationStatsAsync()
        {
            try
            {
                var totalSpots = await _context.Spots.CountAsync();
                var pendingCount = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Pending);
                var underReviewCount = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.UnderReview);
                var approvedCount = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Approved);
                var rejectedCount = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.Rejected);
                var safetyFlaggedCount = await _context.Spots.CountAsync(s => s.ValidationStatus == SpotValidationStatus.SafetyReview);

                var approvalRate = totalSpots > 0 ? (double)approvedCount / totalSpots * 100 : 0;

                // TODO: Calculer le temps moyen de review à partir de l'historique
                var averageReviewTime = TimeSpan.FromHours(24); // Valeur par défaut

                var stats = new SpotValidationStats
                {
                    PendingCount = pendingCount,
                    UnderReviewCount = underReviewCount,
                    ApprovedCount = approvedCount,
                    RejectedCount = rejectedCount,
                    SafetyFlaggedCount = safetyFlaggedCount,
                    TotalSpots = totalSpots,
                    ApprovalRate = approvalRate,
                    AverageReviewTime = averageReviewTime,
                    ModeratorStats = await GetModeratorPerformanceAsync()
                };

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting validation stats: {ex.Message}");
                return new SpotValidationStats();
            }
        }

        public bool CanModerateSpotType(ModeratorSpecialization moderatorSpecialization, SpotType spotType)
        {
            // Mapping des spécialisations aux types de spots
            return moderatorSpecialization switch
            {
                ModeratorSpecialization.DiveSpots => spotType.Category == ActivityCategory.Diving,
                ModeratorSpecialization.FreediveSpots => spotType.Category == ActivityCategory.Freediving,
                ModeratorSpecialization.SnorkelSpots => spotType.Category == ActivityCategory.Snorkeling,
                ModeratorSpecialization.UnderwaterPhotography => spotType.Category == ActivityCategory.UnderwaterPhotography,
                ModeratorSpecialization.TechnicalDiving => spotType.Category == ActivityCategory.Diving,
                ModeratorSpecialization.SafetyAndRegulations => true, // Peut modérer tous les types pour la sécurité
                ModeratorSpecialization.MarineConservation => true, // Peut modérer tous les types
                ModeratorSpecialization.CommunityManagement => true, // Admin général
                _ => false
            };
        }

        private async Task<List<ModeratorPerformance>> GetModeratorPerformanceAsync()
        {
            try
            {
                // TODO: Implémenter les vraies statistiques des modérateurs
                // Pour le moment, on retourne une liste vide
                var moderators = await _context.Users
                    .Where(u => u.AccountType == AccountType.ExpertModerator || 
                               u.AccountType == AccountType.Administrator)
                    .ToListAsync();

                var performance = new List<ModeratorPerformance>();
                
                foreach (var moderator in moderators)
                {
                    performance.Add(new ModeratorPerformance
                    {
                        ModeratorId = moderator.Id,
                        ModeratorName = moderator.DisplayName,
                        Specialization = moderator.ModeratorSpecialization,
                        SpotsReviewed = 0, // TODO: Calculer à partir de l'historique
                        SpotsApproved = 0,
                        SpotsRejected = 0,
                        AverageReviewTime = TimeSpan.FromHours(24),
                        ApprovalRate = 0
                    });
                }

                return performance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpotValidationService] Error getting moderator performance: {ex.Message}");
                return new List<ModeratorPerformance>();
            }
        }
    }
}