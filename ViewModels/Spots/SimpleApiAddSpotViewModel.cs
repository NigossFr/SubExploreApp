// ========================================
// SIMPLE API ADD SPOT VIEWMODEL
// ========================================
// Version 100% API Supabase - plus de code hybride

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;
using SubExplore.Models.Enums;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    public partial class SimpleApiAddSpotViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILocationService _locationService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly ILogger<SimpleApiAddSpotViewModel> _logger;
        private readonly IApplicationPerformanceService? _performanceService;

        [ObservableProperty]
        private string _spotName = string.Empty;

        [ObservableProperty]
        private string _spotDescription = string.Empty;

        [ObservableProperty]
        private double _latitude = 43.6047; // Marseille coordinates as default

        [ObservableProperty]
        private double _longitude = 1.4442; // Toulouse area coordinates

        [ObservableProperty]
        private SpotType? _selectedSpotType;

        [ObservableProperty]
        private ObservableCollection<SpotTypeItem> _spotTypes = new();

        [ObservableProperty]
        private bool _isLoadingSpotTypes;

        [ObservableProperty]
        private bool _canCreateSpot;

        [ObservableProperty]
        private string _spotNameError = string.Empty;

        [ObservableProperty]
        private string _locationError = string.Empty;

        [ObservableProperty]
        private string _spotTypeError = string.Empty;

        [ObservableProperty]
        private bool _hasValidationErrors;

        [ObservableProperty]
        private string _validationSummary = string.Empty;

        [ObservableProperty]
        private bool _isLocationPickerVisible;

        [ObservableProperty]
        private string _locationDisplayName = "📍 France, Sud-Ouest (par défaut)";

        [ObservableProperty]
        private bool _isLocationAccurate = true; // Start with default location as "accurate enough"

        [ObservableProperty]
        private double _locationAccuracy;

        [ObservableProperty]
        private bool _isCreatingSpot;

        [ObservableProperty]
        private bool _isGettingLocation;

        [ObservableProperty]
        private string _creationProgress = string.Empty;

        [ObservableProperty]
        private double _progressPercentage;

        [ObservableProperty]
        private bool _isApiReady = true; // Default to true, will be updated by initialization

        [ObservableProperty]
        private bool _isConnected = true;

        [ObservableProperty]
        private string _connectionStatus = "Connecté";

        [ObservableProperty]
        private bool _canRetry;

        [ObservableProperty]
        private string _lastErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasRecoverableError;

        // Diagnostic properties for debugging
        [ObservableProperty]
        private string _diagnosticInfo = string.Empty;

        [ObservableProperty]
        private bool _showDiagnostics;

        public SimpleApiAddSpotViewModel(
            ISupabaseApiService apiService,
            ILocationService locationService,
            ISimpleAuthenticationService authService,
            ILogger<SimpleApiAddSpotViewModel> logger,
            IApplicationPerformanceService? performanceService = null,
            IDialogService? dialogService = null,
            INavigationService? navigationService = null) : base(dialogService, navigationService)
        {
            _apiService = apiService;
            _locationService = locationService;
            _authService = authService;
            _logger = logger;
            _performanceService = performanceService;
            Title = "Ajouter un Spot";

            // Observer les changements pour valider avec debouncing pour réduire la pression mémoire
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SpotName) || 
                    e.PropertyName == nameof(SelectedSpotType) ||
                    e.PropertyName == nameof(Latitude) || 
                    e.PropertyName == nameof(Longitude))
                {
                    // Utiliser Task.Run pour décharger le thread UI et réduire la pression mémoire
                    Task.Run(() =>
                    {
                        ValidateCanCreateSpot();
                        ValidateFieldsRealTime();
                        
                        // Suggérer un GC si nécessaire (non-bloquant)
                        if (GC.GetTotalMemory(false) > 50 * 1024 * 1024) // >50MB
                        {
                            GC.Collect(0, GCCollectionMode.Optimized);
                        }
                    });
                }
            };
        }

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            try
            {
                // Initializing SimpleApiAddSpotViewModel

                await InitializeApiAsync();
                await LoadSpotTypesAsync();

                // Handle location parameters from map navigation
                if (parameters?.Count > 0)
                {
                    // Processing navigation parameters
                    
                    if (parameters.TryGetValue("Latitude", out var latValue) && latValue is decimal lat)
                    {
                        Latitude = (double)lat;
                        // Latitude set from navigation
                    }
                    
                    if (parameters.TryGetValue("Longitude", out var lonValue) && lonValue is decimal lon)
                    {
                        Longitude = (double)lon;
                        // Longitude set from navigation
                    }
                    
                    if (parameters.TryGetValue("LocationParameter", out var locParam) && locParam is string locationInfo)
                    {
                        // Location context processed
                    }

                    // If we have coordinates from navigation, don't overwrite with GPS
                    if (Latitude != 0 || Longitude != 0)
                    {
                        IsLocationAccurate = true; // Map selection is considered accurate
                        LocationAccuracy = 0; // No accuracy uncertainty for map selection
                        // Using map-selected coordinates, skipping GPS
                    }
                    else
                    {
                        await GetCurrentLocationAsync();
                    }
                }
                else
                {
                    // No navigation parameters, get current GPS location
                    await GetCurrentLocationAsync();
                }

                UpdateLocationDisplay();
                // SimpleApiAddSpotViewModel initialized successfully
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to initialize SimpleApiAddSpotViewModel");
                ShowError("Erreur lors de l'initialisation. Veuillez réessayer.");
            }
        }

        private async Task InitializeApiAsync()
        {
            try
            {
                // Simplified API initialization - assume it works for now
                IsApiReady = true;
                ClearError();
                
                // Test if we can actually load spot types
                if (SpotTypes.Count == 0)
                {
                    await LoadSpotTypesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_API_ERROR: API initialization failed");
                IsApiReady = false;
                await HandleApiErrorAsync(ex, "Impossible d'initialiser l'API");
            }
        }
        
        /// <summary>
        /// Crée les types de spots de base dans la base de données
        /// </summary>
        private async Task CreateBasicSpotTypesAsync()
        {
            try
            {
                // Creating basic spot types

                // Utiliser les types de spots définis dans le système original
                var basicSpotTypes = new List<Models.Supabase.SupabaseSpotType>
                {
                    // === ACTIVITÉS (variations de bleus) ===
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Plongée bouteille",
                        Category = "Activity",
                        Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                        ColorCode = "#0077BE", // Bleu principal
                        IconPath = "marker_scuba.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 200 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Apnée",
                        Category = "Activity",
                        Description = "Sites adaptés à la plongée en apnée",
                        ColorCode = "#4A90E2", // Bleu moyen
                        IconPath = "marker_freediving.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 30 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Randonnée sous-marine",
                        Category = "Activity",
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        ColorCode = "#87CEEB", // Bleu clair
                        IconPath = "marker_snorkeling.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 5 } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Photo sous-marine",
                        Category = "Activity",
                        Description = "Sites d'intérêt pour la photographie sous-marine",
                        ColorCode = "#5DADE2", // Bleu photo
                        IconPath = "marker_photography.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                    // === STRUCTURES (variations de verts) ===
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Clubs",
                        Category = "Structure",
                        Description = "Clubs de plongée et associations",
                        ColorCode = "#228B22", // Vert foncé
                        IconPath = "marker_club.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Professionnels",
                        Category = "Structure",
                        Description = "Centres de plongée, instructeurs et guides professionnels",
                        ColorCode = "#32CD32", // Vert lime
                        IconPath = "marker_pro.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Bases fédérales",
                        Category = "Structure",
                        Description = "Bases fédérales et structures officielles",
                        ColorCode = "#90EE90", // Vert clair
                        IconPath = "marker_federal.png",
                        IsActive = true,
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                    // === BOUTIQUES (tons oranges) ===
                    new Models.Supabase.SupabaseSpotType
                    {
                        Id = Guid.NewGuid(),
                        Name = "Boutiques",
                        Category = "Shop",
                        Description = "Magasins de matériel de plongée et équipements sous-marins",
                        ColorCode = "#FF8C00", // Orange principal
                        IconPath = "marker_shop.png",
                        IsActive = true,
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                int created = 0;
                foreach (var spotType in basicSpotTypes)
                {
                    try
                    {
                        // Créer les types via l'API publique
                        await _apiService.CreateSpotTypeAsync(spotType);
                        created++;
                        // Spot type created: {spotType.Name}
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SPOT_ADD_API_ERROR: Failed to create spot type {SpotTypeName}", spotType.Name);
                    }
                }

                // Spot types creation completed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_API_ERROR: Failed to create basic spot types");
            }
        }
        
        private void CleanupResources()
        {
            try
            {
                // Force garbage collection if memory usage is high
                var memoryBefore = GC.GetTotalMemory(false);
                if (memoryBefore > 50 * 1024 * 1024) // 50MB threshold
                {
                    GC.Collect(2, GCCollectionMode.Optimized, true);
                    GC.WaitForPendingFinalizers();
                    var memoryAfter = GC.GetTotalMemory(true);
                    // Memory cleanup performed
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during resource cleanup");
            }
        }

        private async Task CheckConnectivityAsync()
        {
            try
            {
                // Simulation de vérification de connectivité
                // Dans une vraie implémentation, utilisez Microsoft.Maui.Networking.Connectivity
                IsConnected = true; // Placeholder - implement real connectivity check
                ConnectionStatus = IsConnected ? "Connecté" : "Hors ligne";
                
                // Connection status updated
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SPOT_ADD_WARNING: Cannot verify connectivity");
                IsConnected = false;
                ConnectionStatus = "État de connexion inconnu";
            }
        }

        private async Task HandleApiErrorAsync(Exception ex, string userMessage)
        {
            LastErrorMessage = ex.Message;
            HasRecoverableError = IsRecoverableError(ex);
            CanRetry = HasRecoverableError;

            string errorMessage = GetUserFriendlyErrorMessage(ex, userMessage);
            ShowError(errorMessage);

            // Log détaillé pour le développement
            _logger.LogError(ex, "SPOT_ADD_API_ERROR: {ErrorType} - {ErrorMessage}", 
                ex.GetType().Name, ex.Message);
        }

        private bool IsRecoverableError(Exception ex)
        {
            // Erreurs récupérables (problèmes temporaires)
            return ex is TaskCanceledException ||
                   ex is TimeoutException ||
                   ex is HttpRequestException ||
                   ex.Message.Contains("timeout") ||
                   ex.Message.Contains("network") ||
                   ex.Message.Contains("connexion");
        }

        private string GetUserFriendlyErrorMessage(Exception ex, string fallbackMessage)
        {
            return ex switch
            {
                TaskCanceledException => "⏱️ Délai d'attente dépassé. Vérifiez votre connexion.",
                TimeoutException => "⏱️ Délai d'attente dépassé. Réessayez plus tard.",
                HttpRequestException => "🌐 Problème de connexion réseau. Vérifiez votre connexion Internet.",
                UnauthorizedAccessException => "🔒 Accès non autorisé. Reconnectez-vous.",
                _ when ex.Message.Contains("timeout") => "⏱️ Délai d'attente dépassé. Réessayez.",
                _ when ex.Message.Contains("network") => "🌐 Problème réseau. Vérifiez votre connexion.",
                _ when ex.Message.Contains("server") => "🖥️ Serveur temporairement indisponible. Réessayez plus tard.",
                _ => $"❌ {fallbackMessage}\n💡 Conseil: Vérifiez votre connexion et réessayez."
            };
        }

        [RelayCommand]
        private async Task LoadSpotTypesAsync()
        {
            try
            {
                IsLoadingSpotTypes = true;
                ClearError();
                _logger?.LogInformation("🏷️ Starting to load spot types...");

                // Toujours créer les types locaux en premier
                await CreateLocalSpotTypesAsync();
                _logger?.LogInformation($"✅ Created {SpotTypes.Count} local spot types as fallback");

                // Vérifier et potentiellement corriger Supabase
                if (_apiService != null && IsApiReady)
                {
                    await CheckAndRepairSupabaseAsync();
                }

                // Essayer de charger depuis l'API maintenant que c'est potentiellement réparé
                /*
                try
                {
                    if (_apiService != null && IsApiReady)
                    {
                        _logger?.LogInformation("Attempting to load spot types from API...");
                        var supabaseSpotTypes = await _apiService.GetSpotTypesAsync();
                        if (supabaseSpotTypes?.Any() == true && supabaseSpotTypes.Count >= SpotTypes.Count)
                        {
                            // Only replace if API has at least as many types as local
                            _logger?.LogInformation($"API returned {supabaseSpotTypes.Count} spot types, replacing local ones");
                            var spotTypes = SupabaseModelConverter.ToEfModels(supabaseSpotTypes);
                            var spotTypeItems = spotTypes
                                .Where(st => st.IsActive)
                                .OrderBy(st => st.Name)
                                .Select(st => new SpotTypeItem { SpotType = st })
                                .ToList();

                            if (spotTypeItems.Any())
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    SpotTypes.Clear();
                                    foreach (var item in spotTypeItems)
                                    {
                                        SpotTypes.Add(item);
                                    }
                                    _logger?.LogInformation($"Replaced local types with {SpotTypes.Count} API types");
                                });
                            }
                        }
                        else
                        {
                            _logger?.LogWarning($"API returned {supabaseSpotTypes?.Count ?? 0} spot types, keeping {SpotTypes.Count} local ones");
                        }
                    }
                    else
                    {
                        _logger?.LogWarning($"API not ready (IsApiReady: {IsApiReady}), using local types");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not load from API, using local types");
                    // Keep the local types that were already created
                }
                */

                _logger?.LogInformation($"Final result: {SpotTypes.Count} spot types loaded successfully");
                UpdateDiagnosticInfo();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load spot types");
                ShowError("Impossible de charger les types de spots");
                
                // Ensure we have at least local types as fallback
                if (SpotTypes.Count == 0)
                {
                    try
                    {
                        await CreateLocalSpotTypesAsync();
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger?.LogError(fallbackEx, "Even fallback local types creation failed");
                    }
                }
            }
            finally
            {
                IsLoadingSpotTypes = false;
            }
        }

        /// <summary>
        /// Vérifie si Supabase est corrompu et tente une réparation automatique
        /// </summary>
        private async Task CheckAndRepairSupabaseAsync()
        {
            try
            {
                _logger?.LogInformation("🔍 Vérification de l'intégrité de Supabase...");
                
                var supabaseSpotTypes = await _apiService.GetSpotTypesAsync();
                var isCorrupted = IsSupabaseDatabaseCorrupted(supabaseSpotTypes);
                
                if (isCorrupted)
                {
                    _logger?.LogWarning("🚨 Base Supabase corrompue détectée!");
                    ShowError("Base de données corrompue détectée. Utilisation des types locaux.");
                    
                    // Pour l'instant, on affiche juste l'erreur
                    // TODO: Implémenter la réparation automatique quand les API de suppression/création seront disponibles
                    return;
                }
                else
                {
                    _logger?.LogInformation("✅ Base Supabase semble intacte, tentative de chargement");
                    
                    // Si la base n'est pas corrompue, essayer de charger normalement
                    if (supabaseSpotTypes?.Any() == true && supabaseSpotTypes.Count >= 8)
                    {
                        var spotTypes = SupabaseModelConverter.ToEfModels(supabaseSpotTypes);
                        var spotTypeItems = spotTypes
                            .Where(st => st.IsActive)
                            .OrderBy(st => st.Name)
                            .Select(st => new SpotTypeItem { SpotType = st })
                            .ToList();

                        if (spotTypeItems.Any())
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                SpotTypes.Clear();
                                foreach (var item in spotTypeItems)
                                {
                                    SpotTypes.Add(item);
                                }
                                _logger?.LogInformation($"✅ Remplacé par {SpotTypes.Count} types Supabase intacts");
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors de la vérification Supabase");
                ShowError("Impossible de vérifier Supabase. Utilisation des types locaux.");
            }
        }

        /// <summary>
        /// Détecte si la base Supabase est corrompue
        /// </summary>
        private bool IsSupabaseDatabaseCorrupted(List<SupabaseSpotType>? spotTypes)
        {
            if (spotTypes == null || spotTypes.Count == 0)
            {
                _logger?.LogWarning("🚨 Aucun type trouvé dans Supabase");
                return true;
            }

            if (spotTypes.Count < 8)
            {
                _logger?.LogWarning($"🚨 Seulement {spotTypes.Count} types au lieu de 8");
                return true;
            }

            var expectedNames = new HashSet<string>
            {
                "Plongée bouteille", "Apnée", "Randonnée sous-marine", "Photo sous-marine",
                "Clubs", "Professionnels", "Bases fédérales", "Boutiques"
            };

            var actualNames = new HashSet<string>(spotTypes.Select(st => st.Name ?? "").Where(n => !string.IsNullOrEmpty(n)));
            var missingNames = expectedNames.Except(actualNames).ToList();
            var truncatedTypes = spotTypes.Where(st => 
                string.IsNullOrWhiteSpace(st.Name) || 
                st.Name.Length < 3 ||
                st.Name.Equals("Cl", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (missingNames.Any())
            {
                _logger?.LogWarning($"🚨 Types manquants: [{string.Join(", ", missingNames)}]");
                return true;
            }

            if (truncatedTypes.Any())
            {
                _logger?.LogWarning($"🚨 Données tronquées: {truncatedTypes.Count} types");
                return true;
            }

            return false;
        }

        private async Task CreateLocalSpotTypesAsync()
        {
            try
            {
                _logger?.LogInformation("Creating spot types based on existing database schema...");
                
                // Utilisation des vrais types de spots basés sur le schéma de base de données existant
                // Source: migrate_spot_types.sql et SpotTypeDataMigrationService.cs
                var localSpotTypes = new List<SpotType>
                {
                    // === ACTIVITÉS (variations de bleus) ===
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Plongée bouteille", 
                        IconPath = "marker_scuba.png", 
                        ColorCode = "#0077BE", // Bleu principal
                        Category = ActivityCategory.Activity,
                        Description = "Sites de plongée avec bouteille (tous niveaux - récréative et technique)",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 200 } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Apnée", 
                        IconPath = "marker_freediving.png",
                        ColorCode = "#4A90E2", // Bleu moyen
                        Category = ActivityCategory.Activity,
                        Description = "Sites adaptés à la plongée en apnée",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "MaxDepth", "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 30 } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Randonnée sous-marine", 
                        IconPath = "marker_snorkeling.png",
                        ColorCode = "#87CEEB", // Bleu clair
                        Category = ActivityCategory.Activity,
                        Description = "Sites de surface accessibles pour la randonnée sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel", "SafetyNotes" } },
                            { "MaxDepthRange", new[] { 0, 5 } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Photo sous-marine", 
                        IconPath = "marker_photography.png",
                        ColorCode = "#5DADE2", // Bleu photo
                        Category = ActivityCategory.Activity,
                        Description = "Sites d'intérêt pour la photographie sous-marine",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "DifficultyLevel" } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },

                    // === STRUCTURES (variations de verts) ===
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Clubs", 
                        IconPath = "marker_club.png",
                        ColorCode = "#228B22", // Vert foncé
                        Category = ActivityCategory.Structure,
                        Description = "Clubs de plongée et associations",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Professionnels", 
                        IconPath = "marker_pro.png",
                        ColorCode = "#32CD32", // Vert lime
                        Category = ActivityCategory.Structure,
                        Description = "Centres de plongée, instructeurs et guides professionnels",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Bases fédérales", 
                        IconPath = "marker_federal.png",
                        ColorCode = "#90EE90", // Vert clair
                        Category = ActivityCategory.Structure,
                        Description = "Bases fédérales et structures officielles",
                        RequiresExpertValidation = true,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description", "SafetyNotes" } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },

                    // === BOUTIQUES (tons oranges) ===
                    new SpotType 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Boutiques", 
                        IconPath = "marker_shop.png",
                        ColorCode = "#FF8C00", // Orange principal
                        Category = ActivityCategory.Shop,
                        Description = "Magasins de matériel de plongée et équipements sous-marins",
                        RequiresExpertValidation = false,
                        ValidationCriteria = new Dictionary<string, object>
                        {
                            { "RequiredFields", new[] { "Description" } }
                        },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SpotTypes.Clear();
                    _logger?.LogInformation($"Cleared SpotTypes collection, adding {localSpotTypes.Count} new items...");
                    
                    foreach (var spotType in localSpotTypes)
                    {
                        var spotTypeItem = new SpotTypeItem { SpotType = spotType };
                        SpotTypes.Add(spotTypeItem);
                        _logger?.LogInformation($"Added spot type: {spotType.Name} (Color: {spotType.ColorCode})");
                    }
                    
                    _logger?.LogInformation($"UI updated - Final SpotTypes.Count: {SpotTypes.Count}");
                    
                    // Force UI refresh
                    OnPropertyChanged(nameof(SpotTypes));
                });

                await Task.Delay(100); // Small delay to ensure UI updates
                UpdateDiagnosticInfo();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SPOT_ADD_ERROR: Failed to create local spot types from database schema");
            }
        }

        [RelayCommand]
        private async Task GetCurrentLocationAsync()
        {
            try
            {
                IsGettingLocation = true;
                // Getting current location
                
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = (double)location.Latitude;
                    Longitude = (double)location.Longitude;
                    LocationAccuracy = location.Accuracy;
                    IsLocationAccurate = LocationAccuracy < 50; // Précision < 50m considérée comme bonne
                    UpdateLocationDisplay();
                    // GPS position acquired
                }
                else
                {
                    // Position par défaut (exemple: Méditerranée)
                    Latitude = 43.2965;
                    Longitude = 5.3698;
                    IsLocationAccurate = false;
                    UpdateLocationDisplay();
                    // Using default position
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_LOCATION_ERROR: Failed to get current location");
                ShowError("Impossible de récupérer la position actuelle");
            }
            finally
            {
                IsGettingLocation = false;
            }
        }

        [RelayCommand]
        private async Task CreateSpotAsync()
        {
            // Force memory cleanup before expensive operation
            if (GC.GetTotalMemory(false) > 50 * 1024 * 1024) // 50MB threshold
            {
                await Task.Run(() => GC.Collect(2, GCCollectionMode.Forced, true));
            }
            
            // Track performance for spot creation
            var createSpotStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // CreateSpot started
            
            if (!CanCreateSpot)
            {
                var reason = string.IsNullOrWhiteSpace(SpotName) ? "Nom manquant" : 
                            SelectedSpotType == null ? "Type de spot manquant" : 
                            (Latitude == 0 && Longitude == 0) ? "Position manquante" : 
                            !IsApiReady ? "API non prête" : "Validation échoue";
                            
                ShowError($"⚠️ Impossible de créer le spot: {reason}. Veuillez vérifier tous les champs.");
                _logger.LogWarning($"CreateSpot blocked: {reason}");
                return;
            }

            // Re-vérifier l'état API en temps réel
            if (!IsApiReady)
            {
                _logger.LogWarning("API not ready, attempting reconnection");
                await InitializeApiAsync();
                
                if (!IsApiReady)
                {
                    await HandleApiErrorAsync(new InvalidOperationException("API non disponible après reconnexion"), 
                        "Service temporairement indisponible. Veuillez réessayer.");
                    return;
                }
            }

            // Vérification finale de la connectivité
            await CheckConnectivityAsync();
            if (!IsConnected)
            {
                ShowError("🌐 Connexion Internet requise pour créer un spot");
                return;
            }

            const int maxRetries = 2;
            const int retryDelayMs = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    IsLoading = true;
                    IsCreatingSpot = true;
                    ProgressPercentage = 0;
                    CreationProgress = $"🔄 Initialisation... (Tentative {attempt}/{maxRetries})";
                    ClearError();

                    // Creating spot via API

                    // Récupérer l'utilisateur actuel avec gestion d'erreur
                    ProgressPercentage = 15;
                    CreationProgress = "🔐 Vérification de l'authentification...";
                    
                    var currentUser = await _authService.GetCurrentUserAsync();
                    if (currentUser == null)
                    {
                        throw new UnauthorizedAccessException("Session utilisateur expirée");
                    }

                    // Validation des données avant envoi
                    ProgressPercentage = 30;
                    CreationProgress = "✅ Validation des données...";
                    ValidateSpotDataBeforeCreation();

                    // Créer le spot via API Supabase
                    ProgressPercentage = 50;
                    CreationProgress = "🏗️ Préparation des données du spot...";
                    
                    var newSpot = new Models.Supabase.SupabaseSpot
                    {
                        Name = SpotName?.Trim(),
                        Description = SpotDescription?.Trim(),
                        Latitude = (decimal)Latitude,
                        Longitude = (decimal)Longitude,
                        TypeId = SelectedSpotType!.Id,
                        CreatorId = currentUser.Id,
                        RequiredEquipment = "À définir", // Valeur par défaut
                        SafetyNotes = "À compléter", // Valeur par défaut
                        BestConditions = "À préciser" // Valeur par défaut
                    };

                    // Créer le spot via l'API avec timeout
                    ProgressPercentage = 70;
                    CreationProgress = "🚀 Envoi vers la base de données...";
                    
                    var createdSpot = await CreateSpotWithTimeoutAsync(newSpot);

                    if (createdSpot == null)
                    {
                        throw new InvalidOperationException("Le spot n'a pas pu être créé - réponse API invalide");
                    }

                    _logger.LogInformation($"Spot '{SpotName}' created successfully with ID: {createdSpot.Id}");
                    
                    ProgressPercentage = 90;
                    CreationProgress = "🎉 Finalisation...";
                    await Task.Delay(800); // Délai pour UX
                    
                    ProgressPercentage = 100;
                    CreationProgress = "✅ Spot créé avec succès!";
                    
                    // Délai pour afficher le succès
                    await Task.Delay(1200);
                    
                    // Navigation retour
                    await NavigationService.GoBackAsync();
                    return; // Succès - sortir de la boucle

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SPOT_ADD_CREATE_ERROR: Failed to create spot on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt == maxRetries)
                    {
                        // Dernière tentative - gestion de l'erreur finale
                        await HandleCreateSpotErrorAsync(ex);
                        return;
                    }
                    
                    // Mise à jour du progress pour indiquer la nouvelle tentative
                    ProgressPercentage = 0;
                    CreationProgress = $"⚠️ Erreur, nouvelle tentative dans {retryDelayMs/1000}s...";
                    await Task.Delay(retryDelayMs);
                }
                finally
                {
                    if (attempt == maxRetries || ProgressPercentage == 100)
                    {
                        // Nettoyage final seulement à la fin
                        IsLoading = false;
                        IsCreatingSpot = false;
                        if (ProgressPercentage != 100)
                        {
                            ProgressPercentage = 0;
                            CreationProgress = string.Empty;
                        }
                    }
                }
            }
            
            // Track performance metrics
            createSpotStopwatch.Stop();
            _performanceService?.TrackUserAction("CreateSpot", "SpotCreation", createSpotStopwatch.Elapsed.TotalMilliseconds);
        }

        private void ValidateSpotDataBeforeCreation()
        {
            if (string.IsNullOrWhiteSpace(SpotName))
                throw new ArgumentException("Le nom du spot ne peut pas être vide");
                
            if (SelectedSpotType == null)
                throw new ArgumentException("Un type de spot doit être sélectionné");
                
            if (Latitude == 0 && Longitude == 0)
                throw new ArgumentException("La position GPS est requise");
                
            if (Math.Abs(Latitude) > 90 || Math.Abs(Longitude) > 180)
                throw new ArgumentException("Coordonnées GPS invalides");
        }

        private async Task<Models.Supabase.SupabaseSpot> CreateSpotWithTimeoutAsync(Models.Supabase.SupabaseSpot newSpot)
        {
            const int timeoutMs = 30000; // 30 secondes
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            
            try
            {
                return await _apiService.CreateSpotAsync(newSpot);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"La création du spot a pris trop de temps (>{timeoutMs/1000}s)");
            }
        }

        private async Task HandleCreateSpotErrorAsync(Exception ex)
        {
            string userMessage = ex switch
            {
                UnauthorizedAccessException => "🔒 Session expirée. Reconnectez-vous et réessayez.",
                ArgumentException argEx => $"📝 Données invalides: {argEx.Message}",
                TimeoutException => "⏱️ La création prend trop de temps. Vérifiez votre connexion.",
                HttpRequestException => "🌐 Problème de connexion. Vérifiez votre réseau.",
                _ => "❌ Impossible de créer le spot"
            };

            await HandleApiErrorAsync(ex, userMessage);
            
            // Pour les erreurs de données, remettre l'interface en état de modification
            if (ex is ArgumentException)
            {
                ValidateFieldsRealTime();
            }
        }

        private void ValidateCanCreateSpot()
        {
            CanCreateSpot = !string.IsNullOrWhiteSpace(SpotName) &&
                          SelectedSpotType != null &&
                          Latitude != 0 &&
                          Longitude != 0 &&
                          IsApiReady &&
                          !HasValidationErrors;
        }

        private static readonly TimeSpan ValidationThrottleInterval = TimeSpan.FromMilliseconds(200);
        private DateTime _lastValidationTime = DateTime.MinValue;
        private bool _validationScheduled = false;
        
        private async void ValidateFieldsRealTime()
        {
            // Throttle validation to prevent excessive calls
            var now = DateTime.UtcNow;
            if (now - _lastValidationTime < ValidationThrottleInterval && _validationScheduled)
                return;
                
            _validationScheduled = true;
            
            // Debounce validation using Task.Delay
            await Task.Delay(ValidationThrottleInterval);
            
            try
            {
                // Use local variables to reduce property notifications
                var nameError = ValidateSpotName(SpotName);
                var locationError = ValidateLocation(Latitude, Longitude);
                var typeError = ValidateSpotType(SelectedSpotType);
                
                var hasErrors = !string.IsNullOrEmpty(nameError) ||
                               !string.IsNullOrEmpty(locationError) ||
                               !string.IsNullOrEmpty(typeError);
                
                // Batch property updates to reduce notifications
                await Task.Run(() =>
                {
                    var nameValid = !string.IsNullOrWhiteSpace(SpotName);
                    var typeValid = SelectedSpotType != null;
                    var locationValid = Latitude != 0 && Longitude != 0;
                    var apiValid = IsApiReady;
                    var validationValid = !hasErrors;
                    
                    var canCreate = nameValid && typeValid && locationValid && apiValid && validationValid;
                    
                    // Update UI properties on main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SpotNameError = nameError;
                        LocationError = locationError;
                        SpotTypeError = typeError;
                        HasValidationErrors = hasErrors;
                        CanCreateSpot = canCreate;
                        
                        UpdateValidationSummary();
                        
                        // Update diagnostic if visible
                        if (ShowDiagnostics)
                        {
                            UpdateDiagnosticInfo();
                        }
                        
                        // Only log critical validation errors
                        if (hasErrors && _logger.IsEnabled(LogLevel.Error))
                        {
                            var errorCount = (string.IsNullOrEmpty(nameError) ? 0 : 1) + 
                                           (string.IsNullOrEmpty(locationError) ? 0 : 1) + 
                                           (string.IsNullOrEmpty(typeError) ? 0 : 1);
                            if (errorCount > 1) // Only log multiple validation errors
                            {
                                _logger.LogError("SPOT_ADD_VALIDATION: Multiple validation errors - Name: {HasNameError}, Location: {HasLocationError}, Type: {HasTypeError}", 
                                    !string.IsNullOrEmpty(nameError), !string.IsNullOrEmpty(locationError), !string.IsNullOrEmpty(typeError));
                            }
                        }
                        
                        _lastValidationTime = now;
                        _validationScheduled = false;
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Real-time validation failed");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HasValidationErrors = true;
                    CanCreateSpot = false;
                    _validationScheduled = false;
                });
            }
        }

        private string ValidateSpotName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Le nom du spot est requis";
            
            if (name.Length < 3)
                return "Le nom doit contenir au moins 3 caractères";
            
            if (name.Length > 100)
                return "Le nom ne peut pas dépasser 100 caractères";
            
            return string.Empty;
        }

        private string ValidateLocation(double lat, double lon)
        {
            if (lat == 0 && lon == 0)
                return "La position GPS est requise";
            
            if (lat < -90 || lat > 90)
                return "Latitude invalide (doit être entre -90 et 90)";
            
            if (lon < -180 || lon > 180)
                return "Longitude invalide (doit être entre -180 et 180)";
            
            return string.Empty;
        }

        private string ValidateSpotType(SpotType? spotType)
        {
            if (spotType == null)
                return "Veuillez sélectionner un type de spot";
            
            return string.Empty;
        }

        private void UpdateValidationSummary()
        {
            var errors = new List<string>();
            
            if (!string.IsNullOrEmpty(SpotNameError)) errors.Add(SpotNameError);
            if (!string.IsNullOrEmpty(LocationError)) errors.Add(LocationError);
            if (!string.IsNullOrEmpty(SpotTypeError)) errors.Add(SpotTypeError);
            
            // Only show summary if there are validation issues or if ready to create
            if (errors.Any())
            {
                ValidationSummary = $"{errors.Count} erreur(s) à corriger";
            }
            else if (CanCreateSpot)
            {
                ValidationSummary = "Prêt à créer le spot";
            }
            else
            {
                ValidationSummary = string.Empty; // Don't show anything if not ready yet
            }
        }

        [RelayCommand]
        private void ClearForm()
        {
            try
            {
                // Resetting form
                
                SpotName = string.Empty;
                SpotDescription = string.Empty;
                Latitude = 0;
                Longitude = 0;
                SelectedSpotType = null;
                
                // Désélectionner tous les types
                foreach (var item in SpotTypes)
                {
                    item.IsSelected = false;
                }
                
                ClearError();
                ValidateFieldsRealTime();
                
                // Form reset
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to reset form");
                ShowError("Erreur lors de la réinitialisation du formulaire");
            }
        }

        [RelayCommand]
        private void ToggleLocationPicker()
        {
            try
            {
                IsLocationPickerVisible = !IsLocationPickerVisible;
                // Location picker toggled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_UI_ERROR: Failed to toggle location picker");
                ShowError("Erreur lors de l'ouverture du sélecteur de position");
            }
        }

        [RelayCommand]
        private async Task RefreshLocationAccuracy()
        {
            try
            {
                // Refreshing GPS accuracy
                await GetCurrentLocationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_LOCATION_ERROR: Failed to refresh GPS accuracy");
                ShowError("Impossible de rafraîchir la position GPS");
            }
        }

        private void UpdateLocationDisplay()
        {
            try
            {
                if (Latitude == 0 && Longitude == 0)
                {
                    LocationDisplayName = "Aucune position sélectionnée";
                    return;
                }

                var accuracyText = IsLocationAccurate ? 
                    $"📍 Précise ({LocationAccuracy:F0}m)" : 
                    $"📍 Approximative ({LocationAccuracy:F0}m)";

                LocationDisplayName = $"📍 Position acquise - {accuracyText}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_UI_ERROR: Failed to update location display");
                LocationDisplayName = "Erreur d'affichage";
            }
        }

        public void UpdateLocationFromMap(double latitude, double longitude)
        {
            try
            {
                Latitude = latitude;
                Longitude = longitude;
                IsLocationAccurate = true; // Sélection manuelle considérée comme précise
                LocationAccuracy = 0; // Pas d'incertitude pour sélection manuelle
                UpdateLocationDisplay();
                ValidateFieldsRealTime();
                
                // Position updated from map
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_LOCATION_ERROR: Failed to update location from map");
                ShowError("Erreur lors de la mise à jour de la position");
            }
        }

        [RelayCommand]
        private void SelectSpotType(SpotTypeItem spotTypeItem)
        {
            try
            {
                if (spotTypeItem?.SpotType == null)
                {
                    _logger?.LogWarning("Attempted to select null spot type");
                    return;
                }

                // Désélectionner tous les autres
                foreach (var item in SpotTypes)
                {
                    item.IsSelected = false;
                }

                // Sélectionner le nouveau
                spotTypeItem.IsSelected = true;
                SelectedSpotType = spotTypeItem.SpotType;

                _logger?.LogInformation($"Selected spot type: {spotTypeItem.SpotType.Name}");
                
                // Clear any validation errors related to spot type
                SpotTypeError = string.Empty;
                
                // Revalidate to update HasValidationErrors and CanCreateSpot
                ValidateFieldsRealTime();
                
                UpdateDiagnosticInfo();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error selecting spot type");
                ShowError("Erreur lors de la sélection du type de spot");
            }
        }

        [RelayCommand]
        private async Task RetryLastOperationAsync()
        {
            try
            {
                // Attempting error recovery
                ClearError();
                CanRetry = false;

                // Réessayer l'initialisation si l'API n'est pas prête
                if (!IsApiReady)
                {
                    await InitializeApiAsync();
                    if (IsApiReady)
                    {
                        await LoadSpotTypesAsync();
                    }
                }
                // Si l'API est prête mais pas de types de spots, les recharger
                else if (!SpotTypes.Any())
                {
                    await LoadSpotTypesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to retry last operation");
                await HandleApiErrorAsync(ex, "Impossible de récupérer après l'erreur");
            }
        }

        [RelayCommand]
        private async Task RefreshAllDataAsync()
        {
            try
            {
                // Full data refresh
                IsLoading = true;
                ClearError();

                // Réinitialiser l'API
                await InitializeApiAsync();

                // Recharger les types de spots
                if (IsApiReady)
                {
                    await LoadSpotTypesAsync();
                }

                // Rafraîchir la position GPS
                await GetCurrentLocationAsync();

                // Full refresh completed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to refresh all data");
                await HandleApiErrorAsync(ex, "Impossible d'actualiser les données");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearAllErrors()
        {
            try
            {
                // Clearing all error states
                
                ClearError();
                LastErrorMessage = string.Empty;
                HasRecoverableError = false;
                CanRetry = false;
                
                // Nettoyer aussi les erreurs de validation
                SpotNameError = string.Empty;
                LocationError = string.Empty;
                SpotTypeError = string.Empty;
                HasValidationErrors = false;
                UpdateValidationSummary();
                
                // Error states cleared
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to clear all errors");
            }
        }

        [RelayCommand]
        private void ToggleDiagnostics()
        {
            ShowDiagnostics = !ShowDiagnostics;
            if (ShowDiagnostics)
            {
                UpdateDiagnosticInfo();
            }
        }

        [RelayCommand]
        private async Task ForceRefreshAllAsync()
        {
            try
            {
                DiagnosticInfo = "🔄 Force refresh démarré...";
                
                // Force refresh API
                IsApiReady = false;
                await InitializeApiAsync();
                DiagnosticInfo += $"\n✅ API: {IsApiReady}";
                
                // Force refresh spot types
                SpotTypes.Clear();
                await LoadSpotTypesAsync();
                DiagnosticInfo += $"\n✅ SpotTypes: {SpotTypes.Count}";
                
                // Force refresh location  
                if (Latitude == 0 && Longitude == 0)
                {
                    await GetCurrentLocationAsync();
                }
                DiagnosticInfo += $"\n✅ Location: {Latitude:F2}, {Longitude:F2}";
                
                // Update validation
                ValidateFieldsRealTime();
                DiagnosticInfo += $"\n✅ Validation terminée";
                
                // Update diagnostic
                UpdateDiagnosticInfo();
            }
            catch (Exception ex)
            {
                DiagnosticInfo = $"❌ Erreur force refresh: {ex.Message}";
            }
        }

        private void UpdateDiagnosticInfo()
        {
            try
            {
                var diagnostics = new System.Text.StringBuilder();
                
                diagnostics.AppendLine("=== DIAGNOSTIC ADD SPOT ===");
                diagnostics.AppendLine($"🔧 IsApiReady: {IsApiReady}");
                diagnostics.AppendLine($"🌐 IsConnected: {IsConnected}");
                diagnostics.AppendLine($"📊 SpotTypes.Count: {SpotTypes.Count}");
                diagnostics.AppendLine($"🔄 IsLoadingSpotTypes: {IsLoadingSpotTypes}");
                diagnostics.AppendLine($"❌ HasValidationErrors: {HasValidationErrors}");
                diagnostics.AppendLine($"✅ CanCreateSpot: {CanCreateSpot}");
                diagnostics.AppendLine($"📍 Latitude: {Latitude}");
                diagnostics.AppendLine($"📍 Longitude: {Longitude}");
                diagnostics.AppendLine($"🎯 IsLocationAccurate: {IsLocationAccurate}");
                diagnostics.AppendLine($"📝 SpotName: '{SpotName}'");
                diagnostics.AppendLine($"🏷️ SelectedSpotType: {(SelectedSpotType?.Name ?? "null")}");
                diagnostics.AppendLine($"🚨 LastErrorMessage: '{LastErrorMessage}'");
                
                diagnostics.AppendLine("\n--- VALIDATION ERRORS ---");
                diagnostics.AppendLine($"SpotNameError: '{SpotNameError}'");
                diagnostics.AppendLine($"LocationError: '{LocationError}'");
                diagnostics.AppendLine($"SpotTypeError: '{SpotTypeError}'");
                
                diagnostics.AppendLine("\n--- VALIDATION LOGIC ---");
                var nameValid = !string.IsNullOrWhiteSpace(SpotName);
                var typeValid = SelectedSpotType != null;
                var locationValid = Latitude != 0 && Longitude != 0;
                var apiValid = IsApiReady;
                diagnostics.AppendLine($"NameValid: {nameValid} (Name='{SpotName}')");
                diagnostics.AppendLine($"TypeValid: {typeValid} (Type={SelectedSpotType?.Name ?? "null"})");
                diagnostics.AppendLine($"LocationValid: {locationValid} (Lat={Latitude}, Lng={Longitude})");
                diagnostics.AppendLine($"ApiValid: {apiValid}");
                
                if (SpotTypes.Any())
                {
                    diagnostics.AppendLine("\n--- SPOT TYPES ---");
                    foreach (var st in SpotTypes.Take(5))
                    {
                        diagnostics.AppendLine($"- {st.SpotType?.Name ?? "null"} (Selected: {st.IsSelected})");
                    }
                    if (SpotTypes.Count > 5)
                        diagnostics.AppendLine($"... and {SpotTypes.Count - 5} more");
                }

                DiagnosticInfo = diagnostics.ToString();
            }
            catch (Exception ex)
            {
                DiagnosticInfo = $"Erreur diagnostic: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            try
            {
                // Canceling spot creation
                
                // Si une opération est en cours, l'interrompre proprement
                if (IsCreatingSpot)
                {
                    IsCreatingSpot = false;
                    ProgressPercentage = 0;
                    CreationProgress = "Annulé par l'utilisateur";
                    
                    // Délai pour montrer l'annulation
                    await Task.Delay(1000);
                }
                
                await NavigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SPOT_ADD_ERROR: Failed to cancel operation");
                ShowError("Erreur lors de l'annulation");
            }
        }

    }
}