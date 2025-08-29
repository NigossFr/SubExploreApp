using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface ISpotReportService
    {
        /// <summary>
        /// Submit a new spot report
        /// </summary>
        /// <param name="spotId">ID of the spot being reported</param>
        /// <param name="reportType">Type of report</param>
        /// <param name="description">Description of the issue</param>
        /// <param name="contactEmail">Optional contact email</param>
        /// <param name="severity">Report severity</param>
        /// <returns>Report ID if successful</returns>
        Task<Guid?> SubmitReportAsync(Guid spotId, SpotReportType reportType, 
            string description, string? contactEmail = null, SpotReportSeverity severity = SpotReportSeverity.Low);

        /// <summary>
        /// Get all reports for a specific spot (admin only)
        /// </summary>
        /// <param name="spotId">Spot ID</param>
        /// <returns>List of reports</returns>
        Task<List<SpotReport>> GetReportsForSpotAsync(Guid spotId);

        /// <summary>
        /// Get user's submitted reports
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user reports</returns>
        Task<List<SpotReport>> GetUserReportsAsync(Guid userId);

        /// <summary>
        /// Get pending reports for moderation
        /// </summary>
        /// <returns>List of pending reports</returns>
        Task<List<SpotReport>> GetPendingReportsAsync();

        /// <summary>
        /// Update report status (moderator only)
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <param name="newStatus">New status</param>
        /// <param name="reviewNotes">Review notes</param>
        /// <param name="reviewerId">ID of the reviewer</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateReportStatusAsync(Guid reportId, SpotReportStatus newStatus, 
            string reviewNotes, Guid reviewerId);

        /// <summary>
        /// Check if user has already reported this spot
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="spotId">Spot ID</param>
        /// <returns>True if user has already reported this spot</returns>
        Task<bool> HasUserReportedSpotAsync(Guid userId, Guid spotId);

        /// <summary>
        /// Get available report types
        /// </summary>
        /// <returns>Dictionary of report types with descriptions</returns>
        Dictionary<SpotReportType, string> GetReportTypes();

        /// <summary>
        /// Get report severity levels
        /// </summary>
        /// <returns>Dictionary of severity levels with descriptions</returns>
        Dictionary<SpotReportSeverity, string> GetSeverityLevels();
    }
}