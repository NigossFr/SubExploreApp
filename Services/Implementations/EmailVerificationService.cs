using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Email verification service implementation with secure token management
    /// </summary>
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IEmailVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerificationService> _logger;
        private const int TOKEN_LENGTH = 32;
        private const int MAX_DAILY_TOKENS = 3;
        private const int TOKEN_EXPIRATION_HOURS = 24;

        public EmailVerificationService(
            IEmailVerificationTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            ILogger<EmailVerificationService> logger)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<EmailVerificationResult> SendVerificationEmailAsync(User user, string? ipAddress = null)
        {
            try
            {
                if (user == null)
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "User not found",
                        ResultType = EmailVerificationResultType.UserNotFound
                    };
                }

                // Check if already verified
                if (user.IsEmailConfirmed)
                {
                    _logger.LogInformation("Email already verified for user {UserId}", user.Id);
                    return new EmailVerificationResult
                    {
                        Success = true,
                        ResultType = EmailVerificationResultType.AlreadyVerified
                    };
                }

                // Check daily limit
                if (await _tokenRepository.HasReachedDailyLimitAsync(user.Id, MAX_DAILY_TOKENS))
                {
                    _logger.LogWarning("Daily verification token limit reached for user {UserId}", user.Id);
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = $"Maximum {MAX_DAILY_TOKENS} verification emails per day allowed",
                        ResultType = EmailVerificationResultType.DailyLimitReached
                    };
                }

                // Invalidate any existing tokens for this user
                await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id);

                // Generate new secure token
                var token = GenerateSecureToken();
                var tokenHash = ComputeTokenHash(token);

                // Create token record
                var verificationToken = new EmailVerificationToken
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    Email = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                    CreatedFromIP = ipAddress,
                    CreatedAt = DateTime.UtcNow,
                    MaxAttempts = 5
                };

                await _tokenRepository.AddAsync(verificationToken);
                await _tokenRepository.SaveChangesAsync();

                // Send email
                var emailSent = await _emailService.SendEmailVerificationAsync(user, token);

                if (emailSent)
                {
                    _logger.LogInformation("Email verification sent successfully to user {UserId} at {Email}", 
                        user.Id, user.Email);

                    return new EmailVerificationResult
                    {
                        Success = true,
                        ResultType = EmailVerificationResultType.EmailSent,
                        EmailSentAt = DateTime.UtcNow,
                        TokenExpiresIn = TimeSpan.FromHours(TOKEN_EXPIRATION_HOURS)
                    };
                }
                else
                {
                    _logger.LogError("Failed to send verification email to user {UserId} at {Email}", 
                        user.Id, user.Email);

                    // Mark token as used since email failed
                    verificationToken.IsUsed = true;
                    await _tokenRepository.UpdateAsync(verificationToken);
                    await _tokenRepository.SaveChangesAsync();

                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to send verification email",
                        ResultType = EmailVerificationResultType.SendingFailed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to user {UserId}", user?.Id);
                return new EmailVerificationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while sending verification email",
                    ResultType = EmailVerificationResultType.SendingFailed
                };
            }
        }

        public async Task<EmailVerificationResult> VerifyEmailAsync(string token, string email, string? ipAddress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Token and email are required",
                        ResultType = EmailVerificationResultType.TokenInvalid
                    };
                }

                var tokenHash = ComputeTokenHash(token);
                var verificationToken = await _tokenRepository.GetValidTokenByHashAndEmailAsync(tokenHash, email);

                if (verificationToken == null)
                {
                    _logger.LogWarning("Invalid verification token attempted for email {Email}", email);
                    
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
                            return new EmailVerificationResult
                            {
                                Success = false,
                                ErrorMessage = "Verification token has expired",
                                ResultType = EmailVerificationResultType.TokenExpired
                            };
                        }

                        if (existingToken.AttemptCount >= existingToken.MaxAttempts)
                        {
                            return new EmailVerificationResult
                            {
                                Success = false,
                                ErrorMessage = "Too many verification attempts",
                                ResultType = EmailVerificationResultType.TooManyAttempts
                            };
                        }
                    }

                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid verification token",
                        ResultType = EmailVerificationResultType.TokenInvalid
                    };
                }

                // Check if user's email matches
                if (verificationToken.User.Email.ToLowerInvariant() != email.ToLowerInvariant())
                {
                    _logger.LogWarning("Email mismatch during verification: token email {TokenEmail}, provided email {ProvidedEmail}", 
                        verificationToken.Email, email);
                    
                    verificationToken.AttemptCount++;
                    await _tokenRepository.UpdateAsync(verificationToken);
                    await _tokenRepository.SaveChangesAsync();

                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Email address mismatch",
                        ResultType = EmailVerificationResultType.EmailMismatch
                    };
                }

                // Mark token as used
                verificationToken.IsUsed = true;
                verificationToken.UsedAt = DateTime.UtcNow;
                verificationToken.UsedFromIP = ipAddress;
                await _tokenRepository.UpdateAsync(verificationToken);

                // Mark user as verified
                var user = verificationToken.User;
                user.IsEmailConfirmed = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Invalidate all other tokens for this user
                await _tokenRepository.InvalidateAllTokensForUserAsync(user.Id);

                await _tokenRepository.SaveChangesAsync();

                _logger.LogInformation("Email verified successfully for user {UserId} at {Email}", user.Id, email);

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send welcome email to user {UserId}", user.Id);
                    // Don't fail verification if welcome email fails
                }

                return new EmailVerificationResult
                {
                    Success = true,
                    ResultType = EmailVerificationResultType.EmailVerified,
                    EmailVerifiedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", email);
                return new EmailVerificationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during email verification",
                    ResultType = EmailVerificationResultType.TokenInvalid
                };
            }
        }

        public async Task<EmailVerificationResult> ResendVerificationEmailAsync(string email, string? ipAddress = null)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal if email exists
                    _logger.LogWarning("Verification resend requested for non-existent email: {Email}", email);
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "If this email is registered, a verification email will be sent",
                        ResultType = EmailVerificationResultType.UserNotFound
                    };
                }

                return await SendVerificationEmailAsync(user, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email for {Email}", email);
                return new EmailVerificationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while resending verification email",
                    ResultType = EmailVerificationResultType.SendingFailed
                };
            }
        }

        public async Task<bool> IsEmailVerifiedAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                return user?.IsEmailConfirmed ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status for {Email}", email);
                return false;
            }
        }

        public async Task<EmailVerificationStatus> GetVerificationStatusAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new EmailVerificationStatus
                    {
                        IsVerified = false,
                        CanRequestNewToken = false
                    };
                }

                var validTokens = await _tokenRepository.GetValidTokensForUserAsync(userId);
                var hasReachedLimit = await _tokenRepository.HasReachedDailyLimitAsync(userId, MAX_DAILY_TOKENS);

                var status = new EmailVerificationStatus
                {
                    IsVerified = user.IsEmailConfirmed,
                    VerifiedAt = user.IsEmailConfirmed ? user.UpdatedAt : null,
                    PendingTokens = validTokens.Count,
                    LastTokenSent = validTokens.FirstOrDefault()?.CreatedAt,
                    CanRequestNewToken = !user.IsEmailConfirmed && !hasReachedLimit,
                    MaxDailyTokens = MAX_DAILY_TOKENS
                };

                // Calculate when next token will be available
                if (hasReachedLimit)
                {
                    var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                    status.NextTokenAvailableIn = tomorrow - DateTime.UtcNow;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status for user {UserId}", userId);
                return new EmailVerificationStatus
                {
                    IsVerified = false,
                    CanRequestNewToken = false
                };
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
                _logger.LogError(ex, "Error cleaning up expired verification tokens");
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
    }
}