using SubExplore.Models.Domain;

namespace SubExplore.Repositories.Interfaces
{
    /// <summary>
    /// Repository for email verification token management
    /// </summary>
    public interface IEmailVerificationTokenRepository : IGenericRepository<EmailVerificationToken>
    {
        /// <summary>
        /// Get valid token by token hash
        /// </summary>
        /// <param name="tokenHash">Hashed token</param>
        /// <returns>Valid token if found</returns>
        Task<EmailVerificationToken?> GetValidTokenByHashAsync(string tokenHash);

        /// <summary>
        /// Get valid token by token hash and email
        /// </summary>
        /// <param name="tokenHash">Hashed token</param>
        /// <param name="email">Email address</param>
        /// <returns>Valid token if found</returns>
        Task<EmailVerificationToken?> GetValidTokenByHashAndEmailAsync(string tokenHash, string email);

        /// <summary>
        /// Get all valid tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of valid tokens</returns>
        Task<List<EmailVerificationToken>> GetValidTokensForUserAsync(int userId);

        /// <summary>
        /// Invalidate all tokens for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of tokens invalidated</returns>
        Task<int> InvalidateAllTokensForUserAsync(int userId);

        /// <summary>
        /// Invalidate all tokens for an email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Number of tokens invalidated</returns>
        Task<int> InvalidateAllTokensForEmailAsync(string email);

        /// <summary>
        /// Clean up expired tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// Check if user has reached maximum daily token requests
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="maxTokensPerDay">Maximum tokens per day</param>
        /// <returns>True if limit reached</returns>
        Task<bool> HasReachedDailyLimitAsync(int userId, int maxTokensPerDay = 3);

        /// <summary>
        /// Get token statistics for monitoring
        /// </summary>
        /// <returns>Token usage statistics</returns>
        Task<EmailTokenStatistics> GetTokenStatisticsAsync();
    }

    /// <summary>
    /// Email verification token statistics
    /// </summary>
    public class EmailTokenStatistics
    {
        public int TotalTokensCreated { get; set; }
        public int TokensUsedSuccessfully { get; set; }
        public int ExpiredTokens { get; set; }
        public int TokensExceedingAttempts { get; set; }
        public double SuccessRate => TotalTokensCreated > 0 ? (double)TokensUsedSuccessfully / TotalTokensCreated * 100 : 0;
        public DateTime StatisticsGeneratedAt { get; set; } = DateTime.UtcNow;
    }
}