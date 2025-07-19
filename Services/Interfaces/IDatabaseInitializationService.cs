namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for initializing and migrating the database
    /// </summary>
    public interface IDatabaseInitializationService
    {
        /// <summary>
        /// Initialize the database and apply any pending migrations
        /// </summary>
        Task InitializeDatabaseAsync();

        /// <summary>
        /// Check if the database is properly initialized
        /// </summary>
        Task<bool> IsDatabaseInitializedAsync();

        /// <summary>
        /// Apply pending migrations manually
        /// </summary>
        Task ApplyMigrationsAsync();

        /// <summary>
        /// Create the RevokedTokens table if it doesn't exist
        /// </summary>
        Task EnsureRevokedTokensTableAsync();
    }
}