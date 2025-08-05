using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    /// <summary>
    /// Email verification token repository implementation
    /// </summary>
    public class EmailVerificationTokenRepository : GenericRepository<EmailVerificationToken>, IEmailVerificationTokenRepository
    {
        public EmailVerificationTokenRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<EmailVerificationToken?> GetValidTokenByHashAsync(string tokenHash)
        {
            return await _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                          !t.IsUsed && 
                                          DateTime.UtcNow < t.ExpiresAt &&
                                          t.AttemptCount < t.MaxAttempts);
        }

        public async Task<EmailVerificationToken?> GetValidTokenByHashAndEmailAsync(string tokenHash, string email)
        {
            return await _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                          t.Email.ToLower() == email.ToLower() &&
                                          !t.IsUsed && 
                                          DateTime.UtcNow < t.ExpiresAt &&
                                          t.AttemptCount < t.MaxAttempts);
        }

        public async Task<List<EmailVerificationToken>> GetValidTokensForUserAsync(int userId)
        {
            return await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId && 
                           !t.IsUsed && 
                           DateTime.UtcNow < t.ExpiresAt &&
                           t.AttemptCount < t.MaxAttempts)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> InvalidateAllTokensForUserAsync(int userId)
        {
            var tokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return tokens.Count;
        }

        public async Task<int> InvalidateAllTokensForEmailAsync(string email)
        {
            var tokens = await _context.EmailVerificationTokens
                .Where(t => t.Email.ToLower() == email.ToLower() && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return tokens.Count;
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep expired tokens for 7 days for audit

            var expiredTokens = await _context.EmailVerificationTokens
                .Where(t => t.ExpiresAt < cutoffDate)
                .ToListAsync();

            _context.EmailVerificationTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            return expiredTokens.Count;
        }

        public async Task<bool> HasReachedDailyLimitAsync(int userId, int maxTokensPerDay = 3)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var tokensToday = await _context.EmailVerificationTokens
                .CountAsync(t => t.UserId == userId && 
                                t.CreatedAt >= today && 
                                t.CreatedAt < tomorrow);

            return tokensToday >= maxTokensPerDay;
        }

        public async Task<EmailTokenStatistics> GetTokenStatisticsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Last 30 days

            var tokens = await _context.EmailVerificationTokens
                .Where(t => t.CreatedAt >= cutoffDate)
                .ToListAsync();

            return new EmailTokenStatistics
            {
                TotalTokensCreated = tokens.Count,
                TokensUsedSuccessfully = tokens.Count(t => t.IsUsed && t.UsedAt.HasValue),
                ExpiredTokens = tokens.Count(t => t.IsExpired),
                TokensExceedingAttempts = tokens.Count(t => t.AttemptCount >= t.MaxAttempts),
                StatisticsGeneratedAt = DateTime.UtcNow
            };
        }
    }
}