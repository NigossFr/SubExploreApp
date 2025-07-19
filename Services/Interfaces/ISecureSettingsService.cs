namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Secure settings service for storing sensitive authentication data
    /// Extends ISettingsService with encryption capabilities
    /// </summary>
    public interface ISecureSettingsService : ISettingsService
    {
        /// <summary>
        /// Store encrypted value in secure storage
        /// </summary>
        /// <typeparam name="T">Type of value to store</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="value">Value to encrypt and store</param>
        Task SetSecureAsync<T>(string key, T value);

        /// <summary>
        /// Retrieve and decrypt value from secure storage
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Decrypted value or default</returns>
        Task<T> GetSecureAsync<T>(string key, T defaultValue = default);

        /// <summary>
        /// Check if secure key exists
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <returns>True if key exists in secure storage</returns>
        Task<bool> ContainsSecureAsync(string key);

        /// <summary>
        /// Remove value from secure storage
        /// </summary>
        /// <param name="key">Storage key to remove</param>
        Task RemoveSecureAsync(string key);

        /// <summary>
        /// Clear all secure storage data
        /// </summary>
        Task ClearSecureAsync();

        /// <summary>
        /// Store access token securely
        /// </summary>
        /// <param name="token">JWT access token</param>
        Task SetAccessTokenAsync(string token);

        /// <summary>
        /// Retrieve access token from secure storage
        /// </summary>
        /// <returns>Access token or null if not found</returns>
        Task<string?> GetAccessTokenAsync();

        /// <summary>
        /// Store refresh token securely
        /// </summary>
        /// <param name="token">Refresh token</param>
        Task SetRefreshTokenAsync(string token);

        /// <summary>
        /// Retrieve refresh token from secure storage
        /// </summary>
        /// <returns>Refresh token or null if not found</returns>
        Task<string?> GetRefreshTokenAsync();

        /// <summary>
        /// Clear all authentication tokens
        /// </summary>
        Task ClearAuthenticationTokensAsync();
    }
}