// ========================================
// LOCALIZATION SERVICE INTERFACE
// ========================================
// Interface for managing application localization and resource strings

using System.Globalization;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing application localization and culture-specific resources
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Get localized string by key from the specified resource set
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="resourceSet">Resource set name (optional, defaults to common resources)</param>
        /// <returns>Localized string or key if not found</returns>
        string GetString(string key, string? resourceSet = null);

        /// <summary>
        /// Get localized string with format parameters
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>Formatted localized string</returns>
        string GetString(string key, params object[] args);

        /// <summary>
        /// Get current culture information
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Set application culture
        /// </summary>
        /// <param name="cultureCode">Culture code (e.g., "fr-FR", "en-US")</param>
        Task SetCultureAsync(string cultureCode);

        /// <summary>
        /// Get list of supported cultures
        /// </summary>
        IEnumerable<CultureInfo> GetSupportedCultures();

        /// <summary>
        /// Check if culture is supported
        /// </summary>
        /// <param name="cultureCode">Culture code to check</param>
        bool IsCultureSupported(string cultureCode);
    }
}