using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Email service for authentication workflows and notifications
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send email verification message to new user
        /// </summary>
        /// <param name="user">User who registered</param>
        /// <param name="verificationToken">Email verification token</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendEmailVerificationAsync(User user, string verificationToken);

        /// <summary>
        /// Send password reset email with secure token
        /// </summary>
        /// <param name="user">User requesting password reset</param>
        /// <param name="resetToken">Password reset token</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendPasswordResetEmailAsync(User user, string resetToken);

        /// <summary>
        /// Send welcome email after successful email verification
        /// </summary>
        /// <param name="user">Verified user</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendWelcomeEmailAsync(User user);

        /// <summary>
        /// Send security alert email for important account changes
        /// </summary>
        /// <param name="user">User affected by security change</param>
        /// <param name="alertType">Type of security alert</param>
        /// <param name="details">Additional details about the change</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendSecurityAlertAsync(User user, SecurityAlertType alertType, string? details = null);

        /// <summary>
        /// Send notification email for moderation activities
        /// </summary>
        /// <param name="user">User to notify</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="message">Notification message</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendModerationNotificationAsync(User user, ModerationNotificationType notificationType, string message);

        /// <summary>
        /// Test email connectivity and configuration
        /// </summary>
        /// <param name="testEmailAddress">Email address to send test to</param>
        /// <returns>True if test email sent successfully</returns>
        Task<bool> SendTestEmailAsync(string testEmailAddress);

        /// <summary>
        /// Validate email address format and deliverability
        /// </summary>
        /// <param name="emailAddress">Email address to validate</param>
        /// <returns>Email validation result</returns>
        Task<EmailValidationResult> ValidateEmailAsync(string emailAddress);
    }

    /// <summary>
    /// Types of security alerts for user notifications
    /// </summary>
    public enum SecurityAlertType
    {
        PasswordChanged,
        EmailChanged,
        AccountLocked,
        SuspiciousLogin,
        NewDeviceLogin,
        RoleElevated,
        AccountDeactivated
    }

    /// <summary>
    /// Types of moderation notifications
    /// </summary>
    public enum ModerationNotificationType
    {
        SpotApproved,
        SpotRejected,
        SpotFlagged,
        ContentRemoved,
        AccountWarning,
        ModeratorStatusChanged
    }

    /// <summary>
    /// Email validation result with detailed feedback
    /// </summary>
    public class EmailValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsDeliverable { get; set; }
        public string? ErrorMessage { get; set; }
        public EmailValidationRisk RiskLevel { get; set; }
        public List<string> ValidationIssues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Email risk levels for fraud prevention
    /// </summary>
    public enum EmailValidationRisk
    {
        Low,
        Medium,
        High,
        VeryHigh
    }
}