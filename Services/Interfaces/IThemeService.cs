using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IThemeService
    {
        /// <summary>
        /// Gets the current theme setting
        /// </summary>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// Sets the application theme
        /// </summary>
        /// <param name="theme">Theme to apply (Light, Dark, or Unspecified for system)</param>
        Task SetThemeAsync(AppTheme theme);

        /// <summary>
        /// Event raised when theme changes
        /// </summary>
        event EventHandler<AppTheme> ThemeChanged;
    }
}