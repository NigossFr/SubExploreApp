using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Email verification service for user account activation
    /// </summary>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Send email verification to user
        /// </summary>
        /// <param name="user">User to verify</param>
        /// <param name="ipAddress">IP address of request</param>
        /// <returns>Verification result</returns>
        Task<EmailVerificationResult> SendVerificationEmailAsync(User user, string? ipAddress = null);

        /// <summary>
        /// Verify email using token
        /// </summary>
        /// <param name="token">Verification token</param>
        /// <param name="email">Email address</param>
        /// <param name="ipAddress">IP address of verification attempt</param>
        /// <returns>Verification result</returns>
        Task<EmailVerificationResult> VerifyEmailAsync(string token, string email, string? ipAddress = null);

        /// <summary>
        /// Resend verification email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="ipAddress">IP address of request</param>
        /// <returns>Verification result</returns>
        Task<EmailVerificationResult> ResendVerificationEmailAsync(string email, string? ipAddress = null);

        /// <summary>
        /// Check if email is already verified
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if verified</returns>
        Task<bool> IsEmailVerifiedAsync(string email);

        /// <summary>
        /// Get verification status for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Verification status details</returns>
        Task<EmailVerificationStatus> GetVerificationStatusAsync(int userId);

        /// <summary>
        /// Clean up expired verification tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();
    }

    /// <summary>
    /// Email verification result
    /// </summary>
    public class EmailVerificationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public EmailVerificationResultType ResultType { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public int? RemainingAttempts { get; set; }
        public TimeSpan? TokenExpiresIn { get; set; }
    }

    /// <summary>
    /// Email verification result types
    /// </summary>
    public enum EmailVerificationResultType
    {
        EmailSent,
        EmailVerified,
        AlreadyVerified,
        TokenExpired,
        TokenInvalid,
        TooManyAttempts,
        DailyLimitReached,
        UserNotFound,
        EmailMismatch,
        SendingFailed
    }

    /// <summary>
    /// Email verification status details
    /// </summary>
    public class EmailVerificationStatus
    {
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int PendingTokens { get; set; }
        public DateTime? LastTokenSent { get; set; }
        public bool CanRequestNewToken { get; set; }
        public TimeSpan? NextTokenAvailableIn { get; set; }
        public int TodayTokenCount { get; set; }
        public int MaxDailyTokens { get; set; }
    }
}