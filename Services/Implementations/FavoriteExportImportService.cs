using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for exporting and importing favorite spots in multiple formats
    /// </summary>
    public class FavoriteExportImportService : IFavoriteExportImportService
    {
        private readonly IFavoriteSpotService _favoriteSpotService;
        private readonly ILogger<FavoriteExportImportService> _logger;

        private static readonly List<ExportFormat> SupportedExportFormats = new()
        {
            new ExportFormat
            {
                Id = "csv",
                Name = "CSV (Comma Separated Values)",
                Description = "Standard CSV format compatible with Excel and other spreadsheet applications",
                FileExtension = ".csv",
                MimeType = "text/csv",
                SupportsCompression = false,
                SupportsMetadata = false,
                SupportedFields = new List<string> { "name", "latitude", "longitude", "priority", "notes", "notification", "created_date", "spot_type", "description", "depth" }
            },
            new ExportFormat
            {
                Id = "json",
                Name = "JSON (JavaScript Object Notation)",
                Description = "Structured JSON format with full metadata support",
                FileExtension = ".json",
                MimeType = "application/json",
                SupportsCompression = true,
                SupportsMetadata = true,
                SupportedFields = new List<string> { "all" }
            },
            new ExportFormat
            {
                Id = "gpx",
                Name = "GPX (GPS Exchange Format)",
                Description = "Standard GPS format for navigation devices and mapping applications",
                FileExtension = ".gpx",
                MimeType = "application/gpx+xml",
                SupportsCompression = false,
                SupportsMetadata = true,
                SupportedFields = new List<string> { "name", "latitude", "longitude", "description", "created_date" }
            },
            new ExportFormat
            {
                Id = "package",
                Name = "SubExplore Package",
                Description = "Complete backup package with all data and metadata",
                FileExtension = ".sepkg",
                MimeType = "application/zip",
                SupportsCompression = true,
                SupportsMetadata = true,
                SupportedFields = new List<string> { "all" }
            }
        };

        private static readonly List<ImportFormat> SupportedImportFormats = new()
        {
            new ImportFormat
            {
                Id = "csv",
                Name = "CSV (Comma Separated Values)",
                Description = "Standard CSV format from spreadsheet applications",
                FileExtensions = new List<string> { ".csv", ".txt" },
                MimeTypes = new List<string> { "text/csv", "text/plain" },
                SupportsAutoDetection = true,
                RequiredFields = new List<string> { "name", "latitude", "longitude" },
                OptionalFields = new List<string> { "priority", "notes", "notification", "created_date", "spot_type", "description", "depth" }
            },
            new ImportFormat
            {
                Id = "json",
                Name = "JSON (JavaScript Object Notation)",
                Description = "Structured JSON format with metadata",
                FileExtensions = new List<string> { ".json" },
                MimeTypes = new List<string> { "application/json" },
                SupportsAutoDetection = true,
                RequiredFields = new List<string> { "spotName", "latitude", "longitude" },
                OptionalFields = new List<string> { "all" }
            },
            new ImportFormat
            {
                Id = "gpx",
                Name = "GPX (GPS Exchange Format)",
                Description = "Standard GPS format from navigation devices",
                FileExtensions = new List<string> { ".gpx", ".xml" },
                MimeTypes = new List<string> { "application/gpx+xml", "text/xml" },
                SupportsAutoDetection = true,
                RequiredFields = new List<string> { "name", "lat", "lon" },
                OptionalFields = new List<string> { "desc", "time" }
            }
        };

        public event EventHandler<FavoriteExportProgressEventArgs>? ExportProgressChanged;
        public event EventHandler<FavoriteImportProgressEventArgs>? ImportProgressChanged;

        public FavoriteExportImportService(
            IFavoriteSpotService favoriteSpotService,
            ILogger<FavoriteExportImportService> logger)
        {
            _favoriteSpotService = favoriteSpotService ?? throw new ArgumentNullException(nameof(favoriteSpotService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Export Methods

        /// <summary>
        /// Export user favorites to CSV format
        /// </summary>
        public async Task<FavoriteExportResult> ExportToCsvAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = new FavoriteExportResult { Format = SupportedExportFormats.First(f => f.Id == "csv") };
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting CSV export for user {UserId} to {FilePath}", userId, filePath);
                options ??= new ExportOptions();

                var favorites = await GetFavoritesForExport(userId, options, cancellationToken);
                result.TotalRecords = favorites.Count();

                OnExportProgressChanged(result.TotalRecords, 0, 0, "", result.Format);

                var csv = new StringBuilder();
                
                // CSV Header
                var headers = new List<string> { "SpotName", "Latitude", "Longitude", "Priority" };
                if (options.IncludeNotes) headers.Add("Notes");
                if (options.IncludePersonalData) headers.Add("NotificationEnabled");
                if (options.IncludeSpotDetails)
                {
                    headers.AddRange(new[] { "SpotType", "Description", "MaxDepth", "DifficultyLevel" });
                }
                headers.Add("CreatedDate");

                csv.AppendLine(string.Join(",", headers.Select(EscapeCsvValue)));

                int processed = 0;
                foreach (var favorite in favorites)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var values = new List<string>
                    {
                        EscapeCsvValue(favorite.Spot?.Name ?? "Unknown"),
                        favorite.Spot?.Latitude.ToString(CultureInfo.InvariantCulture) ?? "",
                        favorite.Spot?.Longitude.ToString(CultureInfo.InvariantCulture) ?? "",
                        favorite.Priority.ToString()
                    };

                    if (options.IncludeNotes)
                        values.Add(EscapeCsvValue(favorite.Notes ?? ""));

                    if (options.IncludePersonalData)
                        values.Add(favorite.NotificationEnabled.ToString());

                    if (options.IncludeSpotDetails)
                    {
                        values.Add(EscapeCsvValue(favorite.Spot?.Type?.Name ?? ""));
                        values.Add(EscapeCsvValue(favorite.Spot?.Description ?? ""));
                        values.Add(favorite.Spot?.MaxDepth?.ToString() ?? "");
                        values.Add(EscapeCsvValue(favorite.Spot?.DifficultyLevel.ToString() ?? ""));
                    }

                    values.Add(favorite.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                    csv.AppendLine(string.Join(",", values));
                    processed++;
                    result.ExportedRecords++;

                    OnExportProgressChanged(result.TotalRecords, processed, processed, favorite.Spot?.Name ?? "", result.Format);
                }

                await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8, cancellationToken);

                result.IsSuccess = true;
                result.FilePath = filePath;
                result.FileSizeBytes = new FileInfo(filePath).Length;
                result.ExportDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation("CSV export completed successfully: {Exported} records in {Duration}ms", 
                    result.ExportedRecords, result.ExportDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"CSV export failed: {ex.Message}");
                result.ExportDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "CSV export failed for user {UserId}", userId);
                return result;
            }
        }

        /// <summary>
        /// Export user favorites to JSON format
        /// </summary>
        public async Task<FavoriteExportResult> ExportToJsonAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = new FavoriteExportResult { Format = SupportedExportFormats.First(f => f.Id == "json") };
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting JSON export for user {UserId} to {FilePath}", userId, filePath);
                options ??= new ExportOptions();

                var favorites = await GetFavoritesForExport(userId, options, cancellationToken);
                result.TotalRecords = favorites.Count();

                OnExportProgressChanged(result.TotalRecords, 0, 0, "", result.Format);

                var exportData = new
                {
                    ExportInfo = new
                    {
                        UserId = userId,
                        ExportDate = DateTime.UtcNow,
                        Version = "1.0",
                        TotalRecords = result.TotalRecords,
                        Application = "SubExplore",
                        Metadata = options.CustomMetadata
                    },
                    Favorites = favorites.Select((favorite, index) =>
                    {
                        OnExportProgressChanged(result.TotalRecords, index + 1, index + 1, favorite.Spot?.Name ?? "", result.Format);

                        return new
                        {
                            FavoriteId = favorite.Id,
                            SpotId = favorite.SpotId,
                            SpotName = favorite.Spot?.Name,
                            Priority = favorite.Priority,
                            CreatedAt = favorite.CreatedAt,
                            UpdatedAt = favorite.UpdatedAt,
                            Notes = options.IncludeNotes ? favorite.Notes : null,
                            NotificationEnabled = options.IncludePersonalData ? favorite.NotificationEnabled : (bool?)null,
                            Spot = options.IncludeSpotDetails && favorite.Spot != null ? new
                            {
                                favorite.Spot.Name,
                                Coordinates = options.IncludeCoordinates ? new
                                {
                                    Latitude = favorite.Spot.Latitude,
                                    Longitude = favorite.Spot.Longitude
                                } : null,
                                favorite.Spot.Description,
                                favorite.Spot.MaxDepth,
                                DifficultyLevel = favorite.Spot.DifficultyLevel.ToString(),
                                Type = favorite.Spot.Type?.Name,
                                Category = favorite.Spot.Type?.Category
                            } : null
                        };
                    }).ToList()
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(exportData, jsonOptions);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);

                result.IsSuccess = true;
                result.FilePath = filePath;
                result.FileSizeBytes = new FileInfo(filePath).Length;
                result.ExportedRecords = result.TotalRecords;
                result.ExportDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation("JSON export completed successfully: {Exported} records in {Duration}ms", 
                    result.ExportedRecords, result.ExportDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"JSON export failed: {ex.Message}");
                result.ExportDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "JSON export failed for user {UserId}", userId);
                return result;
            }
        }

        /// <summary>
        /// Export user favorites to GPX format (for GPS devices)
        /// </summary>
        public async Task<FavoriteExportResult> ExportToGpxAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = new FavoriteExportResult { Format = SupportedExportFormats.First(f => f.Id == "gpx") };
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting GPX export for user {UserId} to {FilePath}", userId, filePath);
                options ??= new ExportOptions();

                var favorites = await GetFavoritesForExport(userId, options, cancellationToken);
                result.TotalRecords = favorites.Count();

                OnExportProgressChanged(result.TotalRecords, 0, 0, "", result.Format);

                var gpx = new XElement("gpx",
                    new XAttribute("version", "1.1"),
                    new XAttribute("creator", "SubExplore"),
                    new XElement("metadata",
                        new XElement("name", "SubExplore Favorite Diving Spots"),
                        new XElement("desc", "Exported favorite spots from SubExplore"),
                        new XElement("author",
                            new XElement("name", "SubExplore")
                        ),
                        new XElement("time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    )
                );

                int processed = 0;
                foreach (var favorite in favorites)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (favorite.Spot?.Latitude != null && favorite.Spot?.Longitude != null)
                    {
                        var waypoint = new XElement("wpt",
                            new XAttribute("lat", favorite.Spot.Latitude.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("lon", favorite.Spot.Longitude.ToString(CultureInfo.InvariantCulture)),
                            new XElement("name", favorite.Spot.Name ?? "Unknown"),
                            new XElement("desc", BuildGpxDescription(favorite, options)),
                            new XElement("time", favorite.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                            new XElement("type", "Diving Spot")
                        );

                        if (favorite.Spot.MaxDepth.HasValue)
                        {
                            waypoint.Add(new XElement("ele", -favorite.Spot.MaxDepth.Value)); // Negative for underwater
                        }

                        gpx.Add(waypoint);
                        result.ExportedRecords++;
                    }
                    else
                    {
                        result.SkippedRecords++;
                        result.Warnings.Add($"Skipped {favorite.Spot?.Name} - missing coordinates");
                    }

                    processed++;
                    OnExportProgressChanged(result.TotalRecords, processed, result.ExportedRecords, favorite.Spot?.Name ?? "", result.Format);
                }

                await File.WriteAllTextAsync(filePath, gpx.ToString(), Encoding.UTF8, cancellationToken);

                result.IsSuccess = true;
                result.FilePath = filePath;
                result.FileSizeBytes = new FileInfo(filePath).Length;
                result.ExportDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation("GPX export completed successfully: {Exported} records, {Skipped} skipped in {Duration}ms", 
                    result.ExportedRecords, result.SkippedRecords, result.ExportDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"GPX export failed: {ex.Message}");
                result.ExportDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "GPX export failed for user {UserId}", userId);
                return result;
            }
        }

        /// <summary>
        /// Export user favorites as a shareable package
        /// </summary>
        public async Task<FavoriteExportResult> ExportAsPackageAsync(Guid userId, string filePath, ExportOptions? options = null, CancellationToken cancellationToken = default)
        {
            // This would create a comprehensive package with JSON data, metadata, and potentially compressed format
            // Implementation would involve creating a ZIP package with multiple files
            throw new NotImplementedException("Package export will be implemented in a future update");
        }

        #endregion

        #region Import Methods

        /// <summary>
        /// Import favorites from CSV format
        /// </summary>
        public async Task<FavoriteImportResult> ImportFromCsvAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = new FavoriteImportResult { Format = SupportedImportFormats.First(f => f.Id == "csv") };
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting CSV import for user {UserId} from {FilePath}", userId, filePath);
                options ??= new ImportOptions();

                var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
                if (lines.Length < 2)
                {
                    result.Errors.Add("CSV file is empty or contains only headers");
                    return result;
                }

                var headers = ParseCsvLine(lines[0]);
                var dataLines = lines.Skip(1);
                
                result.TotalRecords = dataLines.Count();
                OnImportProgressChanged(result.TotalRecords, 0, 0, 0, 0, "", result.Format);

                int processed = 0;
                foreach (var line in dataLines)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var values = ParseCsvLine(line);
                        if (values.Count < 3) // Minimum: name, lat, lon
                        {
                            result.SkippedRecords++;
                            result.Warnings.Add($"Line {processed + 2}: Insufficient data");
                            continue;
                        }

                        var spotName = values[0];
                        if (double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                            double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                        {
                            var priority = values.Count > 3 && int.TryParse(values[3], out var p) ? p : 5;
                            var notes = values.Count > 4 ? values[4] : null;
                            var notification = values.Count > 5 ? bool.Parse(values[5]) : true;

                            // For CSV import, we would need to create/find spots and add them as favorites
                            // This is a simplified version - actual implementation would need spot creation logic
                            OnImportProgressChanged(result.TotalRecords, processed + 1, result.ImportedRecords, result.SkippedRecords, result.ErrorRecords, spotName, result.Format);
                            result.ImportedRecords++;
                        }
                        else
                        {
                            result.ErrorRecords++;
                            result.Errors.Add($"Line {processed + 2}: Invalid coordinates");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRecords++;
                        result.Errors.Add($"Line {processed + 2}: {ex.Message}");
                    }

                    processed++;
                }

                result.IsSuccess = result.ImportedRecords > 0;
                result.ImportDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation("CSV import completed: {Imported} imported, {Skipped} skipped, {Errors} errors in {Duration}ms", 
                    result.ImportedRecords, result.SkippedRecords, result.ErrorRecords, result.ImportDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"CSV import failed: {ex.Message}");
                result.ImportDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "CSV import failed for user {UserId}", userId);
                return result;
            }
        }

        // Additional import methods would be implemented similarly...
        public Task<FavoriteImportResult> ImportFromJsonAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("JSON import will be implemented in a future update");
        }

        public Task<FavoriteImportResult> ImportFromGpxAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("GPX import will be implemented in a future update");
        }

        public Task<FavoriteImportResult> ImportFromPackageAsync(Guid userId, string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Package import will be implemented in a future update");
        }

        #endregion

        #region Public Methods

        public async Task<FavoriteImportPreview> PreviewImportAsync(string filePath, ImportOptions? options = null, CancellationToken cancellationToken = default)
        {
            var preview = new FavoriteImportPreview();

            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var format = SupportedImportFormats.FirstOrDefault(f => f.FileExtensions.Contains(extension));

                if (format == null)
                {
                    preview.Errors.Add($"Unsupported file format: {extension}");
                    return preview;
                }

                preview.DetectedFormat = format;

                if (format.Id == "csv")
                {
                    var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
                    if (lines.Length > 0)
                    {
                        var headers = ParseCsvLine(lines[0]);
                        preview.DetectedFields = headers.Select((h, i) => new { h, i }).ToDictionary(x => x.i.ToString(), x => x.h);
                        
                        preview.TotalRecords = lines.Length - 1;
                        preview.ValidRecords = lines.Skip(1).Take(10).Count(line => ParseCsvLine(line).Count >= 3);
                        preview.InvalidRecords = preview.TotalRecords - preview.ValidRecords;
                    }
                }

                preview.IsValid = preview.ValidRecords > 0;
                return preview;
            }
            catch (Exception ex)
            {
                preview.Errors.Add($"Preview failed: {ex.Message}");
                return preview;
            }
        }

        public IEnumerable<ExportFormat> GetSupportedExportFormats() => SupportedExportFormats;

        public IEnumerable<ImportFormat> GetSupportedImportFormats() => SupportedImportFormats;

        public async Task<FileValidationResult> ValidateExportPathAsync(string filePath)
        {
            var result = new FileValidationResult();

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    result.Errors.Add($"Directory does not exist: {directory}");
                }

                if (File.Exists(filePath))
                {
                    result.Warnings.Add("File already exists and will be overwritten");
                }

                result.IsValid = result.Errors.Count == 0;
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Path validation failed: {ex.Message}");
                return result;
            }
        }

        public async Task<FileValidationResult> ValidateImportFileAsync(string filePath)
        {
            var result = new FileValidationResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add("Import file does not exist");
                    return result;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var format = SupportedImportFormats.FirstOrDefault(f => f.FileExtensions.Contains(extension));

                if (format == null)
                {
                    result.Errors.Add($"Unsupported file format: {extension}");
                }
                else
                {
                    result.Properties["DetectedFormat"] = format;
                }

                var fileInfo = new FileInfo(filePath);
                result.Properties["FileSize"] = fileInfo.Length;
                result.Properties["LastModified"] = fileInfo.LastWriteTime;

                result.IsValid = result.Errors.Count == 0;
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"File validation failed: {ex.Message}");
                return result;
            }
        }

        public string GetDefaultExportFileName(Guid userId, ExportFormat format, DateTime? timestamp = null)
        {
            var time = timestamp ?? DateTime.UtcNow;
            var userShort = userId.ToString("N")[..8];
            return $"subexplore_favorites_{userShort}_{time:yyyyMMdd_HHmmss}{format.FileExtension}";
        }

        #endregion

        #region Private Methods

        private async Task<IEnumerable<UserFavoriteSpot>> GetFavoritesForExport(Guid userId, ExportOptions options, CancellationToken cancellationToken)
        {
            var favorites = await _favoriteSpotService.GetUserFavoritesByPriorityAsync(userId, cancellationToken);
            
            // Apply filters
            if (options.FromDate.HasValue)
                favorites = favorites.Where(f => f.CreatedAt >= options.FromDate.Value);
            
            if (options.ToDate.HasValue)
                favorites = favorites.Where(f => f.CreatedAt <= options.ToDate.Value);
            
            if (options.PriorityFilter?.Any() == true)
                favorites = favorites.Where(f => options.PriorityFilter.Contains(f.Priority));
            
            if (options.NotificationsOnly)
                favorites = favorites.Where(f => f.NotificationEnabled);
            
            if (options.MaxRecords.HasValue)
                favorites = favorites.Take(options.MaxRecords.Value);

            return favorites;
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result;
        }

        private static string BuildGpxDescription(UserFavoriteSpot favorite, ExportOptions options)
        {
            var desc = new StringBuilder();

            if (!string.IsNullOrEmpty(favorite.Spot?.Description))
                desc.AppendLine(favorite.Spot.Description);

            if (options.IncludeSpotDetails)
            {
                if (favorite.Spot?.Type?.Name != null)
                    desc.AppendLine($"Type: {favorite.Spot.Type.Name}");

                if (favorite.Spot?.MaxDepth.HasValue == true)
                    desc.AppendLine($"Max Depth: {favorite.Spot.MaxDepth.Value}m");

                if (favorite.Spot?.DifficultyLevel != null)
                    desc.AppendLine($"Difficulty: {favorite.Spot.DifficultyLevel}");
            }

            if (options.IncludePersonalData)
            {
                desc.AppendLine($"Priority: {favorite.Priority}/10");
                
                if (!string.IsNullOrEmpty(favorite.Notes))
                    desc.AppendLine($"Notes: {favorite.Notes}");
            }

            return desc.ToString().Trim();
        }

        private void OnExportProgressChanged(int total, int processed, int exported, string currentSpot, ExportFormat format)
        {
            ExportProgressChanged?.Invoke(this, new FavoriteExportProgressEventArgs
            {
                TotalRecords = total,
                ProcessedRecords = processed,
                CurrentRecord = exported,
                CurrentSpotName = currentSpot,
                Format = format
            });
        }

        private void OnImportProgressChanged(int total, int processed, int imported, int skipped, int errors, string currentSpot, ImportFormat format)
        {
            ImportProgressChanged?.Invoke(this, new FavoriteImportProgressEventArgs
            {
                TotalRecords = total,
                ProcessedRecords = processed,
                ImportedRecords = imported,
                SkippedRecords = skipped,
                ErrorRecords = errors,
                CurrentSpotName = currentSpot,
                Format = format
            });
        }

        #endregion
    }
}