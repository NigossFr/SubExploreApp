using SubExplore.Models.Enums;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Base interface for validation events using Domain Events pattern
    /// </summary>
    public interface IValidationEvent
    {
        int SpotId { get; }
        int UserId { get; }
        DateTime Timestamp { get; }
        string EventType { get; }
        Dictionary<string, object> Metadata { get; }
    }

    /// <summary>
    /// Abstract base class for validation events
    /// </summary>
    public abstract class ValidationEventBase : IValidationEvent
    {
        public int SpotId { get; init; }
        public int UserId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public abstract string EventType { get; }
        public Dictionary<string, object> Metadata { get; init; } = new();
        public string Notes { get; init; } = string.Empty;
        public SpotValidationStatus PreviousStatus { get; init; }
    }

    /// <summary>
    /// Event fired when a spot is approved
    /// </summary>
    public class SpotApprovedEvent : ValidationEventBase
    {
        public override string EventType => "SpotApproved";
        public int ValidatorId { get; init; }
        public bool AutoApproved { get; init; }
        public TimeSpan ReviewDuration { get; init; }
    }

    /// <summary>
    /// Event fired when a spot is rejected
    /// </summary>
    public class SpotRejectedEvent : ValidationEventBase
    {
        public override string EventType => "SpotRejected";
        public int ValidatorId { get; init; }
        public List<string> RejectionReasons { get; init; } = new();
        public bool CanResubmit { get; init; } = true;
    }

    /// <summary>
    /// Event fired when a spot is assigned for review
    /// </summary>
    public class SpotAssignedForReviewEvent : ValidationEventBase
    {
        public override string EventType => "SpotAssignedForReview";
        public int ModeratorId { get; init; }
        public ModeratorSpecialization ModeratorSpecialization { get; init; }
        public DateTime ExpectedCompletionDate { get; init; }
    }

    /// <summary>
    /// Event fired when a spot is flagged for safety review
    /// </summary>
    public class SpotFlaggedForSafetyEvent : ValidationEventBase
    {
        public override string EventType => "SpotFlaggedForSafety";
        public int ReporterId { get; init; }
        public SafetyFlag SafetyFlag { get; init; } = new();
        public bool RequiresImmediateAttention { get; init; }
    }

    /// <summary>
    /// Event fired when safety review is completed
    /// </summary>
    public class SafetyReviewCompletedEvent : ValidationEventBase
    {
        public override string EventType => "SafetyReviewCompleted";
        public int ReviewerId { get; init; }
        public SafetyReviewResult ReviewResult { get; init; } = new();
        public bool SpotRemainsAccessible { get; init; }
    }

    /// <summary>
    /// Event fired when validation status changes
    /// </summary>
    public class ValidationStatusChangedEvent : ValidationEventBase
    {
        public override string EventType => "ValidationStatusChanged";
        public SpotValidationStatus NewStatus { get; init; }
        public string StatusChangeReason { get; init; } = string.Empty;
        public bool IsSystemGenerated { get; init; }
    }

    /// <summary>
    /// Event fired when a spot needs revision
    /// </summary>
    public class SpotNeedsRevisionEvent : ValidationEventBase
    {
        public override string EventType => "SpotNeedsRevision";
        public int ReviewerId { get; init; }
        public List<string> RequiredChanges { get; init; } = new();
        public DateTime RevisionDeadline { get; init; }
    }

    /// <summary>
    /// Event fired when validation metrics cross thresholds
    /// </summary>
    public class ValidationMetricThresholdEvent : IValidationEvent
    {
        public int SpotId => 0; // Not applicable for metric events
        public int UserId => 0; // System generated
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string EventType => "ValidationMetricThreshold";
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        public string MetricName { get; init; } = string.Empty;
        public double CurrentValue { get; init; }
        public double ThresholdValue { get; init; }
        public string ThresholdType { get; init; } = string.Empty; // "above", "below"
        public TrendAlertSeverity Severity { get; init; }
    }

    /// <summary>
    /// Event fired when moderator performance changes significantly
    /// </summary>
    public class ModeratorPerformanceEvent : IValidationEvent
    {
        public int SpotId => 0; // Not applicable
        public int UserId { get; init; } // Moderator ID
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string EventType => "ModeratorPerformance";
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        public ModeratorSpecialization Specialization { get; init; }
        public double PreviousQualityScore { get; init; }
        public double NewQualityScore { get; init; }
        public TimeSpan PreviousAverageReviewTime { get; init; }
        public TimeSpan NewAverageReviewTime { get; init; }
        public string PerformanceChangeType { get; init; } = string.Empty; // "improvement", "degradation"
    }

    /// <summary>
    /// Event handler interface for validation events
    /// </summary>
    public interface IValidationEventHandler<in T> where T : IValidationEvent
    {
        Task HandleAsync(T validationEvent, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Event publisher interface for decoupled event handling
    /// </summary>
    public interface IValidationEventPublisher
    {
        Task PublishAsync<T>(T validationEvent, CancellationToken cancellationToken = default) where T : IValidationEvent;
        Task PublishBatchAsync(IEnumerable<IValidationEvent> events, CancellationToken cancellationToken = default);
    }
}