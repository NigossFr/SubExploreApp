using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    /// <summary>
    /// Password reset token repository implementation
    /// </summary>
    public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
    {
        public PasswordResetTokenRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<PasswordResetToken?> GetValidTokenByHashAsync(string tokenHash)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                          !t.IsUsed && 
                                          DateTime.UtcNow < t.ExpiresAt &&
                                          t.AttemptCount < t.MaxAttempts);
        }

        public async Task<PasswordResetToken?> GetValidTokenByHashAndEmailAsync(string tokenHash, string email)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                          t.Email.ToLower() == email.ToLower() &&
                                          !t.IsUsed && 
                                          DateTime.UtcNow < t.ExpiresAt &&
                                          t.AttemptCount < t.MaxAttempts);
        }

        public async Task<List<PasswordResetToken>> GetValidTokensForUserAsync(int userId)
        {
            return await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && 
                           !t.IsUsed && 
                           DateTime.UtcNow < t.ExpiresAt &&
                           t.AttemptCount < t.MaxAttempts)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> InvalidateAllTokensForUserAsync(int userId)
        {
            var tokens = await _context.PasswordResetTokens
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
            var tokens = await _context.PasswordResetTokens
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

            var expiredTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < cutoffDate)
                .ToListAsync();

            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            return expiredTokens.Count;
        }

        public async Task<bool> HasReachedDailyLimitAsync(int userId, int maxResetsPerDay = 5)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var resetsToday = await _context.PasswordResetTokens
                .CountAsync(t => t.UserId == userId && 
                                t.CreatedAt >= today && 
                                t.CreatedAt < tomorrow);

            return resetsToday >= maxResetsPerDay;
        }

        public async Task<bool> HasEmailReachedDailyLimitAsync(string email, int maxResetsPerDay = 10)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var resetsToday = await _context.PasswordResetTokens
                .CountAsync(t => t.Email.ToLower() == email.ToLower() && 
                                t.CreatedAt >= today && 
                                t.CreatedAt < tomorrow);

            return resetsToday >= maxResetsPerDay;
        }

        public async Task<PasswordResetTokenStatistics> GetTokenStatisticsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Last 30 days

            var tokens = await _context.PasswordResetTokens
                .Where(t => t.CreatedAt >= cutoffDate)
                .ToListAsync();

            return new PasswordResetTokenStatistics
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