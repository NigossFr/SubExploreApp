using Microsoft.Extensions.DependencyInjection;

namespace SubExplore.Helpers;

/// <summary>
/// Helper class for accessing services from dependency injection container
/// </summary>
public static class ServiceHelper
{
    /// <summary>
    /// Gets a service of the specified type from the dependency injection container
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    public static T GetService<T>() where T : class
    {
        try
        {
            // Try to get the service from the current application's service provider
            if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider serviceProvider)
            {
                var service = serviceProvider.GetService<T>();
                if (service != null)
                    return service;
            }

            // If not available, try creating a default instance for certain types
            return CreateDefaultService<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceHelper] Error getting service {typeof(T).Name}: {ex.Message}");
            return CreateDefaultService<T>();
        }
    }

    /// <summary>
    /// Tries to get a service of the specified type from the dependency injection container
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance or null if not found</returns>
    public static T? TryGetService<T>() where T : class
    {
        try
        {
            if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider serviceProvider)
            {
                return serviceProvider.GetService<T>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceHelper] Error trying to get service {typeof(T).Name}: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Creates a default service instance for types that have default constructors
    /// </summary>
    /// <typeparam name="T">The type of service to create</typeparam>
    /// <returns>A default service instance</returns>
    private static T CreateDefaultService<T>() where T : class
    {
        try
        {
            // For INavigationService, create a simple implementation
            if (typeof(T).Name.Contains("INavigationService"))
            {
                return (T)(object)new DefaultNavigationService();
            }

            // Try to create using default constructor
            return Activator.CreateInstance<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceHelper] Error creating default service {typeof(T).Name}: {ex.Message}");
            throw new InvalidOperationException($"Unable to create service of type {typeof(T).Name}", ex);
        }
    }
}

/// <summary>
/// Simple default implementation of INavigationService for fallback scenarios
/// </summary>
internal class DefaultNavigationService : Services.Interfaces.INavigationService
{
    public async Task NavigateToAsync<TViewModel>(object parameter = null)
    {
        try
        {
            // Simple implementation that tries to navigate using Shell
            var route = GetRouteForViewModel<TViewModel>();
            if (!string.IsNullOrEmpty(route))
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Navigation error: {ex.Message}");
        }
    }

    public async Task NavigateToModalAsync<TViewModel>(object parameter = null)
    {
        try
        {
            var route = GetRouteForViewModel<TViewModel>();
            if (!string.IsNullOrEmpty(route))
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Navigate to modal error: {ex.Message}");
        }
    }

    public async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Go back error: {ex.Message}");
        }
    }

    public async Task CloseModalAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Close modal error: {ex.Message}");
        }
    }

    public async Task InitializeAsync<TViewModel>()
    {
        try
        {
            var route = GetRouteForViewModel<TViewModel>();
            if (!string.IsNullOrEmpty(route))
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Initialize error: {ex.Message}");
        }
    }

    public void SwitchToShellNavigation()
    {
        // Default implementation - no action needed for Shell navigation
        System.Diagnostics.Debug.WriteLine("[DefaultNavigationService] SwitchToShellNavigation called");
    }

    public async Task GoToHomeAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("///map");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Go to home error: {ex.Message}");
        }
    }

    public string GetCurrentNavigationPath()
    {
        try
        {
            return Shell.Current?.CurrentItem?.CurrentItem?.Route?.ToString() ?? "/";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Get current navigation path error: {ex.Message}");
            return "/";
        }
    }

    public int GetNavigationHistoryCount()
    {
        try
        {
            return Shell.Current?.Navigation?.NavigationStack?.Count ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Get navigation history count error: {ex.Message}");
            return 0;
        }
    }

    public void ClearNavigationHistory()
    {
        try
        {
            // Clear navigation stack is not directly supported in Shell, but we can log the action
            System.Diagnostics.Debug.WriteLine("[DefaultNavigationService] ClearNavigationHistory called");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DefaultNavigationService] Clear navigation history error: {ex.Message}");
        }
    }

    private string GetRouteForViewModel<TViewModel>()
    {
        var viewModelName = typeof(TViewModel).Name;
        
        // Simple mapping for common ViewModels
        return viewModelName switch
        {
            "MapViewModel" or "MapPageViewModel" => "///map",
            "MySpotsViewModel" or "MySpotsPageViewModel" => "///myspots",
            "FavoritesViewModel" or "FavoriteSpotsViewModel" => "///favorites",
            "UserProfileViewModel" or "ProfileViewModel" => "///userprofile",
            "UserPreferencesViewModel" or "PreferencesViewModel" => "///userpreferences",
            "UserStatsViewModel" or "StatsViewModel" => "///userstats",
            "SpotValidationViewModel" or "ValidationViewModel" => "///spotvalidation",
            _ => string.Empty
        };
    }
}