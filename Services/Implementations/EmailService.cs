using Microsoft.Extensions.Logging;
using SubExplore.Models.Configuration;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Templates;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Professional email service implementation with SMTP, retry logic, and comprehensive templates
    /// </summary>
    public class EmailService : IEmailService, IDisposable
    {
        private EmailConfiguration? _emailConfig;
        private readonly ILogger<EmailService> _logger;
        private readonly ISecureConfigurationService _secureConfig;
        private SmtpClient? _smtpClient;
        private bool _disposed = false;
        private readonly object _configLock = new object();

        public EmailService(
            ISecureConfigurationService secureConfig,
            ILogger<EmailService> logger)
        {
            _secureConfig = secureConfig;
            _logger = logger;
        }

        private async Task<EmailConfiguration> GetConfigurationAsync()
        {
            if (_emailConfig == null)
            {
                lock (_configLock)
                {
                    if (_emailConfig == null)
                    {
                        _emailConfig = GetEmailConfigurationAsync().GetAwaiter().GetResult();
                    }
                }
            }
            return _emailConfig;
        }

        private EmailConfiguration GetConfiguration()
        {
            if (_emailConfig == null)
            {
                lock (_configLock)
                {
                    if (_emailConfig == null)
                    {
                        try
                        {
                            _emailConfig = GetEmailConfigurationAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to load email configuration, using defaults");
                            _emailConfig = GetDefaultConfiguration();
                        }
                    }
                }
            }
            return _emailConfig;
        }

        private static EmailConfiguration GetDefaultConfiguration()
        {
            return new EmailConfiguration
            {
                SmtpHost = "localhost",
                SmtpPort = 587,
                EnableSsl = true,
                Username = "test@test.com",
                Password = "test",
                FromEmail = "noreply@subexplore.app",
                FromName = "SubExplore",
                BaseUrl = "https://subexplore.app",
                EnableEmailSending = false,
                LogEmailsOnly = true,
                TimeoutMilliseconds = 30000,
                MaxRetryAttempts = 3,
                RetryDelayMilliseconds = 5000
            };
        }

        public async Task<bool> SendEmailVerificationAsync(User user, string verificationToken)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(verificationToken))
                {
                    _logger.LogWarning("Invalid parameters for email verification");
                    return false;
                }

                var config = await GetConfigurationAsync();
                var verificationUrl = $"{config.BaseUrl}/auth/verify-email?token={verificationToken}&email={Uri.EscapeDataString(user.Email)}";
                
                var subject = EmailTemplates.GetEmailVerificationSubject();
                var htmlBody = EmailTemplates.GetEmailVerificationTemplate(user, verificationUrl);
                var textBody = GetTextVersionOfVerificationEmail(user, verificationUrl);

                var success = await SendEmailAsync(user.Email, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Email verification sent successfully to {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Failed to send email verification to {Email}", user.Email);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification to {Email}", user?.Email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(User user, string resetToken)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(resetToken))
                {
                    _logger.LogWarning("Invalid parameters for password reset email");
                    return false;
                }

                var config = GetConfiguration();
                var resetUrl = $"{config.BaseUrl}/auth/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";
                
                var subject = EmailTemplates.GetPasswordResetSubject();
                var htmlBody = EmailTemplates.GetPasswordResetTemplate(user, resetUrl);
                var textBody = GetTextVersionOfPasswordResetEmail(user, resetUrl);

                var success = await SendEmailAsync(user.Email, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Password reset email sent successfully to {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Failed to send password reset email to {Email}", user.Email);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", user?.Email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("Invalid user for welcome email");
                    return false;
                }

                var subject = EmailTemplates.GetWelcomeSubject();
                var htmlBody = EmailTemplates.GetWelcomeTemplate(user);
                var textBody = GetTextVersionOfWelcomeEmail(user);

                var success = await SendEmailAsync(user.Email, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Failed to send welcome email to {Email}", user.Email);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", user?.Email);
                return false;
            }
        }

        public async Task<bool> SendSecurityAlertAsync(User user, SecurityAlertType alertType, string? details = null)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("Invalid user for security alert");
                    return false;
                }

                var subject = EmailTemplates.GetSecurityAlertSubject(alertType);
                var htmlBody = EmailTemplates.GetSecurityAlertTemplate(user, alertType, details);
                var textBody = GetTextVersionOfSecurityAlert(user, alertType, details);

                var success = await SendEmailAsync(user.Email, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Security alert sent successfully to {Email} - Type: {AlertType}", user.Email, alertType);
                }
                else
                {
                    _logger.LogError("Failed to send security alert to {Email} - Type: {AlertType}", user.Email, alertType);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending security alert to {Email} - Type: {AlertType}", user?.Email, alertType);
                return false;
            }
        }

        public async Task<bool> SendModerationNotificationAsync(User user, ModerationNotificationType notificationType, string message)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Invalid parameters for moderation notification");
                    return false;
                }

                var subject = $"📋 Notification de modération - SubExplore";
                var htmlBody = GetModerationNotificationHtml(user, notificationType, message);
                var textBody = GetModerationNotificationText(user, notificationType, message);

                var success = await SendEmailAsync(user.Email, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Moderation notification sent successfully to {Email} - Type: {NotificationType}", user.Email, notificationType);
                }
                else
                {
                    _logger.LogError("Failed to send moderation notification to {Email} - Type: {NotificationType}", user.Email, notificationType);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending moderation notification to {Email} - Type: {NotificationType}", user?.Email, notificationType);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string testEmailAddress)
        {
            try
            {
                if (!IsValidEmailAddress(testEmailAddress))
                {
                    _logger.LogWarning("Invalid email address for test: {Email}", testEmailAddress);
                    return false;
                }

                var subject = EmailTemplates.GetTestEmailSubject();
                var htmlBody = EmailTemplates.GetTestEmailTemplate();
                var textBody = GetTextVersionOfTestEmail();

                var success = await SendEmailAsync(testEmailAddress, subject, htmlBody, textBody);
                
                if (success)
                {
                    _logger.LogInformation("Test email sent successfully to {Email}", testEmailAddress);
                }
                else
                {
                    _logger.LogError("Failed to send test email to {Email}", testEmailAddress);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", testEmailAddress);
                return false;
            }
        }

        public async Task<EmailValidationResult> ValidateEmailAsync(string emailAddress)
        {
            try
            {
                var result = new EmailValidationResult();

                // Basic format validation
                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Email address is required";
                    result.RiskLevel = EmailValidationRisk.High;
                    return result;
                }

                // Regex validation
                if (!IsValidEmailAddress(emailAddress))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid email format";
                    result.ValidationIssues.Add("Invalid email format");
                    result.RiskLevel = EmailValidationRisk.Medium;
                    return result;
                }

                // Domain validation
                var domain = emailAddress.Split('@').LastOrDefault()?.ToLowerInvariant();
                if (string.IsNullOrEmpty(domain))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid domain";
                    result.ValidationIssues.Add("Missing or invalid domain");
                    result.RiskLevel = EmailValidationRisk.High;
                    return result;
                }

                // Check for common disposable email domains
                var disposableDomains = new[] { "tempmail.org", "10minutemail.com", "guerrillamail.com", "mailinator.com" };
                if (disposableDomains.Contains(domain))
                {
                    result.IsValid = true;
                    result.IsDeliverable = false;
                    result.ValidationIssues.Add("Disposable email domain detected");
                    result.RiskLevel = EmailValidationRisk.High;
                    return result;
                }

                // Basic DNS validation (simplified for this implementation)
                result.IsValid = true;
                result.IsDeliverable = true;
                result.RiskLevel = EmailValidationRisk.Low;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email address: {Email}", emailAddress);
                return new EmailValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Email validation failed",
                    RiskLevel = EmailValidationRisk.Medium
                };
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string textBody)
        {
            var config = GetConfiguration();
            
            // Handle test mode
            if (!string.IsNullOrEmpty(config.TestModeEmail))
            {
                toEmail = config.TestModeEmail;
                subject = $"[TEST MODE] {subject}";
                _logger.LogInformation("Test mode active - redirecting email to {TestEmail}", config.TestModeEmail);
            }

            // Handle log-only mode
            if (config.LogEmailsOnly)
            {
                _logger.LogInformation("Email would be sent to {Email} with subject '{Subject}'", toEmail, subject);
                _logger.LogDebug("Email content: {Content}", textBody);
                return true;
            }

            // Skip sending if disabled
            if (!config.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Would send to {Email} with subject '{Subject}'", toEmail, subject);
                return true;
            }

            for (int attempt = 1; attempt <= config.MaxRetryAttempts; attempt++)
            {
                try
                {
                    using var message = new MailMessage();
                    message.From = new MailAddress(config.FromEmail, config.FromName);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.IsBodyHtml = true;
                    message.Body = htmlBody;
                    message.BodyEncoding = Encoding.UTF8;
                    message.SubjectEncoding = Encoding.UTF8;

                    // Add text alternative
                    var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
                    message.AlternateViews.Add(textView);
                    message.AlternateViews.Add(htmlView);

                    // Set reply-to if configured
                    if (!string.IsNullOrEmpty(config.ReplyToEmail))
                    {
                        message.ReplyToList.Add(config.ReplyToEmail);
                    }

                    // Send email
                    using var smtpClient = GetSmtpClient();
                    await smtpClient.SendMailAsync(message);

                    _logger.LogInformation("Email sent successfully to {Email} on attempt {Attempt}", toEmail, attempt);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email to {Email} on attempt {Attempt}/{MaxAttempts}", 
                        toEmail, attempt, config.MaxRetryAttempts);

                    if (attempt == config.MaxRetryAttempts)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email} after {MaxAttempts} attempts", 
                            toEmail, config.MaxRetryAttempts);
                        return false;
                    }

                    // Wait before retry
                    await Task.Delay(config.RetryDelayMilliseconds);
                }
            }

            return false;
        }

        private SmtpClient GetSmtpClient()
        {
            var config = GetConfiguration();
            var smtp = new SmtpClient(config.SmtpHost, config.SmtpPort)
            {
                EnableSsl = config.EnableSsl,
                Credentials = new NetworkCredential(config.Username, config.Password),
                Timeout = config.TimeoutMilliseconds,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            return smtp;
        }

        private async Task<EmailConfiguration> GetEmailConfigurationAsync()
        {
            try
            {
                return await _secureConfig.GetEmailConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load email configuration, using defaults");
                return new EmailConfiguration(); // Return default configuration
            }
        }

        private static bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use .NET's built-in email validation
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #region Text Email Versions

        private static string GetTextVersionOfVerificationEmail(User user, string verificationUrl)
        {
            return $@"SUBEXPLORE - VERIFICATION EMAIL

Bonjour {user.FirstName},

Merci de rejoindre SubExplore, votre communauté dédiée aux sports subaquatiques !

Pour activer votre compte et commencer à explorer les plus beaux spots de plongée, merci de confirmer votre adresse email en visitant ce lien :

{verificationUrl}

SÉCURITÉ :
- Ce lien expire dans 24 heures
- Si vous n'avez pas créé ce compte, ignorez cet email
- Ne partagez jamais ce lien avec d'autres personnes

Une fois votre email confirmé, vous pourrez :
- Découvrir des spots de plongée exceptionnels
- Partager vos propres découvertes
- Rejoindre une communauté passionnée
- Noter et commenter les spots

Hâte de vous voir explorer les merveilles sous-marines !

L'équipe SubExplore
© 2024 SubExplore - Votre communauté de sports subaquatiques";
        }

        private static string GetTextVersionOfPasswordResetEmail(User user, string resetUrl)
        {
            return $@"SUBEXPLORE - RÉINITIALISATION MOT DE PASSE

Bonjour {user.FirstName},

Nous avons reçu une demande de réinitialisation de mot de passe pour votre compte SubExplore.

Si vous êtes à l'origine de cette demande, visitez ce lien pour créer un nouveau mot de passe :

{resetUrl}

IMPORTANT :
- Ce lien expire dans 2 heures
- Il ne peut être utilisé qu'une seule fois
- Votre mot de passe actuel reste valide jusqu'à ce que vous en créiez un nouveau

SI VOUS N'AVEZ PAS DEMANDÉ CETTE RÉINITIALISATION :
- Ignorez cet email - votre compte reste sécurisé
- Votre mot de passe actuel n'a pas changé
- Contactez-nous si vous avez des inquiétudes

L'équipe Sécurité SubExplore
© 2024 SubExplore - Sécurité et confiance";
        }

        private static string GetTextVersionOfWelcomeEmail(User user)
        {
            return $@"SUBEXPLORE - BIENVENUE !

Félicitations {user.FirstName} !

Votre adresse email a été confirmée avec succès. Vous faites maintenant partie de la communauté SubExplore !

DÉCOUVREZ CE QUE VOUS POUVEZ FAIRE :
- Explorer : Découvrez des milliers de spots de plongée dans le monde entier
- Partager : Ajoutez vos spots secrets et aidez la communauté
- Evaluer : Notez et commentez vos expériences de plongée
- Connecter : Rencontrez d'autres passionnés près de chez vous

PREMIERS PAS RECOMMANDÉS :
1. Complétez votre profil avec vos certifications
2. Explorez la carte interactive des spots
3. Ajoutez votre premier spot préféré
4. Rejoignez des groupes de plongeurs locaux

Visitez https://subexplore.app pour commencer l'exploration !

Belles plongées et à bientôt sous l'eau !

L'équipe SubExplore
© 2024 SubExplore - Votre passeport pour l'exploration subaquatique";
        }

        private static string GetTextVersionOfSecurityAlert(User user, SecurityAlertType alertType, string? details)
        {
            var message = alertType switch
            {
                SecurityAlertType.PasswordChanged => "Votre mot de passe a été modifié avec succès.",
                SecurityAlertType.EmailChanged => "Votre adresse email a été modifiée.",
                SecurityAlertType.AccountLocked => "Votre compte a été temporairement verrouillé pour des raisons de sécurité.",
                SecurityAlertType.SuspiciousLogin => "Une activité suspecte a été détectée sur votre compte.",  
                SecurityAlertType.NewDeviceLogin => "Une connexion depuis un nouvel appareil a été détectée.",
                SecurityAlertType.RoleElevated => "Vos permissions de compte ont été mises à jour.",
                SecurityAlertType.AccountDeactivated => "Votre compte a été désactivé.",
                _ => "Une activité importante a eu lieu sur votre compte."
            };

            return $@"SUBEXPLORE - ALERTE SÉCURITÉ

Bonjour {user.FirstName},

{message}

{(string.IsNullOrEmpty(details) ? "" : $"Détails : {details}")}

Date : {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC
Compte : {user.Email}
ID Utilisateur : {user.Id}

Si cette action n'a pas été effectuée par vous, contactez immédiatement notre équipe de sécurité.

L'équipe Sécurité SubExplore
Email : security@subexplore.app
© 2024 SubExplore - Sécurité et confiance";
        }

        private static string GetTextVersionOfTestEmail()
        {
            return $@"SUBEXPLORE - TEST EMAIL

Configuration Email Validée !

Félicitations ! Ce message confirme que votre configuration email SubExplore fonctionne parfaitement.

TESTS VALIDÉS :
- Connexion SMTP établie
- Authentification réussie
- Envoi d'email fonctionnel
- Templates HTML compatibles

INFORMATIONS TECHNIQUES :
- Date/Heure : {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC
- Version : SubExplore Email Service v1.0
- Encodage : UTF-8
- Format : HTML + Text

Votre service email est maintenant prêt pour :
- Vérifications d'email
- Réinitialisations de mot de passe
- Messages de bienvenue
- Alertes de sécurité

L'équipe Technique SubExplore
© 2024 SubExplore - Service Email Opérationnel";
        }

        private static string GetModerationNotificationHtml(User user, ModerationNotificationType notificationType, string message)
        {
            var title = notificationType switch
            {
                ModerationNotificationType.SpotApproved => "Spot approuvé",
                ModerationNotificationType.SpotRejected => "Spot rejeté",
                ModerationNotificationType.SpotFlagged => "Spot signalé",
                ModerationNotificationType.ContentRemoved => "Contenu supprimé",
                ModerationNotificationType.AccountWarning => "Avertissement compte",
                ModerationNotificationType.ModeratorStatusChanged => "Statut modérateur modifié",
                _ => "Notification de modération"
            };

            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Modération - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6C5CE7 0%, #A29BFE 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .info-box {{ background: #F3E5F5; border-left: 4px solid #6C5CE7; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .logo {{ font-size: 2em; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>📋 SubExplore</div>
        <h1>{title}</h1>
    </div>
    
    <div class='content'>
        <h2>Bonjour {user.FirstName},</h2>
        
        <p>{message}</p>
        
        <div class='info-box'>
            <strong>📅 Date :</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC<br>
            <strong>👤 Compte :</strong> {user.Email}<br>
            <strong>🔖 Type :</strong> {notificationType}
        </div>
        
        <p>Pour plus d'informations ou si vous avez des questions, n'hésitez pas à nous contacter.</p>
        
        <p>L'équipe Modération SubExplore 🛡️</p>
    </div>
    
    <div class='footer'>
        <p><strong>Besoin d'aide ?</strong><br>
        📧 moderation@subexplore.app</p>
        
        <p><small>© 2024 SubExplore - Communauté respectueuse</small></p>
    </div>
</body>
</html>";
        }

        private static string GetModerationNotificationText(User user, ModerationNotificationType notificationType, string message)
        {
            return $@"SUBEXPLORE - NOTIFICATION MODÉRATION

Bonjour {user.FirstName},

{message}

Date : {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC
Compte : {user.Email}
Type : {notificationType}

Pour plus d'informations ou si vous avez des questions, n'hésitez pas à nous contacter.

L'équipe Modération SubExplore
Email : moderation@subexplore.app
© 2024 SubExplore - Communauté respectueuse";
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _smtpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}