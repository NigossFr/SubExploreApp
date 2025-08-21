// ========================================
// MauiProgram ALTERNATIF SANS ENUM MAPPING
// ========================================
// Cette version utilise des converters personnalisés au lieu 
// du mapping enum direct pour éviter les erreurs PostgreSQL

using Microsoft.Extensions.Logging;
// 🚫 Entity Framework et Npgsql supprimés - API Supabase uniquement
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
// 🚫 Repositories Entity Framework supprimés - API Supabase uniquement
using SubExplore.Services.Validation;
using SubExplore.Services.Caching;
using SubExplore.Models.Validation;
using SubExplore.ViewModels.Settings;
using SubExplore.ViewModels.Map;
using SubExplore.Constants;
using SubExplore.ViewModels.Spots;
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

        // 🚀 CONFIGURATION 100% API SUPABASE - PLUS D'ENTITY FRAMEWORK
        // L'application utilise UNIQUEMENT l'API Supabase via supabase-csharp
        Debug.WriteLine("🚀 Configuration 100% API Supabase - Plus d'Entity Framework");
        
        // Enregistrer le service client Supabase
        builder.Services.AddSingleton<ISupabaseClientService, SupabaseClientService>();

        // 🔐 SECURE CONFIGURATION SERVICE - Managed conditionally below

        // 🛡️ RESILIENCE SERVICES - Retry policies, circuit breakers, health monitoring
        builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
        builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
        // 🚫 ConnectionHealthService supprimé - utilisait Entity Framework
        builder.Services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();
        builder.Services.AddSingleton<IAutoReconnectService, AutoReconnectService>();
        builder.Services.AddSingleton<IFallbackDataService, FallbackDataService>();

        // 🚫 SupabaseService supprimé - utilisait Entity Framework
        
        // 🚀 SERVICES SUPABASE NATIFS - Solution 100% API
        builder.Services.AddScoped<ISupabaseApiService, SupabaseApiService>();
        builder.Services.AddSingleton<ISimpleSupabaseService, SimpleSupabaseService>();
        
        // ✅ NOUVEAUX SERVICES SUPABASE NATIFS
        builder.Services.AddScoped<ISupabaseSpotService, SupabaseSpotService>();
        builder.Services.AddScoped<ISupabaseSpotTypeService, SupabaseSpotTypeService>();
        builder.Services.AddScoped<ISupabaseUserService, SupabaseUserService>();
        
        // 🔐 SERVICE D'AUTHENTIFICATION AVANCÉ
        builder.Services.AddSingleton<IEnhancedAuthenticationService, EnhancedAuthenticationService>();

        Debug.WriteLine("✅ Services Supabase natifs configurés");
        
        // ✅ SERVICE D'INITIALISATION DE L'APPLICATION
        builder.Services.AddSingleton<IAppInitializationService, AppInitializationService>();
        
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
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
        // 🚫 UserProfileService supprimé - utilisait Entity Framework
        // 🚫 Services supprimés - utilisaient des repositories
        // builder.Services.AddScoped<ISpotService, SpotService>();
        // builder.Services.AddScoped<IFavoriteSpotService, FavoriteSpotService>();
        builder.Services.AddSingleton<IFavoriteSpotCacheService, FavoriteSpotCacheService>();
        // 🚫 ErrorHandlingService supprimé - utilisait Entity Framework
        
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
#if DEBUG
        // ✅ Supabase configuré avec les vraies clés API (Janvier 2025)
        builder.Services.AddSingleton<ISupabaseConfigurationService, DevelopmentConfigurationService>();
        System.Diagnostics.Debug.WriteLine("[DEBUG] Using DevelopmentConfigurationService - SUPABASE RÉEL ACTIVÉ");
        // Mode offline disponible avec: OfflineTestConfigurationService
#else
        builder.Services.AddSingleton<ISupabaseConfigurationService, SupabaseConfigurationService>();
        System.Diagnostics.Debug.WriteLine("[RELEASE] Using SupabaseConfigurationService for Supabase configuration");
#endif
        builder.Services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();
        // 🚫 TokenService supprimé - utilisait des repositories
        // builder.Services.AddScoped<ITokenService, TokenService>();
        
        // 🔐 AUTHENTIFICATION 100% API SUPABASE
        builder.Services.AddSingleton<ISimpleAuthenticationService, SimpleAuthenticationService>();
        // 🚫 IAuthenticationService trop complexe - modifier LoginViewModel pour utiliser ISimpleAuthenticationService
        builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
        
        // Email services
        builder.Services.AddSingleton<IEmailService, EmailService>();
        // 🚫 Services email supprimés - utilisaient des repositories
        // builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        // builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
        
        // 🚫 Validation services supprimés - utilisaient Entity Framework
        // builder.Services.AddScoped<ISpotValidationService, SpotValidationService>();
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
        builder.Services.AddLogging(configure => configure.AddDebug());
        
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
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<OptimizedMapViewModel>();
        // ✅ NOUVEAU VIEWMODEL AVANCÉ 100% SUPABASE
        builder.Services.AddTransient<EnhancedMapViewModel>();
        // 🚫 ViewModels supprimés - utilisaient des repositories
        // builder.Services.AddTransient<SpotManagementViewModel>();
        // builder.Services.AddTransient<AddSpotViewModel>();
        // builder.Services.AddTransient<SpotDetailsViewModel>();
        // builder.Services.AddTransient<MySpotsViewModel>();
        builder.Services.AddTransient<SpotLocationViewModel>();
        // builder.Services.AddTransient<SpotCharacteristicsViewModel>();
        builder.Services.AddTransient<SpotPhotosViewModel>();
        builder.Services.AddTransient<UserProfileViewModel>();
        builder.Services.AddTransient<UserStatsViewModel>();
        // 🚫 MenuViewModel supprimé - utilisait des repositories
        // builder.Services.AddTransient<MenuViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.FlyoutMenuViewModel>();
        
        // 🚫 Favorites ViewModels supprimés - utilisaient des repositories
        // builder.Services.AddTransient<SubExplore.ViewModels.Favorites.FavoriteSpotsViewModel>();
        
        // Authentication ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.LoginViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.SimpleLoginViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.RegistrationViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.EmailTestViewModel>();
        
        // 🚫 Admin ViewModels supprimés - utilisaient des repositories
        // builder.Services.AddTransient<SubExplore.ViewModels.Admin.SpotValidationViewModel>();
        // builder.Services.AddTransient<SubExplore.ViewModels.Admin.SpotDiagnosticViewModel>();
        
        // Navigation ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Common.NavigationBarViewModel>();
        
        // Settings ViewModels
        builder.Services.AddTransient<AboutViewModel>();

        // Enregistrement des vues (Pages et Views)
        // 🚫 Pages supprimées - utilisaient Entity Framework
        // builder.Services.AddTransient<DatabaseTestPage>();
        builder.Services.AddTransient<MapPage>();
        // ✅ NOUVELLE PAGE AVANCÉE 100% SUPABASE
        builder.Services.AddTransient<EnhancedMapPage>();
        // builder.Services.AddTransient<AddSpotPage>();
        // builder.Services.AddTransient<SpotDetailsPage>();
        // builder.Services.AddTransient<MySpotsPage>();
        // 🚫 Vues supprimées - utilisaient Entity Framework
        // builder.Services.AddTransient<SpotLocationView>();
        // builder.Services.AddTransient<SpotCharacteristicsView>();
        builder.Services.AddTransient<SpotPhotosView>();
        builder.Services.AddTransient<UserProfilePage>();
        builder.Services.AddTransient<UserStatsPage>();
        
        // 🚫 Favorites Pages supprimées - utilisaient des ViewModels Entity Framework
        // builder.Services.AddTransient<SubExplore.Views.Favorites.FavoriteSpotsPage>();
        
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
        
        // 🚫 Admin Pages supprimées - utilisaient Entity Framework
        // builder.Services.AddTransient<SubExplore.Views.Admin.SpotValidationPage>();
        // builder.Services.AddTransient<SubExplore.Views.Admin.SpotDiagnosticPage>();
        
        // Common Views
        builder.Services.AddTransient<SubExplore.Views.Common.NavigationBarView>();
        
        // Settings Pages
        builder.Services.AddTransient<AboutPage>();
        

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            var exception = e.ExceptionObject as Exception;
            // Log fatal exception through proper logging when app is running
        };

        return builder.Build();
    }
}