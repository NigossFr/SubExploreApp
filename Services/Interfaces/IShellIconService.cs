using Microsoft.Maui.Controls;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing Shell flyout icon visibility across platforms
    /// </summary>
    public interface IShellIconService
    {
        /// <summary>
        /// Configure the Shell flyout icon with platform-appropriate settings
        /// </summary>
        /// <param name="shell">The Shell instance to configure</param>
        void ConfigureFlyoutIcon(Shell shell);

        /// <summary>
        /// Validate that the flyout icon is properly configured
        /// </summary>
        /// <param name="shell">The Shell instance to validate</param>
        /// <returns>True if icon is properly configured</returns>
        bool ValidateFlyoutIcon(Shell shell);

        /// <summary>
        /// Get the appropriate icon source for the current platform
        /// </summary>
        /// <param name="shell">Optional Shell instance for context-aware icon generation</param>
        /// <returns>Platform-appropriate ImageSource</returns>
        ImageSource GetPlatformIconSource(Shell shell = null);
        
        /// <summary>
        /// Force icon visibility with aggressive anti-interference measures
        /// </summary>
        /// <param name="shell">The Shell instance to force visibility on</param>
        void ForceIconVisibilityRefresh(Shell shell);
    }
}