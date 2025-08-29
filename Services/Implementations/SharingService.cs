using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using System.Text;

namespace SubExplore.Services.Implementations
{
    public class SharingService : ISharingService
    {
        private readonly IDialogService _dialogService;

        public SharingService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public bool IsNativeSharingAvailable => true; // Assume sharing is always available on modern platforms

        public async Task<bool> ShareSpotAsync(Spot spot, bool includePhotos = false)
        {
            try
            {
                if (spot == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Aucun spot à partager", "OK");
                    return false;
                }

                // Generate share content
                var shareTitle = $"Spot de plongée: {spot.Name}";
                var shareText = GenerateSpotShareText(spot);
                var shareLink = GenerateSpotShareLink(spot.Id);

                var shareRequest = new ShareTextRequest
                {
                    Title = shareTitle,
                    Text = $"{shareText}\n\n🔗 Voir plus: {shareLink}"
                };

                await Share.Default.RequestAsync(shareRequest);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ShareSpotAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de partager ce spot: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> ShareTextAsync(string title, string text, string? uri = null)
        {
            try
            {
                var shareRequest = new ShareTextRequest
                {
                    Title = title,
                    Text = text,
                    Uri = uri
                };

                await Share.Default.RequestAsync(shareRequest);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ShareTextAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de partager: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> ShareFileAsync(string title, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Fichier non trouvé", "OK");
                    return false;
                }

                var shareRequest = new ShareFileRequest
                {
                    Title = title,
                    File = new ShareFile(filePath)
                };

                await Share.Default.RequestAsync(shareRequest);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ShareFileAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de partager le fichier: {ex.Message}", "OK");
                return false;
            }
        }

        public string GenerateSpotShareLink(Guid spotId)
        {
            // Generate deep link for the spot
            return $"subexplore://spotdetails?spotId={spotId}";
        }

        private string GenerateSpotShareText(Spot spot)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🏊‍♂️ Découvrez ce spot de plongée incroyable!");
            sb.AppendLine();
            sb.AppendLine($"📍 {spot.Name}");
            
            if (!string.IsNullOrEmpty(spot.Description))
            {
                var description = spot.Description.Length > 100 
                    ? spot.Description.Substring(0, 97) + "..." 
                    : spot.Description;
                sb.AppendLine($"📝 {description}");
            }

            sb.AppendLine($"🏊 Type: {spot.Type?.Name ?? "Spot de plongée"}");
            
            if (spot.MaxDepth.HasValue)
                sb.AppendLine($"🌊 Profondeur max: {spot.MaxDepth}m");

            sb.AppendLine($"📊 Difficulté: {spot.DifficultyLevel}");
            sb.AppendLine($"📍 Coordonnées: {spot.Latitude:F6}, {spot.Longitude:F6}");

            if (!string.IsNullOrEmpty(spot.SafetyNotes))
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ Notes de sécurité:");
                var safetyNotes = spot.SafetyNotes.Length > 150 
                    ? spot.SafetyNotes.Substring(0, 147) + "..." 
                    : spot.SafetyNotes;
                sb.AppendLine(safetyNotes);
            }

            sb.AppendLine();
            sb.AppendLine("📱 Partagé depuis SubExplore");

            return sb.ToString();
        }
    }
}