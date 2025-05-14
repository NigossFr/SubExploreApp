using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
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

namespace SubExplore;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps() // Ajout du support des cartes
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Chargement de la configuration
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SubExplore.appsettings.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.Configuration.AddConfiguration(configuration);

        // Configuration de la base de données
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<SubExploreDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
        builder.Services.AddTransient<DatabaseTestViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<AddSpotViewModel>();
        builder.Services.AddTransient<SpotDetailsViewModel>();
        builder.Services.AddTransient<SpotLocationViewModel>();
        builder.Services.AddTransient<SpotCharacteristicsViewModel>();
        builder.Services.AddTransient<SpotPhotosViewModel>();

        // Enregistrement des vues
        builder.Services.AddTransient<DatabaseTestPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<AddSpotPage>();
        builder.Services.AddTransient<SpotDetailsPage>();
        builder.Services.AddTransient<SpotLocationView>();
        builder.Services.AddTransient<SpotCharacteristicsView>();
        builder.Services.AddTransient<SpotPhotosView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}