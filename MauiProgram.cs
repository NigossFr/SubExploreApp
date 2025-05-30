﻿using Microsoft.Extensions.Configuration;
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
using SubExplore.ViewModels.Spot;
using SubExplore.Views.Spot.Components;
using SubExplore.Views.Settings;
using SubExplore.Views.Map;
using SubExplore.Views.Spot;
using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices; // Ajouté pour DeviceInfo
using System.Diagnostics; // Ajouté pour Debug.WriteLine

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

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
            // Pour le développement, lancer une exception est souvent préférable pour ne pas masquer un problème.
            Debug.WriteLine($"Erreur : Impossible de charger le fichier de configuration '{appSettingsResourceName}'.");
            // Créer une configuration vide pour éviter les NullReferenceException plus tard, ou lancer une exception
            configuration = new ConfigurationBuilder().Build();
            // throw new FileNotFoundException($"Le fichier de configuration '{appSettingsResourceName}' est introuvable en tant que ressource incorporée.");
        }

        // Rendre IConfiguration disponible via DI
        builder.Services.AddSingleton(configuration);

        // Configuration de la base de données
        builder.Services.AddDbContext<SubExploreDbContext>(options =>
        {
            string? connectionString = null;
            string connectionStringKey = "DefaultConnection"; // Clé par défaut

#if ANDROID
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
            {
                Debug.WriteLine("Détection : Émulateur Android.");
                connectionStringKey = "AndroidEmulatorConnection";
            }
            else
            {
                Debug.WriteLine("Détection : Appareil Android physique.");
                connectionStringKey = "AndroidDeviceConnection";
            }
#elif IOS
            if (DeviceInfo.Current.DeviceType == DeviceType.Virtual) // Simulateur iOS
            {
                Debug.WriteLine("Détection : Simulateur iOS.");
                connectionStringKey = "iOSSimulatorConnection";
            }
            else // Appareil iOS réel
            {
                Debug.WriteLine("Détection : Appareil iOS physique.");
                connectionStringKey = "iOSDeviceConnection";
            }
#elif WINDOWS
            Debug.WriteLine("Détection : Plateforme Windows.");
            connectionStringKey = "DefaultConnection"; // Ou une clé spécifique pour Windows si nécessaire
#else
            // Pour d'autres plateformes ou si aucune directive n'est active
            Debug.WriteLine("Détection : Plateforme inconnue ou non gérée, utilisation de DefaultConnection.");
#endif

            connectionString = configuration.GetConnectionString(connectionStringKey);
            Debug.WriteLine($"Clé de chaîne de connexion sélectionnée : {connectionStringKey}");

            // Fallback au DefaultConnection si la chaîne spécifique n'est pas trouvée
            if (string.IsNullOrEmpty(connectionString))
            {
                Debug.WriteLine($"Avertissement : '{connectionStringKey}' introuvable, tentative avec 'DefaultConnection'.");
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("La chaîne de connexion à la base de données n'est pas configurée ou introuvable.");
            }

            Debug.WriteLine($"Chaîne de connexion utilisée : {connectionString}"); // Attention, ne pas logger en production si elle contient des mots de passe !

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // Enregistrement des repositories
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<ISpotRepository, SpotRepository>();
        builder.Services.AddScoped<ISpotTypeRepository, SpotTypeRepository>();
        builder.Services.AddScoped<ISpotMediaRepository, SpotMediaRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        // Enregistrement des services
        builder.Services.AddScoped<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();

        // Enregistrement des ViewModels
        // Pour les ViewModels, AddTransient est souvent un bon choix, mais AddScoped peut aussi être pertinent
        // si le ViewModel est lié à la durée de vie d'une page et que vous utilisez la navigation avec DI.
        builder.Services.AddTransient<DatabaseTestViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<AddSpotViewModel>();
        builder.Services.AddTransient<SpotDetailsViewModel>();
        builder.Services.AddTransient<SpotLocationViewModel>();
        builder.Services.AddTransient<SpotCharacteristicsViewModel>();
        builder.Services.AddTransient<SpotPhotosViewModel>();

        // Enregistrement des vues (Pages et Views)
        // Pour les Pages et Views, AddTransient est généralement correct.
        builder.Services.AddTransient<DatabaseTestPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<AddSpotPage>();
        builder.Services.AddTransient<SpotDetailsPage>();
        builder.Services.AddTransient<SpotLocationView>(); // Si c'est une ContentView, c'est bien
        builder.Services.AddTransient<SpotCharacteristicsView>(); // Idem
        builder.Services.AddTransient<SpotPhotosView>(); // Idem

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}