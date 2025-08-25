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

        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            IDialogService dialogService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
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

        public string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "Une valeur requise est manquante.",
                ArgumentException => "Une valeur fournie n'est pas valide.",
                InvalidOperationException => "Cette opération ne peut pas être effectuée maintenant.",
                UnauthorizedAccessException => "Vous n'avez pas les permissions nécessaires.",
                TimeoutException => "L'opération a pris trop de temps. Veuillez réessayer.",
                System.Net.Http.HttpRequestException => "Erreur de connexion réseau. Vérifiez votre connexion internet.",
                _ => "Une erreur inattendue s'est produite. Veuillez réessayer."
            };
        }
    }
}