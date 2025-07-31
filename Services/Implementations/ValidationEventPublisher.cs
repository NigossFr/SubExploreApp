using SubExplore.Models.Validation;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Event publisher implementation for validation events
    /// Supports both synchronous and asynchronous event handling
    /// </summary>
    public class ValidationEventPublisher : IValidationEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationEventPublisher> _logger;

        public ValidationEventPublisher(IServiceProvider serviceProvider, ILogger<ValidationEventPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T validationEvent, CancellationToken cancellationToken = default) where T : IValidationEvent
        {
            try
            {
                _logger.LogInformation("Publishing validation event {EventType} for spot {SpotId}", 
                    validationEvent.EventType, validationEvent.SpotId);

                // Get all registered handlers for this event type
                var handlers = _serviceProvider.GetServices<IValidationEventHandler<T>>();

                var tasks = handlers.Select(handler => 
                    SafeHandleEventAsync(handler, validationEvent, cancellationToken));

                await Task.WhenAll(tasks);

                _logger.LogInformation("Successfully published validation event {EventType}", validationEvent.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing validation event {EventType} for spot {SpotId}", 
                    validationEvent.EventType, validationEvent.SpotId);
                throw;
            }
        }

        public async Task PublishBatchAsync(IEnumerable<IValidationEvent> events, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventList = events.ToList();
                _logger.LogInformation("Publishing batch of {EventCount} validation events", eventList.Count);

                var tasks = eventList.Select(evt => PublishEventAsync(evt, cancellationToken));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Successfully published batch of {EventCount} validation events", eventList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing batch validation events");
                throw;
            }
        }

        private async Task PublishEventAsync(IValidationEvent validationEvent, CancellationToken cancellationToken)
        {
            // Use reflection to call the generic PublishAsync method with the correct type
            var eventType = validationEvent.GetType();
            var method = typeof(ValidationEventPublisher).GetMethod(nameof(PublishAsync))?.MakeGenericMethod(eventType);
            
            if (method != null)
            {
                var task = (Task?)method.Invoke(this, new object[] { validationEvent, cancellationToken });
                if (task != null)
                {
                    await task;
                }
            }
        }

        private async Task SafeHandleEventAsync<T>(IValidationEventHandler<T> handler, T validationEvent, CancellationToken cancellationToken) 
            where T : IValidationEvent
        {
            try
            {
                await handler.HandleAsync(validationEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler {HandlerType} for event {EventType}", 
                    handler.GetType().Name, validationEvent.EventType);
                // Don't rethrow to prevent one failing handler from affecting others
            }
        }
    }

    /// <summary>
    /// Default validation event handlers for common operations
    /// </summary>
    public class NotificationEventHandler : 
        IValidationEventHandler<SpotApprovedEvent>,
        IValidationEventHandler<SpotRejectedEvent>,
        IValidationEventHandler<SpotFlaggedForSafetyEvent>
    {
        private readonly ILogger<NotificationEventHandler> _logger;

        public NotificationEventHandler(ILogger<NotificationEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(SpotApprovedEvent validationEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Spot {SpotId} approved by validator {ValidatorId}", 
                validationEvent.SpotId, validationEvent.ValidatorId);
            
            // TODO: Implement notification to spot creator
            // TODO: Update search indexes
            // TODO: Send email notification if configured
        }

        public async Task HandleAsync(SpotRejectedEvent validationEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Spot {SpotId} rejected by validator {ValidatorId} with reasons: {Reasons}", 
                validationEvent.SpotId, validationEvent.ValidatorId, string.Join(", ", validationEvent.RejectionReasons));
            
            // TODO: Implement notification to spot creator with feedback
            // TODO: Log rejection reasons for analytics
        }

        public async Task HandleAsync(SpotFlaggedForSafetyEvent validationEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Spot {SpotId} flagged for safety by user {ReporterId} - {SafetyFlagType}: {Description}", 
                validationEvent.SpotId, validationEvent.ReporterId, 
                validationEvent.SafetyFlag.Type, validationEvent.SafetyFlag.Description);
            
            // TODO: Notify safety moderators
            // TODO: Auto-escalate critical safety flags
        }
    }

    /// <summary>
    /// Analytics event handler for tracking validation metrics
    /// </summary>
    public class AnalyticsEventHandler : 
        IValidationEventHandler<SpotApprovedEvent>,
        IValidationEventHandler<SpotRejectedEvent>,
        IValidationEventHandler<ValidationStatusChangedEvent>
    {
        private readonly ILogger<AnalyticsEventHandler> _logger;

        public AnalyticsEventHandler(ILogger<AnalyticsEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(SpotApprovedEvent validationEvent, CancellationToken cancellationToken = default)
        {
            // TODO: Record approval metrics
            // TODO: Update moderator performance stats
            // TODO: Track approval trends
            _logger.LogInformation("Recording approval analytics for spot {SpotId}", validationEvent.SpotId);
        }

        public async Task HandleAsync(SpotRejectedEvent validationEvent, CancellationToken cancellationToken = default)
        {
            // TODO: Record rejection metrics and reasons
            // TODO: Analyze rejection patterns
            _logger.LogInformation("Recording rejection analytics for spot {SpotId}", validationEvent.SpotId);
        }

        public async Task HandleAsync(ValidationStatusChangedEvent validationEvent, CancellationToken cancellationToken = default)
        {
            // TODO: Track status change patterns
            // TODO: Calculate processing times
            _logger.LogInformation("Recording status change analytics for spot {SpotId}: {PreviousStatus} -> {NewStatus}", 
                validationEvent.SpotId, validationEvent.PreviousStatus, validationEvent.NewStatus);
        }
    }
}