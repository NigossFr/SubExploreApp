using Microsoft.Extensions.Logging;
using SubExplore.Models.Configuration;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Password reset service implementation with secure token management and rate limiting
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ISecureConfigurationService _secureConfig;
        private readonly ILogger<PasswordResetService> _logger;
        private const int TOKEN_LENGTH = 32;
        private const int MAX_DAILY_RESETS_PER_USER = 5;
        private const int MAX_DAILY_RESETS_PER_EMAIL = 10;
        private const int TOKEN_EXPIRATION_HOURS = 2;

        public PasswordResetService(
            IPasswordResetTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            ISecureConfigurationService secureConfig,
            ILogger<PasswordResetService> logger)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _secureConfig = secureConfig;
            _logger = logger;
        }

        public async Task<PasswordResetResult> RequestPasswordResetAsync(string email, string? ipAddress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Email address is required",
                        ResultType = PasswordResetResultType.UserNotFound
                    };
                }

                email = email.Trim().ToLowerInvariant();

                // Check email daily limit (across all users)
                if (await _tokenRepository.HasEmailReachedDailyLimitAsync(email, MAX_DAILY_RESETS_PER_EMAIL))
                {
                    _logger.LogWarning("Daily reset limit reached for email {Email}", email);
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = $"Maximum {MAX_DAILY_RESETS_PER_EMAIL} password reset requests per day allowed for this email",
                        ResultType = PasswordResetResultType.DailyLimitReached
                    };
                }

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal if email exists - security best practice
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    
                    // Return success to prevent email enumeration, but don't send email
                    return new PasswordResetResult
                    {
                        Success = true,
                        ResultType = PasswordResetResultType.EmailSent,
                        EmailSentAt = DateTime.UtcNow
                    };
                }

                // Check if user's email is verified
                if (!user.IsEmailConfirmed)
                {
                    _logger.LogWarning("Password reset requested for unverified email: {Email}", email);
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Email address must be verified before password reset",
                        ResultType = PasswordResetResultType.UserNotVerified
                    };
                }

                // Check user daily limit
                if (await _tokenRepository.HasReachedDailyLimitAsync(user.Id, MAX_DAILY_RESETS_PER_USER))
                {
                    _logger.LogWarning("Daily reset limit reached for user {UserId}", user.Id);
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = $"Maximum {MAX_DAILY_RESETS_PER_USER} password reset requests per day allowed",
                        ResultType = PasswordResetResultType.DailyLimitReached
                    };
                }

                // Invalidate any existing tokens for this user
                await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id);

                // Generate new secure token
                var token = GenerateSecureToken();
                var tokenHash = ComputeTokenHash(token);

                // Create token record
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    Email = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                    CreatedFromIP = ipAddress,
                    CreatedAt = DateTime.UtcNow,
                    MaxAttempts = 3,
                    ResetReason = "User requested password reset"
                };

                await _tokenRepository.AddAsync(resetToken);
                await _tokenRepository.SaveChangesAsync();

                // Send reset email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user, token);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent successfully to user {UserId} at {Email}", 
                        user.Id, user.Email);

                    return new PasswordResetResult
                    {
                        Success = true,
                        ResultType = PasswordResetResultType.EmailSent,
                        EmailSentAt = DateTime.UtcNow,
                        TokenExpiresIn = TimeSpan.FromHours(TOKEN_EXPIRATION_HOURS)
                    };
                }
                else
                {
                    _logger.LogError("Failed to send password reset email to user {UserId} at {Email}", 
                        user.Id, user.Email);

                    // Mark token as used since email failed
                    resetToken.IsUsed = true;
                    await _tokenRepository.UpdateAsync(resetToken);
                    await _tokenRepository.SaveChangesAsync();

                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to send password reset email",
                        ResultType = PasswordResetResultType.SendingFailed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for email {Email}", email);
                return new PasswordResetResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing password reset request",
                    ResultType = PasswordResetResultType.SendingFailed
                };
            }
        }

        public async Task<PasswordResetTokenValidation> ValidateResetTokenAsync(string token, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                {
                    return new PasswordResetTokenValidation
                    {
                        IsValid = false,
                        ErrorMessage = "Token and email are required",
                        ResultType = PasswordResetResultType.TokenInvalid
                    };
                }

                var tokenHash = ComputeTokenHash(token);
                var resetToken = await _tokenRepository.GetValidTokenByHashAndEmailAsync(tokenHash, email);

                if (resetToken == null)
                {
                    _logger.LogWarning("Invalid reset token attempted for email {Email}", email);
                    
                    // Try to find token to increment attempt count
                    var existingTokens = await _tokenRepository.FindAsync(
                        t => t.TokenHash == tokenHash && t.Email.ToLower() == email.ToLower());
                    var existingToken = existingTokens.FirstOrDefault();
                    
                    if (existingToken != null)
                    {
                        existingToken.AttemptCount++;
                        await _tokenRepository.UpdateAsync(existingToken);
                        await _tokenRepository.SaveChangesAsync();

                        if (existingToken.IsExpired)
                        {
                            return new PasswordResetTokenValidation
                            {
                                IsValid = false,
                                ErrorMessage = "Reset token has expired",
                                ResultType = PasswordResetResultType.TokenExpired
                            };
                        }

                        if (existingToken.AttemptCount >= existingToken.MaxAttempts)
                        {
                            return new PasswordResetTokenValidation
                            {
                                IsValid = false,
                                ErrorMessage = "Too many reset attempts",
                                ResultType = PasswordResetResultType.TooManyAttempts
                            };
                        }
                    }

                    return new PasswordResetTokenValidation
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid reset token",
                        ResultType = PasswordResetResultType.TokenInvalid
                    };
                }

                return new PasswordResetTokenValidation
                {
                    IsValid = true,
                    ExpiresIn = resetToken.ExpiresAt - DateTime.UtcNow,
                    RemainingAttempts = resetToken.MaxAttempts - resetToken.AttemptCount,
                    User = resetToken.User
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token for email {Email}", email);
                return new PasswordResetTokenValidation
                {
                    IsValid = false,
                    ErrorMessage = "An error occurred during token validation",
                    ResultType = PasswordResetResultType.TokenInvalid
                };
            }
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(string token, string email, string newPassword, string? ipAddress = null)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Token, email, and new password are required",
                        ResultType = PasswordResetResultType.TokenInvalid
                    };
                }

                // Validate password strength
                var passwordValidation = ValidatePassword(newPassword);
                if (!passwordValidation.IsValid)
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Password does not meet security requirements",
                        ValidationErrors = passwordValidation.Errors,
                        ResultType = PasswordResetResultType.PasswordValidationFailed
                    };
                }

                // Validate token
                var tokenValidation = await ValidateResetTokenAsync(token, email);
                if (!tokenValidation.IsValid)
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = tokenValidation.ErrorMessage,
                        ResultType = tokenValidation.ResultType,
                        RemainingAttempts = tokenValidation.RemainingAttempts
                    };
                }

                var user = tokenValidation.User!;
                var tokenHash = ComputeTokenHash(token);
                var resetToken = await _tokenRepository.GetValidTokenByHashAndEmailAsync(tokenHash, email);

                if (resetToken == null)
                {
                    return new PasswordResetResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid reset token",
                        ResultType = PasswordResetResultType.TokenInvalid
                    };
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Mark token as used
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;
                resetToken.UsedFromIP = ipAddress;
                await _tokenRepository.UpdateAsync(resetToken);

                // Invalidate all other tokens for this user
                await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id);

                await _tokenRepository.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user {UserId} at {Email}", user.Id, email);

                // Send security alert email
                try
                {
                    await _emailService.SendSecurityAlertAsync(user, SecurityAlertType.PasswordChanged, 
                        $"Password reset completed from IP: {ipAddress}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send security alert email to user {UserId}", user.Id);
                    // Don't fail password reset if security email fails
                }

                return new PasswordResetResult
                {
                    Success = true,
                    ResultType = PasswordResetResultType.PasswordReset,
                    PasswordResetAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email {Email}", email);
                return new PasswordResetResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during password reset",
                    ResultType = PasswordResetResultType.TokenInvalid
                };
            }
        }

        public async Task<bool> HasReachedDailyLimitAsync(string email)
        {
            try
            {
                return await _tokenRepository.HasEmailReachedDailyLimitAsync(email, MAX_DAILY_RESETS_PER_EMAIL);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking daily limit for email {Email}", email);
                return true; // Fail safe - assume limit reached
            }
        }

        public async Task<PasswordResetStatistics> GetResetStatisticsAsync()
        {
            try
            {
                var stats = await _tokenRepository.GetTokenStatisticsAsync();
                return new PasswordResetStatistics
                {
                    TotalResetRequests = stats.TotalTokensCreated,
                    SuccessfulResets = stats.TokensUsedSuccessfully,
                    ExpiredTokens = stats.ExpiredTokens,
                    InvalidAttempts = stats.TokensExceedingAttempts,
                    StatisticsGeneratedAt = stats.StatisticsGeneratedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting password reset statistics");
                return new PasswordResetStatistics();
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                return await _tokenRepository.CleanupExpiredTokensAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired reset tokens");
                return 0;
            }
        }

        private static string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[TOKEN_LENGTH];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private static string ComputeTokenHash(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashBytes);
        }

        private static (bool IsValid, List<string> Errors) ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return (false, errors);
            }

            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long");
            }

            if (password.Length > 128)
            {
                errors.Add("Password must not exceed 128 characters");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                errors.Add("Password must contain at least one digit");
            }

            if (!Regex.IsMatch(password, @"[@$!%*?&]"))
            {
                errors.Add("Password must contain at least one special character (@$!%*?&)");
            }

            // Check for common weak passwords
            var commonPasswords = new[] { "password", "123456789", "qwerty", "abc123", "password123" };
            if (commonPasswords.Any(common => password.ToLowerInvariant().Contains(common)))
            {
                errors.Add("Password contains common patterns and is too weak");
            }

            return (errors.Count == 0, errors);
        }
    }
}