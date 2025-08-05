using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SubExplore.ViewModels.Auth
{
    /// <summary>
    /// ViewModel for testing email service functionality during development
    /// </summary>
    public partial class EmailTestViewModel : ObservableValidator
    {
        private readonly IEmailService _emailService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly ILogger<EmailTestViewModel> _logger;

        [ObservableProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        private string _testEmail = string.Empty;

        [ObservableProperty]
        private bool _isTestInProgress = false;

        [ObservableProperty]
        private string _testResults = string.Empty;

        [ObservableProperty]
        private bool _hasTestResults = false;

        public string Title { get; set; } = "Test Service Email";

        public EmailTestViewModel(
            IEmailService emailService,
            IEmailVerificationService emailVerificationService,
            IPasswordResetService passwordResetService,
            ILogger<EmailTestViewModel> logger)
        {
            _emailService = emailService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
            _logger = logger;
        }

        [RelayCommand]
        private async Task SendTestEmail()
        {
            if (IsTestInProgress || !ValidateEmail()) return;

            try
            {
                IsTestInProgress = true;
                ClearResults();

                _logger.LogInformation("Sending test email to {Email}", TestEmail);
                
                var success = await _emailService.SendTestEmailAsync(TestEmail);
                
                if (success)
                {
                    AddResult("✅ Test email sent successfully!");
                    AddResult($"📧 Sent to: {TestEmail}");
                    AddResult($"⏰ Time: {DateTime.Now:HH:mm:ss}");
                    _logger.LogInformation("Test email sent successfully to {Email}", TestEmail);
                }
                else
                {
                    AddResult("❌ Failed to send test email");
                    AddResult("Check email configuration and logs for details");
                    _logger.LogError("Failed to send test email to {Email}", TestEmail);
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ Error: {ex.Message}");
                _logger.LogError(ex, "Error sending test email to {Email}", TestEmail);
            }
            finally
            {
                IsTestInProgress = false;
                HasTestResults = true;
            }
        }

        [RelayCommand]
        private async Task TestEmailValidation()
        {
            if (IsTestInProgress || !ValidateEmail()) return;

            try
            {
                IsTestInProgress = true;
                ClearResults();

                _logger.LogInformation("Testing email validation for {Email}", TestEmail);
                
                var result = await _emailService.ValidateEmailAsync(TestEmail);
                
                AddResult("📝 Email Validation Results:");
                AddResult($"✅ Valid: {result.IsValid}");
                AddResult($"📤 Deliverable: {result.IsDeliverable}");
                AddResult($"⚠️ Risk Level: {result.RiskLevel}");
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    AddResult($"❌ Error: {result.ErrorMessage}");
                }
                
                if (result.ValidationIssues.Any())
                {
                    AddResult("🚨 Issues:");
                    foreach (var issue in result.ValidationIssues)
                    {
                        AddResult($"  • {issue}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ Error: {ex.Message}");
                _logger.LogError(ex, "Error validating email {Email}", TestEmail);
            }
            finally
            {
                IsTestInProgress = false;
                HasTestResults = true;
            }
        }

        [RelayCommand]
        private async Task TestVerificationService()
        {
            if (IsTestInProgress || !ValidateEmail()) return;

            try
            {
                IsTestInProgress = true;
                ClearResults();

                AddResult("🔍 Testing Email Verification Service...");
                
                // Check if email is already verified
                var isVerified = await _emailVerificationService.IsEmailVerifiedAsync(TestEmail);
                AddResult($"📧 Email verified status: {(isVerified ? "✅ Verified" : "❌ Not verified")}");
                
                // Test cleanup function
                var cleanedUp = await _emailVerificationService.CleanupExpiredTokensAsync();
                AddResult($"🧹 Expired tokens cleaned up: {cleanedUp}");
                
                AddResult("✅ Verification service test completed");
            }
            catch (Exception ex)
            {
                AddResult($"❌ Error: {ex.Message}");
                _logger.LogError(ex, "Error testing verification service for {Email}", TestEmail);
            }
            finally
            {
                IsTestInProgress = false;
                HasTestResults = true;
            }
        }

        [RelayCommand]
        private async Task TestPasswordResetService()
        {
            if (IsTestInProgress || !ValidateEmail()) return;

            try
            {
                IsTestInProgress = true;
                ClearResults();

                AddResult("🔒 Testing Password Reset Service...");
                
                // Check daily limit
                var hasReachedLimit = await _passwordResetService.HasReachedDailyLimitAsync(TestEmail);
                AddResult($"📊 Daily limit reached: {(hasReachedLimit ? "⚠️ Yes" : "✅ No")}");
                
                // Get statistics
                var stats = await _passwordResetService.GetResetStatisticsAsync();
                AddResult($"📈 Reset Statistics (last 30 days):");
                AddResult($"  • Total requests: {stats.TotalResetRequests}");
                AddResult($"  • Successful resets: {stats.SuccessfulResets}");
                AddResult($"  • Success rate: {stats.SuccessRate:F1}%");
                
                // Test cleanup
                var cleanedUp = await _passwordResetService.CleanupExpiredTokensAsync();
                AddResult($"🧹 Expired tokens cleaned up: {cleanedUp}");
                
                AddResult("✅ Password reset service test completed");
            }
            catch (Exception ex)
            {
                AddResult($"❌ Error: {ex.Message}");
                _logger.LogError(ex, "Error testing password reset service for {Email}", TestEmail);
            }
            finally
            {
                IsTestInProgress = false;
                HasTestResults = true;
            }
        }

        [RelayCommand]
        private async Task RunFullTest()
        {
            if (IsTestInProgress || !ValidateEmail()) return;

            try
            {
                IsTestInProgress = true;
                ClearResults();

                AddResult("🚀 Starting Full Email Service Test Suite...");
                AddResult("");
                
                // Test 1: Email validation
                AddResult("1️⃣ Testing email validation...");
                var validationResult = await _emailService.ValidateEmailAsync(TestEmail);
                AddResult($"   ✅ Valid: {validationResult.IsValid}, Risk: {validationResult.RiskLevel}");
                
                // Test 2: Service availability
                AddResult("2️⃣ Testing service availability...");
                var verificationAvailable = _emailVerificationService != null;
                var resetAvailable = _passwordResetService != null;
                AddResult($"   📧 Verification service: {(verificationAvailable ? "✅ Available" : "❌ Unavailable")}");
                AddResult($"   🔒 Reset service: {(resetAvailable ? "✅ Available" : "❌ Unavailable")}");
                
                // Test 3: Configuration test
                AddResult("3️⃣ Testing email configuration...");
                var testEmailSent = await _emailService.SendTestEmailAsync(TestEmail);
                AddResult($"   📤 Test email: {(testEmailSent ? "✅ Sent successfully" : "❌ Failed to send")}");
                
                // Test 4: Statistics
                AddResult("4️⃣ Getting service statistics...");
                if (resetAvailable)
                {
                    var stats = await _passwordResetService.GetResetStatisticsAsync();
                    AddResult($"   📊 Reset success rate: {stats.SuccessRate:F1}%");
                }
                
                AddResult("");
                AddResult($"🎉 Full test completed at {DateTime.Now:HH:mm:ss}");
                
                if (testEmailSent)
                {
                    AddResult("📬 Check your email inbox for the test message!");
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ Full test error: {ex.Message}");
                _logger.LogError(ex, "Error running full email test suite for {Email}", TestEmail);
            }
            finally
            {
                IsTestInProgress = false;
                HasTestResults = true;
            }
        }

        [RelayCommand]
        private void ClearResults()
        {
            TestResults = string.Empty;
            HasTestResults = false;
        }

        private void AddResult(string message)
        {
            if (string.IsNullOrEmpty(TestResults))
            {
                TestResults = message;
            }
            else
            {
                TestResults += Environment.NewLine + message;
            }
        }

        private bool ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(TestEmail))
            {
                AddResult("❌ Please enter an email address");
                HasTestResults = true;
                return false;
            }

            if (!new EmailAddressAttribute().IsValid(TestEmail))
            {
                AddResult("❌ Please enter a valid email address");
                HasTestResults = true;
                return false;
            }

            return true;
        }

        public async Task InitializeAsync(object parameter = null)
        {
            // Initialize with default test email
            if (string.IsNullOrEmpty(TestEmail))
            {
                TestEmail = "test@example.com";
            }
        }
    }
}