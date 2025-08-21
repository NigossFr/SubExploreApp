using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Base interface for validation commands using Command Pattern
    /// </summary>
    public interface IValidationCommand
    {
        Guid SpotId { get; }
        Guid UserId { get; }
        DateTime Timestamp { get; }
        Task<ValidationResult> ExecuteAsync(IValidationContext context);
        Task<ValidationResult> UndoAsync(IValidationContext context);
    }

    /// <summary>
    /// Context for validation command execution (placeholder interface)
    /// </summary>
    public interface IValidationContext
    {
        // TODO: Define validation context interface when implementation is ready
    }

    /// <summary>
    /// Abstract base class for validation commands
    /// </summary>
    public abstract class ValidationCommandBase : IValidationCommand
    {
        public Guid SpotId { get; init; }
        public Guid UserId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string Notes { get; init; } = string.Empty;

        protected ValidationCommandBase(Guid spotId, Guid userId, string notes = "")
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
    }

    /// <summary>
    /// Command to approve a spot
    /// </summary>
    public class ApproveSpotCommand : ValidationCommandBase
    {
        public bool UpdateSafetyReview { get; init; }

        public ApproveSpotCommand(Guid spotId, Guid userId, string notes = "", bool updateSafetyReview = true)
            : base(spotId, userId, notes)
        {
            UpdateSafetyReview = updateSafetyReview;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // TODO: Implement validation logic when IValidationContext is ready
            await Task.CompletedTask;
            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to reject a spot
    /// </summary>
    public class RejectSpotCommand : ValidationCommandBase
    {
        public List<string> RejectionReasons { get; init; } = new();

        public RejectSpotCommand(Guid spotId, Guid userId, string notes, List<string>? rejectionReasons = null)
            : base(spotId, userId, notes)
        {
            RejectionReasons = rejectionReasons ?? new List<string>();
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // TODO: Implement validation logic when IValidationContext is ready
            await Task.CompletedTask;
            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to assign spot for review
    /// </summary>
    public class AssignForReviewCommand : ValidationCommandBase
    {
        public ModeratorSpecialization? PreferredSpecialization { get; init; }

        public AssignForReviewCommand(Guid spotId, Guid userId, Guid moderatorId, string notes = "", ModeratorSpecialization? preferredSpecialization = null)
            : base(spotId, moderatorId, notes)
        {
            PreferredSpecialization = preferredSpecialization;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // TODO: Implement validation logic when IValidationContext is ready
            await Task.CompletedTask;
            return ValidationResult.CreateSuccess();
        }
    }

    /// <summary>
    /// Command to flag a spot for safety review
    /// </summary>
    public class FlagForSafetyReviewCommand : ValidationCommandBase
    {
        public SafetyFlag SafetyFlag { get; init; }

        public FlagForSafetyReviewCommand(Guid spotId, Guid userId, SafetyFlag safetyFlag, string notes = "")
            : base(spotId, userId, notes)
        {
            SafetyFlag = safetyFlag;
        }

        public override async Task<ValidationResult> ExecuteAsync(IValidationContext context)
        {
            // TODO: Implement validation logic when IValidationContext is ready
            await Task.CompletedTask;
            return ValidationResult.CreateSuccess();
        }
    }
}