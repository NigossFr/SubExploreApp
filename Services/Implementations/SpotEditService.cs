using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class SpotEditService : ISpotEditService
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly IDialogService _dialogService;

        public SpotEditService(
            ISupabaseApiService supabaseApiService,
            ISimpleAuthenticationService authService,
            IDialogService dialogService)
        {
            _supabaseApiService = supabaseApiService;
            _authService = authService;
            _dialogService = dialogService;
        }

        public async Task<bool> CanUserEditSpotAsync(Spot spot)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    return false;

                // Rules for editing:
                // 1. Spot creator can always edit their own spots
                // 2. Admins/moderators can edit any spot
                // 3. Experts can edit spots in their domain (TODO: implement expert roles)
                
                bool isCreator = spot.CreatorId == currentUser.Id;
                bool isAdmin = false; // TODO: Implement role checking
                
                return isCreator || isAdmin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CanUserEditSpotAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSpotBasicInfoAsync(Guid spotId, string name, string description, 
            string requiredEquipment, string safetyNotes, string bestConditions)
        {
            try
            {
                // Create validation spot object
                var validationSpot = new Spot
                {
                    Id = spotId,
                    Name = name?.Trim() ?? string.Empty,
                    Description = description?.Trim() ?? string.Empty,
                    RequiredEquipment = requiredEquipment?.Trim() ?? string.Empty,
                    SafetyNotes = safetyNotes?.Trim() ?? string.Empty,
                    BestConditions = bestConditions?.Trim() ?? string.Empty
                };

                // Validate data
                var validation = await ValidateSpotDataAsync(validationSpot);
                if (!validation.IsValid)
                {
                    var errors = string.Join("\n", validation.Errors);
                    await _dialogService.ShowAlertAsync("Erreur de validation", errors, "OK");
                    return false;
                }

                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[EDIT] Updating basic info for spot {spotId}");
                System.Diagnostics.Debug.WriteLine($"[EDIT] - Name: {name}");
                System.Diagnostics.Debug.WriteLine($"[EDIT] - Description: {description?.Substring(0, Math.Min(50, description?.Length ?? 0))}...");

                // await _supabaseApiService.UpdateSpotBasicInfoAsync(spotId, name, description, requiredEquipment, safetyNotes, bestConditions);

                await LogEditAsync(spotId, "BasicInfo", $"Updated name, description, equipment, safety notes, and conditions");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateSpotBasicInfoAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour les informations: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> UpdateSpotLocationAsync(Guid spotId, decimal latitude, decimal longitude)
        {
            try
            {
                // Validate coordinates
                if (latitude < -90 || latitude > 90)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Latitude invalide (doit être entre -90 et 90)", "OK");
                    return false;
                }

                if (longitude < -180 || longitude > 180)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Longitude invalide (doit être entre -180 et 180)", "OK");
                    return false;
                }

                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[EDIT] Updating location for spot {spotId}: {latitude}, {longitude}");

                await LogEditAsync(spotId, "Location", $"Updated coordinates to {latitude:F6}, {longitude:F6}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateSpotLocationAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour la localisation: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> UpdateSpotTechnicalDetailsAsync(Guid spotId, int? maxDepth, 
            DifficultyLevel difficultyLevel, CurrentStrength? currentStrength)
        {
            try
            {
                // Validate depth
                if (maxDepth.HasValue && (maxDepth.Value < 0 || maxDepth.Value > 200))
                {
                    await _dialogService.ShowAlertAsync("Erreur", 
                        "Profondeur invalide (doit être entre 0 et 200 mètres)", "OK");
                    return false;
                }

                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[EDIT] Updating technical details for spot {spotId}");
                System.Diagnostics.Debug.WriteLine($"[EDIT] - MaxDepth: {maxDepth}m");
                System.Diagnostics.Debug.WriteLine($"[EDIT] - Difficulty: {difficultyLevel}");
                System.Diagnostics.Debug.WriteLine($"[EDIT] - Current: {currentStrength}");

                await LogEditAsync(spotId, "TechnicalDetails", 
                    $"Updated depth: {maxDepth}m, difficulty: {difficultyLevel}, current: {currentStrength}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateSpotTechnicalDetailsAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour les détails techniques: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> UpdateSpotTypeAsync(Guid spotId, Guid newTypeId)
        {
            try
            {
                // TODO: Validate that the new type exists
                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[EDIT] Updating spot type for {spotId} to {newTypeId}");

                await LogEditAsync(spotId, "SpotType", $"Changed spot type to {newTypeId}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateSpotTypeAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour le type de spot: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<bool> UpdateSpotSafetyFlagsAsync(Guid spotId, Dictionary<string, object> safetyFlags)
        {
            try
            {
                // TODO: Implement Supabase update
                System.Diagnostics.Debug.WriteLine($"[EDIT] Updating safety flags for spot {spotId}");
                foreach (var flag in safetyFlags)
                {
                    System.Diagnostics.Debug.WriteLine($"[EDIT] - {flag.Key}: {flag.Value}");
                }

                await LogEditAsync(spotId, "SafetyFlags", $"Updated safety flags: {safetyFlags.Count} flags");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateSpotSafetyFlagsAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de mettre à jour les indicateurs de sécurité: {ex.Message}", "OK");
                return false;
            }
        }

        public async Task<List<SpotEditRecord>> GetSpotEditHistoryAsync(Guid spotId)
        {
            try
            {
                // TODO: Implement Supabase query
                return new List<SpotEditRecord>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetSpotEditHistoryAsync failed: {ex.Message}");
                return new List<SpotEditRecord>();
            }
        }

        public async Task<SpotEditValidationResult> ValidateSpotDataAsync(Spot spot)
        {
            var result = new SpotEditValidationResult { IsValid = true };

            try
            {
                // Name validation
                if (string.IsNullOrWhiteSpace(spot.Name))
                {
                    result.Errors.Add("Le nom du spot est requis");
                    result.IsValid = false;
                }
                else if (spot.Name.Length > 100)
                {
                    result.Errors.Add("Le nom du spot ne peut pas dépasser 100 caractères");
                    result.IsValid = false;
                }

                // Description validation
                if (string.IsNullOrWhiteSpace(spot.Description))
                {
                    result.Errors.Add("La description est requise");
                    result.IsValid = false;
                }

                // Coordinates validation
                if (spot.Latitude < -90 || spot.Latitude > 90)
                {
                    result.Errors.Add("Latitude invalide (doit être entre -90 et 90)");
                    result.IsValid = false;
                }

                if (spot.Longitude < -180 || spot.Longitude > 180)
                {
                    result.Errors.Add("Longitude invalide (doit être entre -180 et 180)");
                    result.IsValid = false;
                }

                // Depth validation
                if (spot.MaxDepth.HasValue && (spot.MaxDepth.Value < 0 || spot.MaxDepth.Value > 200))
                {
                    result.Errors.Add("Profondeur invalide (doit être entre 0 et 200 mètres)");
                    result.IsValid = false;
                }

                // Add warnings for missing optional fields
                if (string.IsNullOrWhiteSpace(spot.RequiredEquipment))
                {
                    result.Warnings.Add("Équipement requis non spécifié");
                }

                if (string.IsNullOrWhiteSpace(spot.SafetyNotes))
                {
                    result.Warnings.Add("Notes de sécurité non spécifiées");
                }

                // Determine if revalidation is required
                result.RequiresRevalidation = result.Errors.Count > 0 || 
                    (spot.MaxDepth.HasValue && spot.MaxDepth > 30); // Deep dives need revalidation

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ValidateSpotDataAsync failed: {ex.Message}");
                result.Errors.Add("Erreur lors de la validation des données");
                result.IsValid = false;
            }

            return result;
        }

        public async Task<bool> SubmitForRevalidationAsync(Guid spotId, string editReason)
        {
            try
            {
                // TODO: Implement Supabase revalidation submission
                System.Diagnostics.Debug.WriteLine($"[EDIT] Submitting spot {spotId} for revalidation");
                System.Diagnostics.Debug.WriteLine($"[EDIT] Reason: {editReason}");

                await LogEditAsync(spotId, "Revalidation", $"Submitted for revalidation: {editReason}");
                
                await _dialogService.ShowToastAsync("Spot soumis pour revalidation par l'équipe de modération");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SubmitForRevalidationAsync failed: {ex.Message}");
                await _dialogService.ShowAlertAsync("Erreur", 
                    $"Impossible de soumettre pour revalidation: {ex.Message}", "OK");
                return false;
            }
        }

        private async Task LogEditAsync(Guid spotId, string editType, string changes)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null) return;

                // TODO: Implement edit history logging to Supabase
                System.Diagnostics.Debug.WriteLine($"[EDIT_LOG] Spot: {spotId}, Type: {editType}, Changes: {changes}, User: {currentUser.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LogEditAsync failed: {ex.Message}");
            }
        }
    }
}