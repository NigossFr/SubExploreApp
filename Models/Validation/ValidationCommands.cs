using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Base interface for validation commands using Command Pattern
    /// </summary>
    public interface IValidationCommand
    {
        int SpotId { get; }
        int UserId { get; }
        DateTime Timestamp { get; }
        Task<ValidationResult> ExecuteAsync(IValidationContext context);
        Task<ValidationResult> UndoAsync(IValidationContext context);
    }

    /// <summary>
    /// Context for validation command execution
    /// </summary>
    public interface IValidationContext
    {
        Task<Spot?> GetSpotAsync(int spotId);
        Task<User?> GetUserAsync(int userId);
        Task<bool> SaveSpotAsync(Spot spot);
        Task<bool> SaveValidationHistoryAsync(SpotValidationHistory history);
        Task<bool> CanUserPerformActionAsync(int userId, int spotId, ValidationActionType actionType);
        Task PublishEventAsync(IValidationEvent validationEvent);
    }

    /// <summary>
    /// Abstract base class for validation commands
    /// </summary>
    public abstract class ValidationCommandBase : IValidationCommand
    {
        public int SpotId { get; init; }
        public int UserId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string Notes { get; init; } = string.Empty;

        protected ValidationCommandBase(int spotId, int userId, string notes = "")
        {
            SpotId = spotId;
            UserId = userId;
            Notes = notes;
        }

        public abstract Task<ValidationResult> ExecuteAsync(IValidationContext context);
        public virtual Task<ValidationResult> UndoAsync(IValidationContext context)
        {
            return Task.FromResult(ValidationResult.CreateError("Undo not supported for this command"));
        }

        protected async Task<ValidationResult> ValidatePermissionsAsync(IValidationContext context, ValidationActionType actionType)
        {
            var canPerform = await context.CanUserPerformActionAsync(UserId, SpotId, actionType);
            if (!canPerform)
            {
                return ValidationResult.CreateError($"User {UserId} does not have permission to perform {actionType} on spot {SpotId}");
            }
            return ValidationResult.CreateSuccess();
        }

        protected async Task<ValidationResult> ValidateSpotExistsAsync(IValidationContext context)
        {
            var spot = await context.GetSpotAsync(SpotId);
            if (spot == null)
            {
                return ValidationResult.CreateError($"Spot with ID {SpotId} not found");
            }
            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to approve a spot
    /// </summary>
    public class ApproveSpotCommand : ValidationCommandBase
    {
        public bool UpdateSafetyReview { get; init; }

        public ApproveSpotCommand(int spotId, int userId, string notes = "", bool updateSafetyReview = true)
            : base(spotId, userId, notes)
        {
            UpdateSafetyReview = updateSafetyReview;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // Validate permissions
            var permissionResult = await ValidatePermissionsAsync(context, ValidationActionType.Approve);
            if (!permissionResult.Success) return permissionResult;

            // Validate spot exists
            var spotResult = await ValidateSpotExistsAsync(context);
            if (!spotResult.Success) return spotResult;

            var spot = await context.GetSpotAsync(SpotId);
            var user = await context.GetUserAsync(UserId);

            if (spot == null || user == null)
            {
                return ValidationResult.CreateError("Required entities not found");
            }

            // Update spot status
            var previousStatus = spot.ValidationStatus;
            spot.ValidationStatus = SpotValidationStatus.Approved;
            
            if (UpdateSafetyReview)
            {
                spot.LastSafetyReview = DateTime.UtcNow;
            }

            // Save changes
            var saveResult = await context.SaveSpotAsync(spot);
            if (!saveResult)
            {
                return ValidationResult.CreateError("Failed to save spot changes");
            }

            // Create validation history entry
            var history = new SpotValidationHistory
            {
                SpotId = SpotId,
                ValidatorId = UserId,
                ValidatorName = user.DisplayName,
                Status = SpotValidationStatus.Approved,
                ValidationNotes = Notes,
                ValidatedAt = Timestamp,
                ValidatorSpecialization = user.ModeratorSpecialization
            };

            await context.SaveValidationHistoryAsync(history);

            // Publish event
            await context.PublishEventAsync(new SpotApprovedEvent
            {
                SpotId = SpotId,
                ValidatorId = UserId,
                PreviousStatus = previousStatus,
                Timestamp = Timestamp,
                Notes = Notes
            });

            return ValidationResult.CreateSuccess();
        }

        public override async Task<ValidationResult> UndoAsync(IValidationContext context)
        {
            var spot = await context.GetSpotAsync(SpotId);
            if (spot == null)
            {
                return ValidationResult.CreateError("Spot not found for undo operation");
            }

            spot.ValidationStatus = SpotValidationStatus.UnderReview;
            var saveResult = await context.SaveSpotAsync(spot);
            
            return saveResult 
                ? ValidationResult.CreateSuccess() 
                : ValidationResult.CreateError("Failed to undo approval");
        }
    }

    /// <summary>
    /// Command to reject a spot
    /// </summary>
    public class RejectSpotCommand : ValidationCommandBase
    {
        public List<string> RejectionReasons { get; init; } = new();

        public RejectSpotCommand(int spotId, int userId, string notes, List<string>? rejectionReasons = null)
            : base(spotId, userId, notes)
        {
            RejectionReasons = rejectionReasons ?? new List<string>();
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // Validate permissions
            var permissionResult = await ValidatePermissionsAsync(context, ValidationActionType.Reject);
            if (!permissionResult.Success) return permissionResult;

            // Validate required notes
            if (string.IsNullOrWhiteSpace(Notes))
            {
                return ValidationResult.CreateError("Rejection notes are required");
            }

            var spot = await context.GetSpotAsync(SpotId);
            var user = await context.GetUserAsync(UserId);

            if (spot == null || user == null)
            {
                return ValidationResult.CreateError("Required entities not found");
            }

            // Update spot status
            var previousStatus = spot.ValidationStatus;
            spot.ValidationStatus = SpotValidationStatus.Rejected;

            // Save changes
            var saveResult = await context.SaveSpotAsync(spot);
            if (!saveResult)
            {
                return ValidationResult.CreateError("Failed to save spot changes");
            }

            // Create validation history entry
            var history = new SpotValidationHistory
            {
                SpotId = SpotId,
                ValidatorId = UserId,
                ValidatorName = user.DisplayName,
                Status = SpotValidationStatus.Rejected,
                ValidationNotes = Notes,
                ValidatedAt = Timestamp,
                ValidatorSpecialization = user.ModeratorSpecialization
            };

            await context.SaveValidationHistoryAsync(history);

            // Publish event
            await context.PublishEventAsync(new SpotRejectedEvent
            {
                SpotId = SpotId,
                ValidatorId = UserId,
                PreviousStatus = previousStatus,
                Timestamp = Timestamp,
                Notes = Notes,
                RejectionReasons = RejectionReasons
            });

            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to assign spot for review
    /// </summary>
    public class AssignForReviewCommand : ValidationCommandBase
    {
        public ModeratorSpecialization? PreferredSpecialization { get; init; }

        public AssignForReviewCommand(int spotId, int userId, int moderatorId, string notes = "", ModeratorSpecialization? preferredSpecialization = null)
            : base(spotId, moderatorId, notes)
        {
            PreferredSpecialization = preferredSpecialization;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            var permissionResult = await ValidatePermissionsAsync(context, ValidationActionType.AssignReview);
            if (!permissionResult.Success) return permissionResult;

            var spot = await context.GetSpotAsync(SpotId);
            var moderator = await context.GetUserAsync(UserId);

            if (spot == null || moderator == null)
            {
                return ValidationResult.CreateError("Required entities not found");
            }

            // Validate moderator can handle this spot type
            if (PreferredSpecialization.HasValue && moderator.ModeratorSpecialization != PreferredSpecialization.Value)
            {
                return ValidationResult.CreateError($"Moderator specialization mismatch. Required: {PreferredSpecialization}, Actual: {moderator.ModeratorSpecialization}");
            }

            var previousStatus = spot.ValidationStatus;
            spot.ValidationStatus = SpotValidationStatus.UnderReview;

            var saveResult = await context.SaveSpotAsync(spot);
            if (!saveResult)
            {
                return ValidationResult.CreateError("Failed to assign spot for review");
            }

            // Create history entry
            var history = new SpotValidationHistory
            {
                SpotId = SpotId,
                ValidatorId = UserId,
                ValidatorName = moderator.DisplayName,
                Status = SpotValidationStatus.UnderReview,
                ValidationNotes = $"Assigned for review: {Notes}",
                ValidatedAt = Timestamp,
                ValidatorSpecialization = moderator.ModeratorSpecialization
            };

            await context.SaveValidationHistoryAsync(history);

            // Publish event
            await context.PublishEventAsync(new SpotAssignedForReviewEvent
            {
                SpotId = SpotId,
                ModeratorId = UserId,
                PreviousStatus = previousStatus,
                Timestamp = Timestamp,
                Notes = Notes
            });

            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to flag spot for safety review
    /// </summary>
    public class FlagForSafetyReviewCommand : ValidationCommandBase
    {
        public SafetyFlag SafetyFlag { get; init; }

        public FlagForSafetyReviewCommand(int spotId, int userId, SafetyFlag safetyFlag, string notes = "")
            : base(spotId, userId, notes)
        {
            SafetyFlag = safetyFlag;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            var permissionResult = await ValidatePermissionsAsync(context, ValidationActionType.FlagSafety);
            if (!permissionResult.Success) return permissionResult;

            var spot = await context.GetSpotAsync(SpotId);
            if (spot == null)
            {
                return ValidationResult.CreateError("Spot not found");
            }

            var previousStatus = spot.ValidationStatus;
            spot.ValidationStatus = SpotValidationStatus.SafetyReview;

            // Update safety flags with structured data
            // This would require updating the Spot model to handle structured safety flags
            
            var saveResult = await context.SaveSpotAsync(spot);
            if (!saveResult)
            {
                return ValidationResult.CreateError("Failed to flag spot for safety review");
            }

            // Publish event
            await context.PublishEventAsync(new SpotFlaggedForSafetyEvent
            {
                SpotId = SpotId,
                ReporterId = UserId,
                SafetyFlag = SafetyFlag,
                PreviousStatus = previousStatus,
                Timestamp = Timestamp
            });

            return ValidationResult.CreateSuccess();
        }
    }
}