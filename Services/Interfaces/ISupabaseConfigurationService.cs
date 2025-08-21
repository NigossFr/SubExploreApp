// ========================================
// SECURE SUPABASE CONFIGURATION SERVICE
// ========================================
// Interface for secure configuration management

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for secure Supabase configuration management
    /// Handles environment variables and credential security
    /// </summary>
    public interface ISupabaseConfigurationService
    {
        /// <summary>
        /// Gets the Supabase API URL from secure configuration
        /// </summary>
        Task<string> GetSupabaseUrlAsync();

        /// <summary>
        /// Gets the Supabase anonymous key from secure configuration
        /// </summary>
        Task<string> GetSupabaseAnonKeyAsync();

        /// <summary>
        /// Gets the Supabase service role key from secure configuration
        /// </summary>
        Task<string> GetSupabaseServiceRoleKeyAsync();

        /// <summary>
        /// Gets the complete database connection string from secure configuration
        /// </summary>
        Task<string> GetDatabaseConnectionStringAsync();

        /// <summary>
        /// Validates that all required Supabase configuration is present
        /// </summary>
        Task<bool> ValidateConfigurationAsync();

        /// <summary>
        /// Gets configuration status for diagnostic purposes
        /// Masks sensitive values for security
        /// </summary>
        Task<string> GetConfigurationStatusAsync();

        /// <summary>
        /// Refreshes configuration from environment variables
        /// </summary>
        Task RefreshConfigurationAsync();
    }
}