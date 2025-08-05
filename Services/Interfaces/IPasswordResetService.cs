using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Password reset service for secure password recovery
    /// </summary>
    public interface IPasswordResetService
    {
        /// <summary>
        /// Request password reset for email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="ipAddress">IP address of request</param>
        /// <returns>Password reset result</returns>
        Task<PasswordResetResult> RequestPasswordResetAsync(string email, string? ipAddress = null);

        /// <summary>
        /// Validate password reset token
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="email">Email address</param>
        /// <returns>Token validation result</returns>
        Task<PasswordResetTokenValidation> ValidateResetTokenAsync(string token, string email);

        /// <summary>
        /// Reset password using valid token
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="email">Email address</param>
        /// <param name="newPassword">New password</param>
        /// <param name="ipAddress">IP address of reset attempt</param>
        /// <returns>Password reset result</returns>
        Task<PasswordResetResult> ResetPasswordAsync(string token, string email, string newPassword, string? ipAddress = null);

        /// <summary>
        /// Check if email has reached daily reset limit
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if limit reached</returns>
        Task<bool> HasReachedDailyLimitAsync(string email);

        /// <summary>
        /// Get password reset statistics
        /// </summary>
        /// <returns>Reset statistics</returns>
        Task<PasswordResetStatistics> GetResetStatisticsAsync();

        /// <summary>
        /// Clean up expired reset tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();
    }

    /// <summary>
    /// Password reset result
    /// </summary>
    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public PasswordResetResultType ResultType { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public DateTime? PasswordResetAt { get; set; }
        public int? RemainingAttempts { get; set; }
        public TimeSpan? TokenExpiresIn { get; set; }
    }

    /// <summary>
    /// Password reset result types
    /// </summary>
    public enum PasswordResetResultType
    {
        EmailSent,
        PasswordReset,
        TokenExpired,
        TokenInvalid,
        TooManyAttempts,
        DailyLimitReached,
        UserNotFound,
        EmailMismatch,
        SendingFailed,
        PasswordValidationFailed,
        UserNotVerified
    }

    /// <summary>
    /// Password reset token validation result
    /// </summary>
    public class PasswordResetTokenValidation
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public PasswordResetResultType ResultType { get; set; }
        public TimeSpan? ExpiresIn { get; set; }
        public int? RemainingAttempts { get; set; }
        public User? User { get; set; }
    }

    /// <summary>
    /// Password reset statistics
    /// </summary>
    public class PasswordResetStatistics
    {
        public int TotalResetRequests { get; set; }
        public int SuccessfulResets { get; set; }
        public int ExpiredTokens { get; set; }
        public int InvalidAttempts { get; set; }
        public double SuccessRate => TotalResetRequests > 0 ? (double)SuccessfulResets / TotalResetRequests * 100 : 0;
        public DateTime StatisticsGeneratedAt { get; set; } = DateTime.UtcNow;
    }
}