using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Comprehensive authentication service with secure user management
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly ISecureSettingsService _secureSettings;
        private readonly ILogger<AuthenticationService> _logger;
        
        private User? _currentUser;
        private string? _currentAccessToken;

        public User? CurrentUser => _currentUser;
        public int? CurrentUserId => _currentUser?.Id;
        public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_currentAccessToken);

        public event EventHandler<AuthenticationStateChangedEventArgs> StateChanged;

        public AuthenticationService(
            IUserRepository userRepository,
            ITokenService tokenService,
            ISecureSettingsService secureSettings,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _secureSettings = secureSettings;
            _logger = logger;
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Attempting login for email: {Email}", email);

                // Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Email and password are required"
                    };
                }

                // Find user by email
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent email: {Email}", email);
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid email or password"
                    };
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password attempt for user: {UserId}", user.Id);
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid email or password"
                    };
                }

                // Generate tokens
                var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Update user last login
                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                // Store tokens securely
                await _secureSettings.SetAccessTokenAsync(accessToken);
                await _secureSettings.SetRefreshTokenAsync(refreshToken);

                // Update current user state
                _currentUser = user;
                _currentAccessToken = accessToken;

                // Set current user ID in settings for compatibility
                _secureSettings.Set("CurrentUserId", user.Id);

                _logger.LogInformation("Successful login for user: {UserId}", user.Id);

                // Notify state change
                OnStateChanged(true, user, "Login successful");

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    User = user,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Default 1 hour
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during login. Please try again."
                };
            }
        }

        public async Task<AuthenticationResult> RegisterAsync(UserRegistrationRequest registerRequest)
        {
            try
            {
                _logger.LogInformation("Attempting registration for email: {Email}", registerRequest.Email);

                // Validate request
                var validationResult = ValidateRegistrationRequest(registerRequest);
                if (!validationResult.IsValid)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Validation failed",
                        ValidationErrors = validationResult.Errors
                    };
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(registerRequest.Email);
                if (existingUser != null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "An account with this email address already exists"
                    };
                }

                // Check if username is taken
                var existingUsername = await _userRepository.GetUserByUsernameAsync(registerRequest.Username);
                if (existingUsername != null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "This username is already taken"
                    };
                }

                // Create new user
                var newUser = new User
                {
                    Email = registerRequest.Email.ToLowerInvariant(),
                    Username = registerRequest.Username,
                    FirstName = registerRequest.FirstName,
                    LastName = registerRequest.LastName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password),
                    CreatedAt = DateTime.UtcNow,
                    AccountType = Models.Enums.AccountType.Standard,
                    SubscriptionStatus = Models.Enums.SubscriptionStatus.Free
                };

                await _userRepository.AddAsync(newUser);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {UserId}", newUser.Id);

                // Auto-login after registration
                return await LoginAsync(registerRequest.Email, registerRequest.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", registerRequest.Email);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during registration. Please try again."
                };
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _secureSettings.GetRefreshTokenAsync();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("No refresh token available");
                    return false;
                }

                // Check if refresh token is revoked
                if (await _tokenService.IsRefreshTokenRevokedAsync(refreshToken))
                {
                    _logger.LogWarning("Refresh token is revoked");
                    await LogoutAsync();
                    return false;
                }

                // Get current user from token or database
                if (_currentUser == null)
                {
                    var currentUserId = _secureSettings.Get<int?>("CurrentUserId");
                    if (currentUserId.HasValue)
                    {
                        _currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
                    }
                }

                if (_currentUser == null)
                {
                    _logger.LogWarning("Cannot refresh token: no current user");
                    return false;
                }

                // Generate new tokens
                var newAccessToken = _tokenService.GenerateAccessToken(_currentUser.Id, _currentUser.Email);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Revoke old refresh token
                await _tokenService.RevokeRefreshTokenAsync(refreshToken);

                // Store new tokens
                await _secureSettings.SetAccessTokenAsync(newAccessToken);
                await _secureSettings.SetRefreshTokenAsync(newRefreshToken);

                _currentAccessToken = newAccessToken;

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", _currentUser.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("Logging out user: {UserId}", _currentUser?.Id);

                // Revoke refresh token
                var refreshToken = await _secureSettings.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _tokenService.RevokeRefreshTokenAsync(refreshToken);
                }

                // Clear stored tokens and user data
                await _secureSettings.ClearAuthenticationTokensAsync();
                _secureSettings.Remove("CurrentUserId");

                // Clear current state
                var previousUser = _currentUser;
                _currentUser = null;
                _currentAccessToken = null;

                // Notify state change
                OnStateChanged(false, null, "Logout successful");

                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing authentication service");

                // Try to restore session from stored tokens
                var accessToken = await _secureSettings.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogDebug("No stored access token found");
                    return;
                }

                // Validate stored token
                if (_tokenService.IsTokenExpired(accessToken))
                {
                    _logger.LogDebug("Stored access token is expired, attempting refresh");
                    if (!await RefreshTokenAsync())
                    {
                        _logger.LogWarning("Failed to refresh token, clearing session");
                        await LogoutAsync();
                        return;
                    }
                    accessToken = await _secureSettings.GetAccessTokenAsync();
                }

                // Extract user ID from token
                var userId = _tokenService.GetUserIdFromToken(accessToken);
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Invalid token: no user ID found");
                    await LogoutAsync();
                    return;
                }

                // Load user from database
                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId.Value);
                    await LogoutAsync();
                    return;
                }

                // Restore session
                _currentUser = user;
                _currentAccessToken = accessToken;
                _secureSettings.Set("CurrentUserId", user.Id);

                _logger.LogInformation("Authentication session restored for user: {UserId}", user.Id);
                OnStateChanged(true, user, "Session restored");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing authentication service");
                await LogoutAsync();
            }
        }

        public async Task<bool> ValidateAuthenticationAsync()
        {
            try
            {
                if (!IsAuthenticated)
                    return false;

                var accessToken = await _secureSettings.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                    return false;

                // Check if token is expired
                if (_tokenService.IsTokenExpired(accessToken))
                {
                    // Try to refresh
                    return await RefreshTokenAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating authentication");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (!IsAuthenticated || _currentUser == null)
                {
                    _logger.LogWarning("Change password attempt without authentication");
                    return false;
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, _currentUser.PasswordHash))
                {
                    _logger.LogWarning("Invalid current password for user: {UserId}", _currentUser.Id);
                    return false;
                }

                // Validate new password
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                {
                    _logger.LogWarning("Invalid new password for user: {UserId}", _currentUser.Id);
                    return false;
                }

                // Update password
                _currentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _currentUser.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(_currentUser);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user: {UserId}", _currentUser.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", _currentUser?.Id);
                return false;
            }
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                _logger.LogInformation("Password reset requested for email: {Email}", email);

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal if email exists for security
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    return true; // Return true to not reveal if email exists
                }

                // TODO: Implement email service and send reset email
                // For now, just log the request
                _logger.LogInformation("Password reset email would be sent to: {Email}", email);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for email: {Email}", email);
                return false;
            }
        }

        private (bool IsValid, List<string> Errors) ValidateRegistrationRequest(UserRegistrationRequest request)
        {
            var errors = new List<string>();
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(request, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Validation error"));
            }

            if (!request.AcceptTermsAndConditions)
            {
                errors.Add("You must accept the terms and conditions");
            }

            return (errors.Count == 0, errors);
        }

        private void OnStateChanged(bool isAuthenticated, User? user, string reason)
        {
            try
            {
                StateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
                {
                    IsAuthenticated = isAuthenticated,
                    User = user,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying authentication state change");
            }
        }
    }
}