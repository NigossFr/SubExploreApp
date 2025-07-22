using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess; // Pour SubExploreDbContext
using SubExplore.Repositories.Interfaces;
using SubExplore.Repositories.Implementations;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Settings;
using SubExplore.ViewModels.Map;
using SubExplore.Constants;
using SubExplore.ViewModels.Spots;
using SubExplore.ViewModels.Profile;
using SubExplore.ViewModels.Menu;
using SubExplore.ViewModels.Auth;
using SubExplore.Views.Spots.Components;
using SubExplore.Views.Settings;
using SubExplore.Views.Map;
using SubExplore.Views.Spots;
using SubExplore.Views.Profile;
using SubExplore.Views.Auth;
using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices; // Ajouté pour DeviceInfo
using DotNetEnv; // Pour charger les variables d'environnement

namespace SubExplore;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Load environment variables from .env file
        try
        {
            Env.Load();
            // Environment variables loaded successfully
        }
        catch (Exception ex)
        {
            // Continue without .env file - environment variables can still be set manually
            // Continue without .env file - environment variables can still be set manually
        }

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                // Temporarily commented out to fix font loading issues
                // fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                // fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID
        // Additional Android-specific Google Maps configuration will be handled by the platform-specific code
#endif

        // Charger la configuration depuis appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        var appSettingsResourceName = "SubExplore.appsettings.json"; // Assurez-vous que cela correspond
        using var stream = assembly.GetManifestResourceStream(appSettingsResourceName);

        IConfiguration configuration;

        if (stream != null)
        {
            configuration = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            // Ajouter la configuration au builder pour qu'elle soit accessible via builder.Configuration ailleurs si besoin
            builder.Configuration.AddConfiguration(configuration);
        }
        else
        {
            // Gestion de secours - peut-être lancer une exception ou utiliser des valeurs par défaut codées en dur.
            // For development, throwing an exception is often preferable to not hide a problem.
            // Configuration file could not be loaded - using empty configuration
            // Créer une configuration vide pour éviter les NullReferenceException plus tard, ou lancer une exception
            configuration = new ConfigurationBuilder().Build();
            // throw new FileNotFoundException($"Le fichier de configuration '{appSettingsResourceName}' est introuvable en tant que ressource incorporée.");
        }

        // Rendre IConfiguration disponible via DI
        builder.Services.AddSingleton(configuration);

        // Register the performance interceptor factory
        builder.Services.AddSingleton<DataAccess.PerformanceInterceptor>();

        // Configuration de la base de données
        builder.Services.AddDbContext<SubExploreDbContext>((serviceProvider, options) =>
        {
            string? connectionString = null;
            string connectionStringKey = "DefaultConnection"; // Clé par défaut

#if ANDROID
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
            {
                // Detection: Android Emulator
                connectionStringKey = "AndroidEmulatorConnection";
            }
            else
            {
                // Detection: Physical Android Device
                connectionStringKey = "AndroidDeviceConnection";
            }
#elif IOS
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual) // Simulateur iOS
            {
                // Detection: iOS Simulator
                connectionStringKey = "iOSSimulatorConnection";
            }
            else // Appareil iOS réel
            {
                // Detection: Physical iOS Device
                connectionStringKey = "iOSDeviceConnection";
            }
#elif WINDOWS
            // Detection: Windows Platform
            connectionStringKey = "DefaultConnection"; // Ou une clé spécifique pour Windows si nécessaire
#else
            // Pour d'autres plateformes ou si aucune directive n'est active
            // Detection: Unknown or unhandled platform, using DefaultConnection
#endif

            connectionString = configuration.GetConnectionString(connectionStringKey);
            // Connection string key selected: {connectionStringKey}

            // Fallback au DefaultConnection si la chaîne spécifique n'est pas trouvée
            if (string.IsNullOrEmpty(connectionString))
            {
                // Warning: '{connectionStringKey}' not found, attempting with 'DefaultConnection'
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                var errorMessage = "La chaîne de connexion à la base de données n'est pas configurée ou introuvable.";
                // Critical: Database connection string not configured
                throw new InvalidOperationException(errorMessage);
            }

            // Connection string configured successfully (password masked for security)

            try
            {
                // Test de la connexion avant de continuer
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                
                // Add performance monitoring interceptor
                var performanceInterceptor = serviceProvider.GetRequiredService<DataAccess.PerformanceInterceptor>();
                options.AddInterceptors(performanceInterceptor);
            }
            catch (Exception ex)
            {
                // Database connection test failed during configuration
                throw new InvalidOperationException($"Database connection failed: {ex.Message}", ex);
            }
        });

        // Enregistrement des repositories
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<ISpotRepository, SpotRepository>();
        builder.Services.AddScoped<ISpotTypeRepository, SpotTypeRepository>();
        builder.Services.AddScoped<ISpotMediaRepository, SpotMediaRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();

        // Enregistrement des services
        builder.Services.AddScoped<IDatabaseService, DatabaseServiceSimple>();
        builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IImageCacheService, ImageCacheService>();
        builder.Services.AddSingleton<IMapDiagnosticService, MapDiagnosticService>();
        builder.Services.AddSingleton<IPlatformMapService, PlatformMapService>();
        builder.Services.AddSingleton<IMenuService, MenuService>();
        builder.Services.AddScoped<IUserProfileService, UserProfileService>();
        builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
        
        // Performance monitoring services
        builder.Services.AddSingleton<IPerformanceProfilingService, PerformanceProfilingService>();
        builder.Services.AddSingleton<IApplicationPerformanceService, ApplicationPerformanceService>();
        builder.Services.AddScoped<IPerformanceValidationService, PerformanceValidationService>();
        
        // Authentication services
        builder.Services.AddSingleton<ISecureSettingsService>(provider =>
        {
            var baseSettings = provider.GetRequiredService<ISettingsService>();
            return new SecureSettingsService(baseSettings);
        });
        builder.Services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        // Configure logging
        builder.Services.AddLogging(configure => configure.AddDebug());
        
        // Add HttpClient for image caching
        builder.Services.AddHttpClient<ImageCacheService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SubExplore/1.0");
        });

        // Enregistrement des ViewModels
        // Pour les ViewModels, AddTransient est souvent un bon choix, mais AddScoped peut aussi être pertinent
        // si le ViewModel est lié à la durée de vie d'une page et que vous utilisez la navigation avec DI.
        builder.Services.AddTransient<DatabaseTestViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<SpotManagementViewModel>();
        builder.Services.AddTransient<AddSpotViewModel>();
        builder.Services.AddTransient<SpotDetailsViewModel>();
        builder.Services.AddTransient<SpotLocationViewModel>();
        builder.Services.AddTransient<SpotCharacteristicsViewModel>();
        builder.Services.AddTransient<SpotPhotosViewModel>();
        builder.Services.AddTransient<UserProfileViewModel>();
        builder.Services.AddTransient<UserPreferencesViewModel>();
        builder.Services.AddTransient<UserStatsViewModel>();
        builder.Services.AddTransient<MenuViewModel>();
        
        // Authentication ViewModels
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.LoginViewModel>();
        builder.Services.AddTransient<SubExplore.ViewModels.Auth.RegistrationViewModel>();

        // Enregistrement des vues (Pages et Views)
        // Pour les Pages et Views, AddTransient est généralement correct.
        builder.Services.AddTransient<DatabaseTestPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<AddSpotPage>();
        builder.Services.AddTransient<SpotDetailsPage>();
        builder.Services.AddTransient<SpotLocationView>(); // Si c'est une ContentView, c'est bien
        builder.Services.AddTransient<SpotCharacteristicsView>(); // Idem
        builder.Services.AddTransient<SpotPhotosView>(); // Idem
        builder.Services.AddTransient<UserProfilePage>();
        builder.Services.AddTransient<UserPreferencesPage>();
        builder.Services.AddTransient<UserStatsPage>();
        
        // Authentication Pages
        builder.Services.AddTransient<SubExplore.Views.Auth.LoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.DiagnosticLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.SimpleLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.WorkingLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.MinimalLoginPage>();
        builder.Services.AddTransient<SubExplore.Views.Auth.RegistrationPage>();
        
        // Test Pages
        builder.Services.AddTransient<SubExplore.Views.TestPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            var exception = e.ExceptionObject as Exception;
            // Log fatal exception through proper logging when app is running
            // For now, system will handle the exception appropriately
        };

        return builder.Build();
    }
}