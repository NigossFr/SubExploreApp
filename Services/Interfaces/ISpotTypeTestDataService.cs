namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing spot type test data initialization
    /// </summary>
    public interface ISpotTypeTestDataService
    {
        /// <summary>
        /// Ensures basic spot types exist in the database
        /// Creates default spot types if none are found
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task EnsureBasicSpotTypesAsync();
    }
}