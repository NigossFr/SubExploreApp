using Microsoft.Maui.Controls;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    public class ThemeService : IThemeService
    {
        public AppTheme CurrentTheme => Application.Current?.RequestedTheme ?? AppTheme.Unspecified;

        public event EventHandler<AppTheme>? ThemeChanged;

        public async Task SetThemeAsync(AppTheme theme)
        {
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = theme;
                    ThemeChanged?.Invoke(this, theme);
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