// ========================================
// MauiProgram HYBRIDE ENTITY FRAMEWORK + SUPABASE
// ========================================
// Cette version utilise Entity Framework + PostgreSQL pour les performances
// tout en conservant l'API Supabase pour certaines fonctionnalités

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
// ✅ Entity Framework et Npgsql ajoutés pour l'approche hybride
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
// ✅ Repositories pour l'approche hybride Entity Framework + PostGIS
using SubExplore.Repositories.Interfaces;
using SubExplore.Repositories.Implementations;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
// using SubExplore.Repositories.Implementations;
// 🚫 Entity Framework supprimé - migration vers Supabase 100% API
// using Microsoft.EntityFrameworkCore;
// using SubExplore.DataAccess;
using SubExplore.Extensions;
// 🚫 Repositories Entity Framework supprimés - API Supabase uniquement
using SubExplore.Services.Validation;
using SubExplore.Services.Caching;
using SubExplore.Models.Validation;
using SubExplore.Models.Domain;
using SubExplore.ViewModels.Settings;
using SubExplore.ViewModels.Map;
using SubExplore.Constants;
using SubExplore.ViewModels.Spots;
using SubExplore.ViewModels.Organizations;
using SubExplore.ViewModels.Businesses;
using SubExplore.Views.Organizations;
using SubExplore.Views.Businesses;
using SubExplore.ViewModels.Profile;
// 🚫 ViewModels.Menu supprimé
// using SubExplore.ViewModels.Menu;
using SubExplore.ViewModels.Auth;
using SubExplore.ViewModels;
using SubExplore.Views.Spots.Components;
using SubExplore.Views.Settings;
using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Profile;
using SubExplore.Views.Auth;
using SubExplore.Views;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;
using SubExplore.Helpers;

namespace SubExplore;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // 📋 CONFIGURATION DES PARAMÈTRES DE L'APPLICATION AVEC RESSOURCE EMBARQUÉE
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("SubExplore.appsettings.json");
        if (stream != null)
        {
            builder.Configuration.AddJsonStream(stream);
        }
        else
        {
            // appsettings.json not found as embedded resource
        }

        // 🚀 CONFIGURATION 100% API SUPABASE - PLUS D'ENTITY FRAMEWORK
        // L'application utilise UNIQUEMENT l'API Supabase via supabase-csharp
        // Configuration 100% API Supabase - Plus d'Entity Framework
        
        // 🔧 CORRECTIF RESEAU EMULATEUR ANDROID
        EmulatorNetworkFix.ApplyEmulatorDnsFixIfNeeded();
        // Network diagnostic applied
        
        // 🔐 CONFIGURATION SERVICE - DOIT ÊTRE ENREGISTRÉ EN PREMIER
#if DEBUG
        // ✅ Supabase configuré avec les vraies clés API (Janvier 2025)
        builder.Services.AddSingleton<ISupabaseConfigurationService, DevelopmentConfigurationService>();
        // Using DevelopmentConfigurationService - SUPABASE ENABLED
        // Mode offline disponible avec: OfflineTestConfigurationService
#else
        builder.Services.AddSingleton<ISupabaseConfigurationService, SupabaseConfigurationService>();
        System.Diagnostics.Debug.WriteLine("[RELEASE] Using SupabaseConfigurationService for Supabase configuration");
#endif
        
        // Enregistrer le service client Supabase (APRÈS le service de configuration)
        builder.Services.AddSingleton<ISupabaseClientService, SupabaseClientService>();

        // ✅ ENTITY FRAMEWORK + POSTGRESQL CONFIGURATION HYBRIDE
        // Configuration du DbContext pour l'approche hybride performante
        builder.Services.AddDbContext<SubExploreDbContext>(options =>
        {
            var configuration = builder.Configuration;
            
            // Détection automatique de la plateforme pour la chaîne de connexion
            string connectionString = GetPlatformConnectionString(configuration);
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Configuration PostGIS pour les fonctions de géolocalisation
                npgsqlOptions.UseNetTopologySuite();
                // Timeout adapté aux requêtes PostGIS complexes
                npgsqlOptions.CommandTimeout(60);
            });

#if DEBUG
            // Logs SQL en développement pour le debugging
            options.EnableSensitiveDataLogging();
            options.LogTo(message => System.Diagnostics.Debug.WriteLine($"[EF] {message}"));
#endif
        });

        // ✅ REPOSITORIES HYBRIDES ENTITY FRAMEWORK + POSTGIS
        // Enregistrement des repositories génériques et spécialisés
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IPracticeSpotRepository, PracticeSpotRepository>();
        builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
        builder.Services.AddScoped<IUnifiedMediaRepository, UnifiedMediaRepository>();

        // ✅ SERVICE HYBRIDE POUR LA CARTE (Entity Framework + PostGIS + Supabase fallback)
        builder.Services.AddScoped<IHybridMapService, HybridMapService>();
        
        // ✅ SERVICES SUPABASE NATIFS (définis plus bas dans le fichier)

        // 🛡️ RESILIENCE SERVICES - Retry policies, circuit breakers, health monitoring
        builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
        builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
        builder.Services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();
        builder.Services.AddSingleton<IAutoReconnectService, AutoReconnectService>();
        builder.Services.AddSingleton<IFallbackDataService, FallbackDataService>();
        
        // 📡 NETWORK MONITORING SERVICES - Enhanced network health and offline capabilities
        builder.Services.AddSingleton<INetworkHealthService, NetworkHealthService>();
        builder.Services.AddSingleton<IOfflineCapabilityService, OfflineCapabilityService>();

        // 🚫 SupabaseService supprimé - utilisait Entity Framework
        
        // 🚀 SERVICES SUPABASE NATIFS - Solution 100% API
        builder.Services.AddScoped<ISupabaseApiService, SupabaseApiService>();
        builder.Services.AddSingleton<ISimpleSupabaseService, SimpleSupabaseService>();
        
        // ✅ NOUVEAUX SERVICES SUPABASE NATIFS
        builder.Services.AddScoped<ISupabaseSpotService, SupabaseSpotService>();
        builder.Services.AddScoped<ISupabaseSpotTypeService, SupabaseSpotTypeService>();
        builder.Services.AddScoped<ISupabaseUserService, SupabaseUserService>();

        // 🎯 ADD SPOT FORM SERVICES - Refactored form logic for better maintainability
        builder.Services.AddScoped<IAddSpotFormService, AddSpotFormService>();
        builder.Services.AddScoped<ISpotTypeService, SpotTypeService>();
        builder.Services.AddScoped<ISpotTypeTestDataService, SpotTypeTestDataService>();
        
        // 🔐 SERVICE D'AUTHENTIFICATION AVANCÉ
        builder.Services.AddSingleton<ISimpleAuthenticationService, EnhancedAuthenticationService>();

        // Services Supabase natifs configured
        
        // ✅ SERVICE D'INITIALISATION DE L'APPLICATION
        builder.Services.AddSingleton<IAppInitializationService, AppInitializationService>();
        
        builder.Services.AddSingleton<IDialogService, DialogService>();
        
        // 🎯 NEW SHELL SYSTEM SERVICES - Must be registered BEFORE NavigationService
        builder.Services.AddSubExploreServices();
        
        // 🔧 Updated NavigationService registration to include IShellRouteRegistry
        builder.Services.AddSingleton<INavigationService>(provider =>
        {
            var routeRegistry = provider.GetRequiredService<IShellRouteRegistry>();
            return new NavigationService(provider, routeRegistry);
        });
        builder.Services.AddScoped<INavigationGuardService, NavigationGuardService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IImageCacheService, ImageCacheService>();
        builder.Services.AddSingleton<IMapDiagnosticService, MapDiagnosticService>();
        builder.Services.AddSingleton<IPlatformMapService, PlatformMapService>();
        builder.Services.AddSingleton<IMenuService, MenuService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        // ✅ SimpleUserProfileService compatible Supabase API
        builder.Services.AddScoped<IUserProfileService, SimpleUserProfileService>();
        // 🚫 Services supprimés - utilisaient des repositories
        // builder.Services.AddScoped<ISpotService, SpotService>();
        // ✅ SERVICE DE FAVORIS 100% SUPABASE (avec Lazy pour éviter les dépendances circulaires)
        builder.Services.AddScoped<IFavoriteSpotService>(provider =>
        {
            var supabaseApi = provider.GetRequiredService<ISupabaseApiService>();
            var cacheService = provider.GetRequiredService<IFavoriteSpotCacheService>();
            var logger = provider.GetRequiredService<ILogger<SupabaseFavoriteSpotService>>();
            
            // Lazy loading pour éviter la dépendance circulaire
            var lazyOfflineService = new Lazy<IOfflineFavoriteService>(() => 
                provider.GetRequiredService<IOfflineFavoriteService>());
            
            var spotService = provider.GetRequiredService<ISupabaseSpotService>();
            return new SupabaseFavoriteSpotService(supabaseApi, cacheService, spotService, logger, lazyOfflineService);
        });
        builder.Services.AddSingleton<IFavoriteSpotCacheService, FavoriteSpotCacheService>();
        
        // ✅ ENHANCED FAVORITES SERVICES - Offline capabilities sans dépendance circulaire
        builder.Services.AddSingleton<IOfflineFavoriteService>(provider =>
        {
            var cacheService = provider.GetRequiredService<IFavoriteSpotCacheService>();
            var networkService = provider.GetRequiredService<INetworkHealthService>();
            var settingsService = provider.GetRequiredService<ISettingsService>();
            var logger = provider.GetRequiredService<ILogger<OfflineFavoriteService>>();
            
            // Création sans dépendance au IFavoriteSpotService pour éviter la circularité
            return new OfflineFavoriteService(null, cacheService, networkService, settingsService, logger);
        });
        builder.Services.AddScoped<IFavoriteExportImportService, FavoriteExportImportService>();
        // ✅ ErrorHandlingService with enhanced network error handling capabilities
        builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
        
        // Weather services
        builder.Services.AddSingleton<IWeatherCacheService, WeatherCacheService>();
        builder.Services.AddScoped<IWeatherService, WeatherService>();
        builder.Services.AddHttpClient<IWeatherService, WeatherService>();
        
        // Performance monitoring services
        builder.Services.AddSingleton<IPerformanceProfilingService, PerformanceProfilingService>();
        builder.Services.AddSingleton<IApplicationPerformanceService, ApplicationPerformanceService>();
        builder.Services.AddSingleton<IPerformanceOptimizationService, PerformanceOptimizationService>();
        
        // Add memory cache for improved performance
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.25;
        });
        
        // Add high-performance response caching service
        builder.Services.AddSingleton<IResponseCacheService, ResponseCacheService>();
        
        // Add specialized query result caching service
        builder.Services.AddScoped<IQueryCacheService, QueryCacheService>();
        
        // Add high-performance batch operation service
        builder.Services.AddSingleton<IBatchOperationService, BatchOperationService>();
        
        // Add response compression service
        builder.Services.AddSingleton<ICompressionService, CompressionService>();
        
        // Add request deduplication service
        builder.Services.AddSingleton<IRequestDeduplicationService, RequestDeduplicationService>();
        
        // Add HTTP client factory for better performance
        builder.Services.AddHttpClient();
        
        // Configuration HttpClient spécifique pour l'émulateur Android et Supabase
        builder.Services.AddHttpClient("SupabaseClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            
#if ANDROID
            // Configuration spéciale pour l'émulateur Android
            try 
            {
                if (EmulatorNetworkFix.IsRunningOnAndroidEmulator())
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Configuration HttpClient pour émulateur Android");
                    
                    // Validation SSL permissive pour Supabase en émulateur
                    handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        if (sender is HttpRequestMessage request)
                        {
                            var host = request.RequestUri?.Host;
                            if (host?.Contains("supabase.co") == true)
                            {
                                System.Diagnostics.Debug.WriteLine($"🔐 SSL validation bypassed for emulator: {host}");
                                return true;
                            }
                        }
                        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur configuration émulateur: {ex.Message}");
            }
#endif
            return handler;
        });
        // 🚫 PerformanceValidationService supprimé - utilisait des repositories
        // builder.Services.AddScoped<IPerformanceValidationService, PerformanceValidationService>();
        
        // Pin management optimization services
        builder.Services.AddSingleton<PinManagementConfig>();
        builder.Services.AddScoped<IPinManagementService, PinManagementService>();
        
        // Pin selection services
        builder.Services.AddSingleton<IPinSelectionService, PinSelectionService>();
        
        // Data optimization services
        builder.Services.AddSingleton<ISpotOptimizationService, SpotOptimizationService>();
        
        // Authentication services
        builder.Services.AddSingleton<ISecureSettingsService>(provider =>
        {
            var baseSettings = provider.GetRequiredService<ISettingsService>();
            return new SecureSettingsService(baseSettings);
        });
        // 🔐 CONFIGURATION SERVICE DÉJÀ ENREGISTRÉ AU DÉBUT
        builder.Services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();
        // 🚫 TokenService supprimé - utilisait des repositories
        // builder.Services.AddScoped<ITokenService, TokenService>();
        
        // 🔐 AUTHENTIFICATION 100% API SUPABASE
        builder.Services.AddSingleton<ISimpleAuthenticationService, SimpleAuthenticationService>();
        // ✅ NavigationGuardService now handles null IAuthenticationService gracefully
        builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
        
        // Email services
        builder.Services.AddSingleton<IEmailService, EmailService>();
        // 🚫 Services email supprimés - utilisaient des repositories
        // builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        // ✅ PASSWORD RESET SERVICE - 100% SUPABASE
        builder.Services.AddScoped<IPasswordResetService, StubPasswordResetService>();

        // ✅ ENHANCED SPOT SERVICES
        builder.Services.AddScoped<ISharingService, SharingService>();
        builder.Services.AddScoped<ISpotReportService, SpotReportService>();
        builder.Services.AddScoped<ISpotEditService, SpotEditService>();
        builder.Services.AddScoped<IEnhancedMediaService, EnhancedMediaService>();
        
        // ✅ Validation services - Supabase compatible
        builder.Services.AddScoped<ISpotValidationService, SupabaseSpotValidationService>();
        // builder.Services.AddScoped<TestDataService>();
        // builder.Services.AddScoped<SpotMigrationService>();
        
        // Validation strategy services
        builder.Services.AddScoped<IValidationStrategyFactory, ValidationStrategyFactory>();
        builder.Services.AddScoped<IValidationEventPublisher, ValidationEventPublisher>();
        
        // Validation event handlers
        builder.Services.AddScoped<IValidationEventHandler<SpotApprovedEvent>, SubExplore.Services.Implementations.NotificationEventHandler>();
        builder.Services.AddScoped<IValidationEventHandler<SpotRejectedEvent>, SubExplore.Services.Implementations.NotificationEventHandler>();
        builder.Services.AddScoped<IValidationEventHandler<SpotFlaggedForSafetyEvent>, SubExplore.Services.Implementations.NotificationEventHandler>();
        builder.Services.AddScoped<IValidationEventHandler<SpotApprovedEvent>, AnalyticsEventHandler>();
        builder.Services.AddScoped<IValidationEventHandler<SpotRejectedEvent>, AnalyticsEventHandler>();
        builder.Services.AddScoped<IValidationEventHandler<ValidationStatusChangedEvent>, AnalyticsEventHandler>();
        
        // Validation services
        builder.Services.AddScoped<IValidationService, ValidationService>();
        
        // Caching services
        builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
        builder.Services.AddScoped<ISpotCacheService, SpotCacheService>();
        
        // Configure logging
        // 🔧 AMÉLIORATION: Configuration de logging renforcée
#if DEBUG
        builder.Services.AddLogging(configure =>
        {
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Debug);
        });
#else
        builder.Services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.Information);
        });
#endif
        
        // Add HttpClient for image caching
        builder.Services.AddHttpClient<ImageCacheService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SubExplore/1.0");
        });

        // Enregistrement des ViewModels
        // 🚫 DatabaseTestViewModel supprimé - utilisait des services Entity Framework
        // builder.Services.AddTransient<DatabaseTestViewModel>();
        builder.Services.AddTransient<SimpleApiAddSpotViewModel>();
        builder.Services.AddTransient<RefactoredAddSpotViewModel>();
        builder.Services.AddTransient<SpotTypeSelectionViewModel>();
        builder.Services.AddTransient<OrganizationAddViewModel>();
        builder.Services.AddTransient<BusinessAddViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<OptimizedMapViewModel>();
        // ✅ NOUVEAU VIEWMODEL AVANCÉ 100% SUPABASE
        builder.Services.AddTransient<EnhancedMapViewModel>();
        // ✅ NOUVEAU VIEWMODEL HYBRIDE ENTITY FRAMEWORK + POSTGIS
        builder.Services.AddTransient<HybridMapViewModel>();
        // 🚫 ViewModels supprimés - utilisaient des repositories
        // builder.Services.AddTransient<SpotManagementViewModel>();
        // builder.Services.AddTransient<AddSpotViewModel>();
        builder.Services.AddTransient<SpotDetailsViewModel>();
        builder.Services.AddTransient<SpotEditViewModel>();
        // builder.Services.AddTransient<MySpotsViewModel>();
        builder.Services.AddTransient<SpotLocationViewModel>();
        // builder.Services.AddTransient<SpotCharacteristicsViewModel>();
        builder.Services.AddTransient<SpotPhotosViewModel>();
        builder.Services.AddTransient<UserProfileViewModel>();
        builder.Services.AddTransient<UserStatsViewModel>();
        // 🚫 MenuViewModel supprimé - utilisait des repositories
        // builder.Services.AddTransient<MenuViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.FlyoutMenuViewModel>();
        
        // Favorites ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Favorites.FavoriteSpotsViewModel>();
        
        // Authentication ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.LoginViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.SimpleLoginViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.RegistrationViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.EmailTestViewModel>();
        
        // ✅ Admin ViewModels - Supabase compatible
        builder.Services.AddTransient<SubExplore.ViewModels.Admin.SpotValidationViewModel>();
        // builder.Services.AddTransient<SubExplore.ViewModels.Admin.SpotDiagnosticViewModel>();
        
        // ✅ New specialized detail ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Organizations.OrganizationDetailsViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Businesses.BusinessDetailsViewModel>();
        
        // Navigation ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Common.NavigationBarViewModel>();
        
        // Settings ViewModels
        builder.Services.AddTransient<AboutViewModel>();
        
        // ✅ TEST VIEWMODEL POSTGIS
        builder.Services.AddTransient<SubExplore.ViewModels.Test.PostGisTestViewModel>();

        // Enregistrement des vues (Pages et Views)
        // 🚫 Pages supprimées - utilisaient Entity Framework
        // builder.Services.AddTransient<DatabaseTestPage>();
        builder.Services.AddTransient<MapPage>();
        // ✅ NOUVELLE PAGE AVANCÉE 100% SUPABASE
        builder.Services.AddTransient<EnhancedMapPage>();
        builder.Services.AddTransient<AddSpotPage>();
        builder.Services.AddTransient<SpotTypeSelectionPage>();
        builder.Services.AddTransient<OrganizationAddPage>();
        builder.Services.AddTransient<BusinessAddPage>();
        builder.Services.AddTransient<SpotDetailsPage>();
        builder.Services.AddTransient<SpotPhotosPage>();
        builder.Services.AddTransient<SpotEditPage>();
        builder.Services.AddTransient<MySpotsPage>();
        // 🚫 Vues supprimées - utilisaient Entity Framework
        // builder.Services.AddTransient<SpotLocationView>();
        // builder.Services.AddTransient<SpotCharacteristicsView>();
        builder.Services.AddTransient<SpotPhotosView>();
        builder.Services.AddTransient<UserProfilePage>();
        builder.Services.AddTransient<UserStatsPage>();
        
        // 🚫 Favorites Pages supprimées - utilisaient des ViewModels Entity Framework
        // builder.Services.AddTransient<SubExplore.Views.Favorites.FavoriteSpotsPage>();
        builder.Services.AddTransient<SubExplore.Views.Favorites.FavoritesPage>();
        
        // Authentication Pages
        builder.Services.AddTransient<SubExplore.Views.Auth.LoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.ProductionLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.DiagnosticLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.SimpleLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.WorkingLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.MinimalLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.UltraSimpleLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.DebugLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.BasicTestPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.CodeOnlyLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.CompleteLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.SimpleCompleteLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.CompleteRegistrationPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.RegistrationPage>();
        
        // ✅ Admin Pages - Supabase compatible
        builder.Services.AddTransient<SubExplore.Views.Admin.SpotValidationPage>();
        // builder.Services.AddTransient<SubExplore.Views.Admin.SpotDiagnosticPage>();
        
        // ✅ New specialized detail Pages
        builder.Services.AddTransient<SubExplore.Views.Organizations.OrganizationDetailsPage>();
        builder.Services.AddTransient<SubExplore.Views.Businesses.BusinessDetailsPage>();
        
        // Common Views
        builder.Services.AddTransient<SubExplore.Views.Common.NavigationBarView>();
        
        // Settings Pages
        builder.Services.AddTransient<AboutPage>();
        
        // ✅ TEST PAGE POSTGIS
        builder.Services.AddTransient<SubExplore.Views.Test.PostGisTestPage>();
        

#if DEBUG
        // Configure restrictive logging to reduce noise
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("Default", LogLevel.Information);
        
        // Only show errors and critical issues for SubExplore namespaces
        builder.Logging.AddFilter("SubExplore", LogLevel.Error);
        
        // Allow spot addition specific logs with our SPOT_ADD_ prefixes
        builder.Logging.AddFilter((provider, category, level) => 
        {
            // Always allow Error and Critical levels
            if (level >= LogLevel.Error) return true;
            
            // For SubExplore categories, only allow Warning and above
            if (category?.StartsWith("SubExplore") == true && level >= LogLevel.Warning) return true;
            
            // Block everything else
            return false;
        });
        
        builder.Logging.AddDebug();
#endif

        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            var exception = e.ExceptionObject as Exception;
            // Log fatal exception through proper logging when app is running
        };

        var app = builder.Build();

        // ✅ Initialize services including route registry
        Task.Run(async () =>
        {
            try
            {
                var serviceProvider = app.Services;
                var isHealthy = await serviceProvider.ValidateAndInitializeServicesAsync();
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Service initialization completed. Healthy: {isHealthy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Service initialization failed: {ex.Message}");
            }
        });

        return app;
    }

    /// <summary>
    /// Détection automatique de la chaîne de connexion selon la plateforme
    /// Compatible avec l'architecture hybride Entity Framework + Supabase
    /// </summary>
    private static string GetPlatformConnectionString(IConfiguration configuration)
    {
        string connectionString = string.Empty;

#if ANDROID
        if (DeviceInfo.DeviceType == DeviceType.Virtual)
        {
            // Émulateur Android - utilise l'IP de l'hôte
            connectionString = configuration.GetConnectionString("AndroidEmulator");
            System.Diagnostics.Debug.WriteLine("🤖 Android Emulator: Using AndroidEmulator connection string");
        }
        else
        {
            // Appareil Android physique
            connectionString = configuration.GetConnectionString("AndroidDevice");
            System.Diagnostics.Debug.WriteLine("📱 Android Device: Using AndroidDevice connection string");
        }
#elif IOS
        if (DeviceInfo.DeviceType == DeviceType.Virtual)
        {
            // Simulateur iOS
            connectionString = configuration.GetConnectionString("iOSSimulator");
            System.Diagnostics.Debug.WriteLine("📲 iOS Simulator: Using iOSSimulator connection string");
        }
        else
        {
            // Appareil iOS physique
            connectionString = configuration.GetConnectionString("iOSDevice");
            System.Diagnostics.Debug.WriteLine("📱 iOS Device: Using iOSDevice connection string");
        }
#elif WINDOWS
        // Windows Desktop
        connectionString = configuration.GetConnectionString("Windows");
        System.Diagnostics.Debug.WriteLine("🪟 Windows: Using Windows connection string");
#else
        // Plateforme par défaut
        connectionString = configuration.GetConnectionString("Default");
        System.Diagnostics.Debug.WriteLine("⚙️ Default Platform: Using Default connection string");
#endif

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fallback vers la connexion par défaut
            connectionString = configuration.GetConnectionString("Default");
            System.Diagnostics.Debug.WriteLine("⚠️ Fallback: Using Default connection string");
        }

        System.Diagnostics.Debug.WriteLine($"🔗 Final Connection String: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
        return connectionString ?? throw new InvalidOperationException("Aucune chaîne de connexion disponible pour la plateforme actuelle");
    }
}