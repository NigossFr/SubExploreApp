namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for centralized error handling, logging, and user notification
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handle an exception with logging and optional user notification
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Context information about where the error occurred</param>
        /// <param name="showToUser">Whether to show a user-friendly message to the user</param>
        /// <param name="userMessage">Custom message to show to user (optional)</param>
        Task HandleExceptionAsync(Exception exception, string context, bool showToUser = true, string userMessage = null);

        /// <summary>
        /// Handle an exception with logging only (no user notification)
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Context information about where the error occurred</param>
        Task LogExceptionAsync(Exception exception, string context);

        /// <summary>
        /// Handle a validation error with user-friendly messaging
        /// </summary>
        /// <param name="validationMessage">The validation error message</param>
        /// <param name="context">Context information about the validation failure</param>
        Task HandleValidationErrorAsync(string validationMessage, string context);

        /// <summary>
        /// Handle a network connectivity error
        /// </summary>
        /// <param name="exception">The network exception</param>
        /// <param name="operation">The operation that was being attempted</param>
        Task HandleNetworkErrorAsync(Exception exception, string operation);

        /// <summary>
        /// Handle a database operation error
        /// </summary>
        /// <param name="exception">The database exception</param>
        /// <param name="operation">The database operation that failed</param>
        Task HandleDatabaseErrorAsync(Exception exception, string operation);

        /// <summary>
        /// Check if an exception is critical and requires immediate attention
        /// </summary>
        /// <param name="exception">The exception to evaluate</param>
        /// <returns>True if the exception is critical</returns>
        bool IsCriticalException(Exception exception);

        /// <summary>
        /// Get a user-friendly error message for an exception
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <returns>A user-friendly error message</returns>
        string GetUserFriendlyMessage(Exception exception);
    }
}