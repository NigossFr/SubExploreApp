using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Simple error handling service implementation for Supabase-based app
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly IDialogService _dialogService;
        private readonly IErrorCategorizationService? _errorCategorizationService;
        private readonly INetworkHealthService? _networkHealthService;

        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            IDialogService dialogService,
            IErrorCategorizationService? errorCategorizationService = null,
            INetworkHealthService? networkHealthService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _errorCategorizationService = errorCategorizationService;
            _networkHealthService = networkHealthService;
        }

        public async Task HandleExceptionAsync(Exception exception, string context, bool showToUser = true, string userMessage = null)
        {
            await LogExceptionAsync(exception, context);

            if (showToUser)
            {
                var message = userMessage ?? GetUserFriendlyMessage(exception);
                await _dialogService.ShowAlertAsync("Erreur", message, "OK");
            }
        }

        public Task LogExceptionAsync(Exception exception, string context)
        {
            _logger.LogError(exception, "Error in {Context}: {Message}", context, exception.Message);
            return Task.CompletedTask;
        }

        public async Task HandleValidationErrorAsync(string validationMessage, string context)
        {
            _logger.LogWarning("Validation error in {Context}: {Message}", context, validationMessage);
            await _dialogService.ShowAlertAsync("Erreur de validation", validationMessage, "OK");
        }

        public async Task HandleNetworkErrorAsync(Exception exception, string operation)
        {
            await LogExceptionAsync(exception, $"Network operation: {operation}");
            await _dialogService.ShowAlertAsync("Erreur réseau", 
                "Impossible de se connecter au service. Vérifiez votre connexion internet.", "OK");
        }

        public async Task HandleDatabaseErrorAsync(Exception exception, string operation)
        {
            await LogExceptionAsync(exception, $"Database operation: {operation}");
            await _dialogService.ShowAlertAsync("Erreur de données", 
                "Une erreur s'est produite lors de l'accès aux données. Veuillez réessayer.", "OK");
        }

        public bool IsCriticalException(Exception exception)
        {
            return exception is OutOfMemoryException ||
                   exception is StackOverflowException ||
                   exception is AccessViolationException ||
                   exception is AppDomainUnloadedException;
        }

        public async Task HandleNetworkErrorWithContextAsync(Exception exception, string operation, object? networkStatus = null)
        {
            await LogExceptionAsync(exception, $"Network operation: {operation}");
            
            var userMessage = GetContextualNetworkErrorMessage(exception, networkStatus);
            var actionText = GetNetworkErrorActionText(exception, networkStatus);
            
            await _dialogService.ShowAlertAsync("Erreur réseau", userMessage, actionText ?? "OK");
        }

        public async Task HandleNetworkErrorWithRetryAsync(Exception exception, string operation, Func<Task>? retryAction = null)
        {
            await LogExceptionAsync(exception, $"Network operation: {operation}");
            
            var userMessage = GetContextualNetworkErrorMessage(exception, null);
            
            if (retryAction != null && ShouldOfferRetry(exception))
            {
                bool shouldRetry = await _dialogService.ShowConfirmationAsync(
                    "Erreur réseau",
                    $"{userMessage}\n\nVoulez-vous réessayer ?",
                    "Réessayer",
                    "Annuler");
                
                if (shouldRetry)
                {
                    try
                    {
                        await retryAction();
                    }
                    catch (Exception retryException)
                    {
                        await HandleExceptionAsync(retryException, $"Retry: {operation}", true);
                    }
                }
            }
            else
            {
                await _dialogService.ShowAlertAsync("Erreur réseau", userMessage, "OK");
            }
        }

        public string GetUserFriendlyMessage(Exception exception)
        {
            // Use error categorization service if available
            if (_errorCategorizationService != null)
            {
                return _errorCategorizationService.GetUserFriendlyMessage(exception);
            }
            
            return exception switch
            {
                ArgumentNullException => "Une valeur requise est manquante.",
                ArgumentException => "Une valeur fournie n'est pas valide.",
                InvalidOperationException => "Cette opération ne peut pas être effectuée maintenant.",
                UnauthorizedAccessException => "Vous n'avez pas les permissions nécessaires.",
                TimeoutException => "L'opération a pris trop de temps. Veuillez réessayer.",
                System.Net.Http.HttpRequestException => GetContextualNetworkErrorMessage(exception, null),
                _ => "Une erreur inattendue s'est produite. Veuillez réessayer."
            };
        }

        private string GetContextualNetworkErrorMessage(Exception exception, object? networkStatus)
        {
            // Try to get network health context
            NetworkHealthStatus? healthStatus = null;
            if (networkStatus is NetworkHealthStatus status)
            {
                healthStatus = status;
            }
            else if (_networkHealthService != null)
            {
                healthStatus = _networkHealthService.CurrentStatus;
            }

            // Provide context-aware messaging
            if (healthStatus != null)
            {
                return healthStatus.Level switch
                {
                    NetworkHealthLevel.Offline => "Aucune connexion internet détectée. Vérifiez votre connexion et réessayez.",
                    NetworkHealthLevel.Critical => "Connexion très lente détectée. L'opération peut échouer ou prendre du temps.",
                    NetworkHealthLevel.Poor => "Connexion lente détectée. Certaines fonctionnalités peuvent être ralenties.",
                    NetworkHealthLevel.Fair when healthStatus.IsOnCellular => "Connexion mobile détectée. Considérez l'utilisation du WiFi pour de meilleures performances.",
                    _ => "Erreur de connexion réseau. Vérifiez votre connexion internet."
                };
            }

            // Fallback to basic network error messages
            return exception switch
            {
                System.Net.Http.HttpRequestException httpEx when httpEx.Message.Contains("timeout") => 
                    "La connexion a pris trop de temps. Vérifiez votre connexion et réessayez.",
                System.Net.Http.HttpRequestException httpEx when httpEx.Message.Contains("SSL") => 
                    "Erreur de sécurité de connexion. Veuillez réessayer.",
                TaskCanceledException => 
                    "L'opération a été annulée ou a pris trop de temps.",
                _ => "Erreur de connexion réseau. Vérifiez votre connexion internet."
            };
        }

        private string? GetNetworkErrorActionText(Exception exception, object? networkStatus)
        {
            if (ShouldOfferRetry(exception))
            {
                return "Réessayer";
            }
            return null;
        }

        private bool ShouldOfferRetry(Exception exception)
        {
            // Use error categorization service if available
            if (_errorCategorizationService != null)
            {
                return _errorCategorizationService.ShouldRetryError(exception);
            }
            
            // Fallback logic
            return exception switch
            {
                System.Net.Http.HttpRequestException => true,
                TaskCanceledException => true,
                TimeoutException => true,
                System.Net.Sockets.SocketException => true,
                UnauthorizedAccessException => false,
                ArgumentException => false,
                _ => false
            };
        }
    }
}