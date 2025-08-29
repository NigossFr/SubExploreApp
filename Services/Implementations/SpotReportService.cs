using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class SpotReportService : ISpotReportService
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly IDialogService _dialogService;

        public SpotReportService(
            ISupabaseApiService supabaseApiService,
            ISimpleAuthenticationService authService,
            IDialogService dialogService)
        {
            _supabaseApiService = supabaseApiService;
            _authService = authService;
            _dialogService = dialogService;
        }

        public async Task<Guid?> SubmitReportAsync(Guid spotId, SpotReportType reportType, 
            string description, string? contactEmail = null, SpotReportSeverity severity = SpotReportSeverity.Low)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await _dialogService.ShowAlertAsync("Authentification requise", 
                        "Vous devez être connecté pour signaler un spot.", "OK");
                    return null;
                }

                // Check if user has already reported this spot
                if (await HasUserReportedSpotAsync(currentUser.Id, spotId))
                {
                    await _dialogService.ShowAlertAsync("Signalement déjà effectué", 
                        "Vous avez déjà signalé ce spot. Notre équipe examine votre rapport.", "OK");
                    return null;
                }

                var reportId = Guid.NewGuid();
                var report = new SpotReport
                {
                    Id = reportId,
                    SpotId = spotId,
                    ReporterId = currentUser.Id,
                    ReportType = reportType,
                    Description = description.Trim(),
                    ContactEmail = contactEmail?.Trim(),
                    Severity = severity,
                    Status = SpotReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                // TODO: Implement Supabase SpotReport storage
                // For now, simulate success and log to debug
                System.Diagnostics.Debug.WriteLine($"[REPORT] New spot report submitted:");
                System.Diagnostics.Debug.WriteLine($"[REPORT] - Spot: {spotId}");
                System.Diagnostics.Debug.WriteLine($"[REPORT] - Reporter: {currentUser.Id}");
                System.Diagnostics.Debug.WriteLine($"[REPORT] - Type: {reportType}");
                System.Diagnostics.Debug.WriteLine($"[REPORT] - Severity: {severity}");
                System.Diagnostics.Debug.WriteLine($"[REPORT] - Description: {description}");

                // TODO: Send to Supabase when SpotReport table is implemented
                // await _supabaseApiService.CreateSpotReportAsync(report);

                return reportId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SubmitReportAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de soumettre le signalement: {ex.Message}", "OK");
                return null;
            }
        }

        public async Task<List<SpotReport>> GetReportsForSpotAsync(Guid spotId)
        {
            try
            {
                // TODO: Implement Supabase query
                // return await _supabaseApiService.GetSpotReportsAsync(spotId);
                
                return new List<SpotReport>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetReportsForSpotAsync failed: {ex.Message}");
                return new List<SpotReport>();
            }
        }

        public async Task<List<SpotReport>> GetUserReportsAsync(Guid userId)
        {
            try
            {
                // TODO: Implement Supabase query
                return new List<SpotReport>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetUserReportsAsync failed: {ex.Message}");
                return new List<SpotReport>();
            }
        }

        public async Task<List<SpotReport>> GetPendingReportsAsync()
        {
            try
            {
                // TODO: Implement Supabase query for moderators
                return new List<SpotReport>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetPendingReportsAsync failed: {ex.Message}");
                return new List<SpotReport>();
            }
        }

        public async Task<bool> UpdateReportStatusAsync(Guid reportId, SpotReportStatus newStatus, 
            string reviewNotes, Guid reviewerId)
        {
            try
            {
                // TODO: Implement Supabase update
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateReportStatusAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HasUserReportedSpotAsync(Guid userId, Guid spotId)
        {
            try
            {
                // TODO: Implement Supabase query
                // For now, always return false to allow testing
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] HasUserReportedSpotAsync failed: {ex.Message}");
                return false;
            }
        }

        public Dictionary<SpotReportType, string> GetReportTypes()
        {
            return new Dictionary<SpotReportType, string>
            {
                { SpotReportType.SafetyConcern, "Problème de sécurité" },
                { SpotReportType.InaccurateInformation, "Informations incorrectes" },
                { SpotReportType.InappropriateContent, "Contenu inapproprié" },
                { SpotReportType.AccessIssues, "Problème d'accès au site" },
                { SpotReportType.EnvironmentalDamage, "Dommage environnemental" },
                { SpotReportType.PrivacyViolation, "Violation de la vie privée" },
                { SpotReportType.Spam, "Spam ou contenu indésirable" },
                { SpotReportType.Other, "Autre problème" }
            };
        }

        public Dictionary<SpotReportSeverity, string> GetSeverityLevels()
        {
            return new Dictionary<SpotReportSeverity, string>
            {
                { SpotReportSeverity.Low, "Faible - Information générale" },
                { SpotReportSeverity.Medium, "Moyenne - Nécessite attention" },
                { SpotReportSeverity.High, "Élevée - Action requise rapidement" },
                { SpotReportSeverity.Critical, "Critique - Action immédiate" },
                { SpotReportSeverity.Emergency, "Urgence - Danger imminent" }
            };
        }
    }
}