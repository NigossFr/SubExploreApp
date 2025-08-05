namespace SubExplore.Models.Configuration
{
    /// <summary>
    /// Email service configuration with SMTP settings and templates
    /// </summary>
    public class EmailConfiguration
    {
        /// <summary>
        /// SMTP server hostname (e.g., smtp.gmail.com, smtp.outlook.com)
        /// </summary>
        public string SmtpHost { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port (587 for TLS, 465 for SSL)
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Enable SSL/TLS encryption
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// SMTP authentication username (usually email address)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SMTP authentication password or app password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Sender email address for all outgoing emails
        /// </summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Sender display name
        /// </summary>
        public string FromName { get; set; } = "SubExplore";

        /// <summary>
        /// Reply-to email address for user responses
        /// </summary>
        public string? ReplyToEmail { get; set; }

        /// <summary>
        /// Base URL for email verification and password reset links
        /// </summary>
        public string BaseUrl { get; set; } = "https://subexplore.app";

        /// <summary>
        /// Email verification link expiration in hours
        /// </summary>
        public int EmailVerificationExpirationHours { get; set; } = 24;

        /// <summary>
        /// Password reset link expiration in hours
        /// </summary>
        public int PasswordResetExpirationHours { get; set; } = 2;

        /// <summary>
        /// Maximum number of password reset emails per day per user
        /// </summary>
        public int MaxPasswordResetEmailsPerDay { get; set; } = 5;

        /// <summary>
        /// Enable email sending (false for development/testing)
        /// </summary>
        public bool EnableEmailSending { get; set; } = true;

        /// <summary>
        /// Log emails to console/file instead of sending (for debugging)
        /// </summary>
        public bool LogEmailsOnly { get; set; } = false;

        /// <summary>
        /// Test mode - redirect all emails to this address
        /// </summary>
        public string? TestModeEmail { get; set; }

        /// <summary>
        /// SMTP connection timeout in milliseconds
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 30000;

        /// <summary>
        /// Maximum number of retry attempts for failed email sends
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 5000;
    }
}