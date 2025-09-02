using Microsoft.Maui.Controls;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class ThemeService : IThemeService
    {
        public AppTheme CurrentTheme
        {
            get
            {
                var userTheme = Application.Current?.UserAppTheme;
                var requestedTheme = Application.Current?.RequestedTheme;
                var currentTheme = userTheme ?? requestedTheme ?? AppTheme.Unspecified;
                
                System.Diagnostics.Debug.WriteLine($"[ThemeService] CurrentTheme: UserAppTheme={userTheme}, RequestedTheme={requestedTheme}, Result={currentTheme}");
                return currentTheme;
            }
        }

        public event EventHandler<AppTheme>? ThemeChanged;

        public async Task SetThemeAsync(AppTheme theme)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] SetThemeAsync: Setting theme to {theme}");
                
                if (Application.Current != null)
                {
                    var oldUserTheme = Application.Current.UserAppTheme;
                    Application.Current.UserAppTheme = theme;
                    
                    System.Diagnostics.Debug.WriteLine($"[ThemeService] SetThemeAsync: Changed UserAppTheme from {oldUserTheme} to {Application.Current.UserAppTheme}");
                    
                    ThemeChanged?.Invoke(this, theme);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ThemeService] SetThemeAsync: Application.Current is null!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] SetThemeAsync error: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }
    }
}