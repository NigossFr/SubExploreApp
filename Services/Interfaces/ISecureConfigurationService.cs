using SubExplore.Models.Configuration;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Secure configuration service for managing sensitive application settings
    /// </summary>
    public interface ISecureConfigurationService
    {
        /// <summary>
        /// Get JWT configuration with secure key management
        /// </summary>
        /// <returns>JWT configuration</returns>
        Task<JwtConfiguration> GetJwtConfigurationAsync();

        /// <summary>
        /// Get database connection string for current platform
        /// </summary>
        /// <returns>Secure connection string</returns>
        Task<string> GetDatabaseConnectionStringAsync();

        /// <summary>
        /// Get API configuration settings
        /// </summary>
        /// <returns>API configuration</returns>
        Task<T> GetConfigurationAsync<T>(string sectionName) where T : class, new();

        /// <summary>
        /// Initialize secure configuration on application startup
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Validate all security configurations
        /// </summary>
        /// <returns>Validation result with any errors</returns>
        Task<(bool IsValid, List<string> Errors)> ValidateConfigurationAsync();

        /// <summary>
        /// Generate and store a new JWT secret key securely
        /// </summary>
        Task<string> GenerateAndStoreJwtSecretAsync();

        /// <summary>
        /// Rotate JWT secret key (for security maintenance)
        /// </summary>
        Task<bool> RotateJwtSecretAsync();
    }
}