using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for exporting and importing favorite spots data
    /// </summary>
    public interface IFavoriteExportImportService
    {
        /// <summary>
        /// Export user favorites to CSV format
        /// </summary>
        Task<FavoriteExportResult> ExportToCsvAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Export user favorites to JSON format
        /// </summary>
        Task<FavoriteExportResult> ExportToJsonAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Export user favorites to GPX format (for GPS devices)
        /// </summary>
        Task<FavoriteExportResult> ExportToGpxAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Export user favorites as a shareable package
        /// </summary>
        Task<FavoriteExportResult> ExportAsPackageAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Import favorites from CSV format
        /// </summary>
        Task<FavoriteImportResult> ImportFromCsvAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Import favorites from JSON format
        /// </summary>
        Task<FavoriteImportResult> ImportFromJsonAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Import favorites from GPX format
        /// </summary>
        Task<FavoriteImportResult> ImportFromGpxAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Import favorites from a shareable package
        /// </summary>
        Task<FavoriteImportResult> ImportFromPackageAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preview import data without actually importing
        /// </summary>
        Task<FavoriteImportPreview> PreviewImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get supported export formats
        /// </summary>
        IEnumerable<ExportFormat> GetSupportedExportFormats();

        /// <summary>
        /// Get supported import formats
        /// </summary>
        IEnumerable<ImportFormat> GetSupportedImportFormats();

        /// <summary>
        /// Validate export file path and permissions
        /// </summary>
        Task<FileValidationResult> ValidateExportPathAsync(string filePath);

        /// <summary>
        /// Validate import file and format
        /// </summary>
        Task<FileValidationResult> ValidateImportFileAsync(string filePath);

        /// <summary>
        /// Get default export file name for user
        /// </summary>
        string GetDefaultExportFileName(Guid userId, ExportFormat format, DateTime? timestamp = null);

        /// <summary>
        /// Event fired when export progress changes
        /// </summary>
        event EventHandler<FavoriteExportProgressEventArgs> ExportProgressChanged;

        /// <summary>
        /// Event fired when import progress changes
        /// </summary>
        event EventHandler<FavoriteImportProgressEventArgs> ImportProgressChanged;
    }

    /// <summary>
    /// Export options for customizing the export process
    /// </summary>
    public class ExportOptions
    {
        public bool IncludeNotes { get; set; } = true;
        public bool IncludePersonalData { get; set; } = true;
        public bool IncludeStatistics { get; set; } = false;
        public bool CompressOutput { get; set; } = false;
        public bool IncludeSpotDetails { get; set; } = true;
        public bool IncludeCoordinates { get; set; } = true;
        public int? MaxRecords { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<int>? PriorityFilter { get; set; }
        public bool NotificationsOnly { get; set; } = false;
        public string? CustomMetadata { get; set; }

        public ExportOptions()
        {
            PriorityFilter = new List<int>();
        }
    }

    /// <summary>
    /// Import options for customizing the import process
    /// </summary>
    public class ImportOptions
    {
        public bool AllowDuplicates { get; set; } = false;
        public bool UpdateExisting { get; set; } = true;
        public bool PreservePriorities { get; set; } = true;
        public bool PreserveNotes { get; set; } = true;
        public bool PreserveNotificationSettings { get; set; } = true;
        public bool ValidateCoordinates { get; set; } = true;
        public bool SkipInvalidRecords { get; set; } = true;
        public int? MaxRecordsToImport { get; set; }
        public bool CreateMissingSpots { get; set; } = false;
        public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.UpdateExisting;

        /// <summary>
        /// Mapping of import fields to system fields
        /// </summary>
        public Dictionary<string, string> FieldMapping { get; set; } = new();
    }

    /// <summary>
    /// Conflict resolution strategies for import
    /// </summary>
    public enum ConflictResolution
    {
        Skip,
        UpdateExisting,
        CreateDuplicate,
        PromptUser,
        PreferImported,
        PreferExisting
    }

    /// <summary>
    /// Result of export operation
    /// </summary>
    public class FavoriteExportResult
    {
        public bool IsSuccess { get; set; }
        public int TotalRecords { get; set; }
        public int ExportedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public TimeSpan ExportDuration { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ExportTimestamp { get; set; } = DateTime.UtcNow;
        public ExportFormat Format { get; set; }
        public string FormattedFileSize => FormatFileSize(FileSizeBytes);

        private static string FormatFileSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            if (bytes >= MB) return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB) return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} bytes";
        }
    }

    /// <summary>
    /// Result of import operation
    /// </summary>
    public class FavoriteImportResult
    {
        public bool IsSuccess { get; set; }
        public int TotalRecords { get; set; }
        public int ImportedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public int DuplicateRecords { get; set; }
        public int ErrorRecords { get; set; }
        public TimeSpan ImportDuration { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<ImportConflict> Conflicts { get; set; } = new();
        public DateTime ImportTimestamp { get; set; } = DateTime.UtcNow;
        public ImportFormat Format { get; set; }

        public double SuccessRate => TotalRecords > 0 ? (double)ImportedRecords / TotalRecords * 100 : 0;
    }

    /// <summary>
    /// Preview of import data
    /// </summary>
    public class FavoriteImportPreview
    {
        public bool IsValid { get; set; }
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public ImportFormat DetectedFormat { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<FavoritePreviewRecord> SampleRecords { get; set; } = new();
        public Dictionary<string, string> DetectedFields { get; set; } = new();

        /// <summary>
        /// First few records for user preview
        /// </summary>
        public List<FavoritePreviewRecord> GetPreviewRecords(int maxCount = 5)
        {
            return SampleRecords.Take(maxCount).ToList();
        }
    }

    /// <summary>
    /// Preview record for import
    /// </summary>
    public class FavoritePreviewRecord
    {
        public string SpotName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Priority { get; set; } = 5;
        public string? Notes { get; set; }
        public bool NotificationEnabled { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public bool IsValid { get; set; } = true;
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Import conflict information
    /// </summary>
    public class ImportConflict
    {
        public Guid SpotId { get; set; }
        public string SpotName { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string ConflictDescription { get; set; } = string.Empty;
        public FavoritePreviewRecord ImportedData { get; set; } = new();
        public UserFavoriteSpot? ExistingData { get; set; }
        public ConflictResolution SuggestedResolution { get; set; }
        public ConflictResolution? UserResolution { get; set; }
    }

    /// <summary>
    /// Export format information
    /// </summary>
    public class ExportFormat
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public bool SupportsCompression { get; set; }
        public bool SupportsMetadata { get; set; }
        public List<string> SupportedFields { get; set; } = new();
    }

    /// <summary>
    /// Import format information
    /// </summary>
    public class ImportFormat
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> FileExtensions { get; set; } = new();
        public List<string> MimeTypes { get; set; } = new();
        public bool SupportsAutoDetection { get; set; }
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
    }

    /// <summary>
    /// File validation result for export/import operations
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Export progress event arguments
    /// </summary>
    public class FavoriteExportProgressEventArgs : EventArgs
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int CurrentRecord { get; set; }
        public string CurrentSpotName { get; set; } = string.Empty;
        public double ProgressPercentage => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;
        public ExportFormat Format { get; set; } = new();
        public bool CanCancel { get; set; } = true;
    }

    /// <summary>
    /// Import progress event arguments
    /// </summary>
    public class FavoriteImportProgressEventArgs : EventArgs
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int ImportedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public string CurrentSpotName { get; set; } = string.Empty;
        public double ProgressPercentage => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;
        public ImportFormat Format { get; set; } = new();
        public bool CanCancel { get; set; } = true;
    }
}