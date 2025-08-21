using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Centralized error handling service providing consistent error management across the application
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly IDialogService _dialogService;

        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            IDialogService dialogService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        /// <summary>
        /// Handle an exception with logging and optional user notification
        /// </summary>
        public async Task HandleExceptionAsync(Exception exception, string context, bool showToUser = true, string userMessage = null)
        {
            try
            {
                // Log the exception with full details
                var logLevel = IsCriticalException(exception) ? LogLevel.Critical : LogLevel.Error;
                
                _logger.Log(logLevel, exception, 
                    "Exception occurred in context: {Context}. Exception: {ExceptionType}", 
                    context, exception.GetType().Name);

                // Show user-friendly message if requested
                if (showToUser && _dialogService != null)
                {
                    var message = userMessage ?? GetUserFriendlyMessage(exception);
                    await _dialogService.ShowAlertAsync("Erreur", message, "D'accord");
                }
            }
            catch (Exception ex)
            {
                // Fallback logging if primary logging fails
                _logger.LogCritical(ex, "Failed to handle exception in context: {Context}. Original exception: {OriginalException}", 
                    context, exception?.Message ?? "Unknown");
            }
        }

        /// <summary>
        /// Handle an exception with logging only (no user notification)
        /// </summary>
        public async Task LogExceptionAsync(Exception exception, string context)
        {
            await HandleExceptionAsync(exception, context, showToUser: false);
        }

        /// <summary>
        /// Handle a validation error with user-friendly messaging
        /// </summary>
        public async Task HandleValidationErrorAsync(string validationMessage, string context)
        {
            _logger.LogWarning("Validation error in context: {Context}. Message: {ValidationMessage}", 
                context, validationMessage);

            if (_dialogService != null)
            {
                await _dialogService.ShowAlertAsync("Validation", validationMessage, "D'accord");
            }
        }

        /// <summary>
        /// Handle a network connectivity error
        /// </summary>
        public async Task HandleNetworkErrorAsync(Exception exception, string operation)
        {
            _logger.LogError(exception, "Network error during operation: {Operation}", operation);

            var userMessage = "Problème de connexion réseau. Vérifiez votre connexion internet et réessayez.";
            
            if (_dialogService != null)
            {
                await _dialogService.ShowAlertAsync("Connexion", userMessage, "D'accord");
            }
        }

        /// <summary>
        /// Handle a database operation error
        /// </summary>
        public async Task HandleDatabaseErrorAsync(Exception exception, string operation)
        {
            _logger.LogError(exception, "Database error during operation: {Operation}", operation);

            var userMessage = "Erreur de base de données. L'opération n'a pas pu être effectuée.";
            
            // Provide more specific messages for common database errors
            if (exception is DbUpdateException)
            {
                userMessage = "Impossible de sauvegarder les modifications. Veuillez réessayer.";
            }
            else if (exception is TimeoutException)
            {
                userMessage = "L'opération a pris trop de temps. Veuillez réessayer.";
            }
            else if (exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                userMessage = "Impossible de se connecter à la base de données. Vérifiez votre connexion.";
            }

            if (_dialogService != null)
            {
                await _dialogService.ShowAlertAsync("Base de données", userMessage, "D'accord");
            }
        }

        /// <summary>
        /// Check if an exception is critical and requires immediate attention
        /// </summary>
        public bool IsCriticalException(Exception exception)
        {
            return exception is OutOfMemoryException ||
                   exception is StackOverflowException ||
                   exception is AccessViolationException ||
                   exception is AppDomainUnloadedException ||
                   exception is BadImageFormatException ||
                   exception is CannotUnloadAppDomainException ||
                   exception is InvalidProgramException ||
                   exception is SystemException;
        }

        /// <summary>
        /// Get a user-friendly error message for an exception
        /// </summary>
        public string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "Données manquantes. Veuillez vérifier votre saisie.",
                ArgumentException => "Données invalides. Veuillez corriger votre saisie.",
                NotSupportedException => "Opération non supportée sur cet appareil.",
                TimeoutException => "L'opération a pris trop de temps. Veuillez réessayer.",
                SocketException => "Problème de connexion réseau. Vérifiez votre connexion.",
                HttpRequestException => "Erreur de communication avec le serveur.",
                TaskCanceledException => "L'opération a été annulée.",
                
                // Database exceptions
                DbUpdateConcurrencyException => "Les données ont été modifiées par un autre utilisateur.",
                DbUpdateException => "Impossible de sauvegarder les modifications.",
                InvalidOperationException when exception.Message.Contains("connection") => 
                    "Problème de connexion à la base de données.",
                
                // File/IO exceptions
                FileNotFoundException => "Fichier non trouvé.",
                DirectoryNotFoundException => "Dossier non trouvé.",
                UnauthorizedAccessException => "Accès refusé. Vérifiez vos permissions.",
                IOException => "Erreur de lecture/écriture du fichier.",
                
                // Network exceptions
                WebException => "Erreur de connexion réseau.",
                Exception when exception.Message.Contains("network", StringComparison.OrdinalIgnoreCase) => 
                    "Problème de connexion réseau.",
                
                // Memory exceptions
                OutOfMemoryException => "Mémoire insuffisante. Fermez d'autres applications.",
                
                // Default cases
                Exception when IsCriticalException(exception) => 
                    "Erreur critique. Veuillez redémarrer l'application.",
                _ => "Une erreur inattendue s'est produite. Veuillez réessayer."
            };
        }

        /// <summary>
        /// Create a correlation ID for tracking related operations
        /// </summary>
        private static string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// Sanitize sensitive information from error messages
        /// </summary>
        private static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Remove common sensitive patterns
            message = System.Text.RegularExpressions.Regex.Replace(message, @"password=\w+", "password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            message = System.Text.RegularExpressions.Regex.Replace(message, @"token=[\w-]+", "token=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            message = System.Text.RegularExpressions.Regex.Replace(message, @"key=[\w-]+", "key=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return message;
        }
    }
}