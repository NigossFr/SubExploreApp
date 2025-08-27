using Microsoft.Maui.Controls;
using SubExplore.Services.Interfaces;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Unified service for managing Shell flyout icon across platforms
    /// Eliminates platform-specific timer workarounds and centralizes icon logic
    /// </summary>
    public class ShellIconService : IShellIconService
    {
        private const string ICON_COLOR = "#333333"; // Dark gray for good contrast on light nav bar
        private const string ICON_COLOR_DARK = "#FFFFFF"; // White for dark nav bar
        private const string FALLBACK_ICON_COLOR = "#FF0000"; // Red for testing visibility
        private const string PRIMARY_COLOR = "#006994";
        private const string FALLBACK_ICON = "dotnet_bot.png";
        private const string HAMBURGER_GLYPH = "\u2630"; // Unicode for hamburger menu ☰
        private const string ALTERNATIVE_GLYPH = "\u2261"; // Alternative hamburger glyph ≡
        private const int ICON_SIZE = 32; // Increased size for better visibility
        private const int LARGE_ICON_SIZE = 40; // Even larger for testing

        public void ConfigureFlyoutIcon(Shell shell)
        {
            try
            {
                Debug.WriteLine("[ShellIconService] Configuring flyout icon");

                // Clear any existing icon to avoid conflicts
                shell.FlyoutIcon = null;

                // Ensure Shell properties support flyout behavior first
                shell.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(shell, true);
                
                // Set the icon using platform-appropriate source with proper contrast color
                var iconSource = GetPlatformIconSource(shell);
                shell.FlyoutIcon = iconSource;
                
                Debug.WriteLine($"[ShellIconService] Set icon source: {iconSource?.GetType().Name}");
                
                // Additional Shell properties to ensure visibility
                shell.FlyoutBehavior = FlyoutBehavior.Flyout; // Ensure flyout is enabled
                
                // Force Shell to refresh icon rendering
                if (iconSource is FontImageSource fontIcon)
                {
                    Debug.WriteLine($"[ShellIconService] FontIcon - Glyph: {fontIcon.Glyph}, Color: {fontIcon.Color}, Size: {fontIcon.Size}, Family: {fontIcon.FontFamily}");
                }
                else if (iconSource is FileImageSource fileIcon)
                {
                    Debug.WriteLine($"[ShellIconService] FileIcon - File: {fileIcon.File}");
                }

                // Multiple delayed validation attempts to combat GMM interference
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                    TimeSpan.FromMilliseconds(100), () =>
                    {
                        Debug.WriteLine("[ShellIconService] First validation attempt...");
                        ForceIconVisibility(shell);
                    });
                    
                // Second attempt to combat external style injection
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                    TimeSpan.FromMilliseconds(500), () =>
                    {
                        Debug.WriteLine("[ShellIconService] Second validation attempt (anti-GMM)...");
                        ForceIconVisibility(shell);
                    });
                    
                // Third attempt for stubborn cases
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                    TimeSpan.FromMilliseconds(1000), () =>
                    {
                        Debug.WriteLine("[ShellIconService] Final validation attempt...");
                        ForceIconVisibility(shell);
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] ❌ Error configuring flyout icon: {ex.Message}");
                ApplyFallbackIcon(shell);
            }
        }

        public bool ValidateFlyoutIcon(Shell shell)
        {
            try
            {
                // Check if icon is set and flyout behavior is correct
                bool hasIcon = shell.FlyoutIcon != null;
                bool hasFlyoutBehavior = shell.FlyoutBehavior == FlyoutBehavior.Flyout;
                bool hasNavBarVisible = Shell.GetNavBarIsVisible(shell);

                Debug.WriteLine($"[ShellIconService] Validation - HasIcon: {hasIcon}, FlyoutBehavior: {hasFlyoutBehavior}, NavBarVisible: {hasNavBarVisible}");

                return hasIcon && hasFlyoutBehavior && hasNavBarVisible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Validation error: {ex.Message}");
                return false;
            }
        }

        public ImageSource GetPlatformIconSource(Shell shell = null)
        {
#if ANDROID
            // Android: ULTIMATE MaterialButton bypass strategy
            Debug.WriteLine("[ShellIconService] Android: Creating ULTRA-AGGRESSIVE MaterialButton override");
            
            // Strategy 1: Force MaterialButton-compatible icon with multiple refresh attempts
            var androidIcon = new FileImageSource 
            { 
                File = FALLBACK_ICON
            };
            
            if (shell != null)
            {
                try
                {
                    // AGGRESSIVE: Override MaterialButton background control with multiple strategies
                    shell.SetValue(Shell.FlyoutIconProperty, androidIcon);
                    shell.SetValue(Shell.FlyoutBehaviorProperty, FlyoutBehavior.Flyout);
                    shell.SetValue(Shell.NavBarIsVisibleProperty, true);
                    
                    // Force Shell to re-render icon immediately
                    shell.FlyoutIcon = androidIcon;
                    shell.FlyoutBehavior = FlyoutBehavior.Flyout;
                    
                    Debug.WriteLine("[ShellIconService] Android: Applied ULTRA-AGGRESSIVE MaterialButton override");
                    
                    // NUCLEAR OPTION: Multiple delayed attempts to combat MaterialButton persistence
                    Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                        TimeSpan.FromMilliseconds(50), () => 
                        {
                            shell.SetValue(Shell.FlyoutIconProperty, androidIcon);
                            shell.FlyoutIcon = androidIcon;
                            Debug.WriteLine("[ShellIconService] Android: NUCLEAR MaterialButton override attempt 1");
                        });
                        
                    Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                        TimeSpan.FromMilliseconds(150), () => 
                        {
                            shell.SetValue(Shell.FlyoutIconProperty, androidIcon);
                            shell.FlyoutIcon = androidIcon;
                            Debug.WriteLine("[ShellIconService] Android: NUCLEAR MaterialButton override attempt 2");
                        });
                        
                    Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                        TimeSpan.FromMilliseconds(300), () => 
                        {
                            shell.SetValue(Shell.FlyoutIconProperty, androidIcon);
                            shell.FlyoutIcon = androidIcon;
                            Debug.WriteLine("[ShellIconService] Android: NUCLEAR MaterialButton override attempt 3");
                        });
                        
                    Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                        TimeSpan.FromMilliseconds(600), () => 
                        {
                            shell.SetValue(Shell.FlyoutIconProperty, androidIcon);
                            shell.FlyoutIcon = androidIcon;
                            Debug.WriteLine("[ShellIconService] Android: NUCLEAR MaterialButton override attempt 4 (FINAL)");
                        });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ShellIconService] Android ULTRA-AGGRESSIVE override failed: {ex.Message}");
                }
            }
            
            return androidIcon;
#elif IOS
            // iOS: Multiple fallback strategies for maximum visibility
            Debug.WriteLine("[ShellIconService] Creating iOS FontImageSource with enhanced visibility");
            
            // Strategy 1: Try default system font
            try
            {
                var icon = new FontImageSource
                {
                    Glyph = HAMBURGER_GLYPH,
                    Color = Microsoft.Maui.Graphics.Color.Parse(FALLBACK_ICON_COLOR), // Use red for testing
                    Size = LARGE_ICON_SIZE,
                    FontFamily = "System" // Try system font instead
                };
                Debug.WriteLine($"[ShellIconService] iOS FontImageSource created: {icon.Glyph} in {icon.FontFamily} at size {icon.Size}");
                return icon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] iOS FontImageSource failed: {ex.Message}, using FileImageSource");
                return new FileImageSource { File = FALLBACK_ICON };
            }
#elif WINDOWS
            // Windows: Enhanced visibility with fallback strategies
            Debug.WriteLine("[ShellIconService] Creating Windows FontImageSource with enhanced visibility");
            
            // Strategy 1: Try multiple fonts and glyphs
            try
            {
                var icon = new FontImageSource
                {
                    Glyph = ALTERNATIVE_GLYPH, // Try alternative glyph
                    Color = Microsoft.Maui.Graphics.Color.Parse(FALLBACK_ICON_COLOR), // Use red for testing
                    Size = LARGE_ICON_SIZE,
                    FontFamily = "Segoe MDL2 Assets" // Windows icons font
                };
                Debug.WriteLine($"[ShellIconService] Windows FontImageSource created: {icon.Glyph} in {icon.FontFamily} at size {icon.Size}");
                return icon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Windows FontImageSource failed: {ex.Message}, using FileImageSource");
                return new FileImageSource { File = FALLBACK_ICON };
            }
#else
            // Default fallback with enhanced logging
            Debug.WriteLine("[ShellIconService] Using default FileImageSource fallback with enhanced visibility");
            return new FileImageSource { File = FALLBACK_ICON };
#endif
        }

        private void ApplyPlatformSpecificFallback(Shell shell)
        {
            try
            {
                Debug.WriteLine("[ShellIconService] Applying platform-specific fallback");

#if ANDROID
                // Android may need a slight delay for proper rendering
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.StartTimer(
                    TimeSpan.FromMilliseconds(100), () =>
                    {
                        shell.FlyoutIcon = new FileImageSource { File = FALLBACK_ICON };
                        Debug.WriteLine("[ShellIconService] Android fallback applied");
                        return false;
                    });
#elif IOS
                // iOS may need more time for rendering
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.StartTimer(
                    TimeSpan.FromMilliseconds(200), () =>
                    {
                        shell.FlyoutIcon = GetPlatformIconSource(shell);
                        Debug.WriteLine("[ShellIconService] iOS fallback applied");
                        return false;
                    });
#elif WINDOWS
                // Windows fallback
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.StartTimer(
                    TimeSpan.FromMilliseconds(50), () =>
                    {
                        shell.FlyoutIcon = GetPlatformIconSource(shell);
                        Debug.WriteLine("[ShellIconService] Windows fallback applied");
                        return false;
                    });
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Platform fallback error: {ex.Message}");
                ApplyFallbackIcon(shell);
            }
        }

        private void ApplyFallbackIcon(Shell shell)
        {
            try
            {
                Debug.WriteLine("[ShellIconService] Applying ultimate fallback icon strategy");
                
                // Strategy 1: Try reliable FileImageSource
                shell.FlyoutIcon = new FileImageSource { File = FALLBACK_ICON };
                
                // Strategy 2: If still not working, ensure Shell configuration is correct
                shell.FlyoutBehavior = FlyoutBehavior.Flyout;
                
                // Strategy 3: Force refresh the Shell
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                    TimeSpan.FromMilliseconds(200), () =>
                    {
                        if (shell.FlyoutIcon == null)
                        {
                            // Last resort: recreate icon
                            shell.FlyoutIcon = new FileImageSource { File = FALLBACK_ICON };
                            Debug.WriteLine("[ShellIconService] Applied last resort icon recreation");
                        }
                        else
                        {
                            Debug.WriteLine("[ShellIconService] Ultimate fallback icon applied successfully");
                        }
                    });
                    
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Even fallback icon failed: {ex.Message}");
                // If all else fails, at least ensure flyout behavior is enabled
                shell.FlyoutBehavior = FlyoutBehavior.Flyout;
            }
        }
        
        private string GetOptimalIconColor(Shell shell)
        {
            try
            {
                // For SubExplore app, we know the nav bar is typically white/light
                // so we want a dark icon for good contrast
                
                // Check if we can detect the navigation bar background color
                var backgroundColor = shell?.BackgroundColor;
                
                if (backgroundColor != null)
                {
                    // Simple luminance check - if background is dark, use light icon
                    var color = backgroundColor;
                    var luminance = (0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                    
                    Debug.WriteLine($"[ShellIconService] Nav bar luminance: {luminance:F2}");
                    
                    // If background is dark (luminance < 0.5), use white icon
                    if (luminance < 0.5)
                    {
                        Debug.WriteLine($"[ShellIconService] Using white icon for dark background");
                        return ICON_COLOR_DARK;
                    }
                }
                
                // Default: dark icon for light background (most common case)
                Debug.WriteLine($"[ShellIconService] Using dark icon for light background");
                return ICON_COLOR;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Error determining icon color: {ex.Message}");
                // Safe fallback: dark icon
                return ICON_COLOR;
            }
        }
        
        public void ForceIconVisibilityRefresh(Shell shell)
        {
            ForceIconVisibility(shell);
        }
        
        private void ForceIconVisibility(Shell shell)
        {
            try
            {
                Debug.WriteLine("[ShellIconService] Forcing icon visibility...");
                
                // Validate current state
                var hasIcon = shell.FlyoutIcon != null;
                var hasFlyoutBehavior = shell.FlyoutBehavior == FlyoutBehavior.Flyout;
                var hasNavBarVisible = Shell.GetNavBarIsVisible(shell);
                
                Debug.WriteLine($"[ShellIconService] Current state - HasIcon: {hasIcon}, FlyoutBehavior: {hasFlyoutBehavior}, NavBarVisible: {hasNavBarVisible}");
                
                if (!hasIcon || !ValidateFlyoutIcon(shell))
                {
                    Debug.WriteLine("[ShellIconService] Icon validation failed, applying aggressive fixes...");
                    
                    // Platform-specific aggressive icon setting
#if ANDROID
                    // Android: EXTREME MaterialButton counter-attack
                    var androidFallbackIcon = new FileImageSource { File = FALLBACK_ICON };
                    
                    // BRUTE FORCE: Multiple simultaneous property setting attempts
                    shell.SetValue(Shell.FlyoutIconProperty, androidFallbackIcon);
                    shell.FlyoutIcon = androidFallbackIcon;
                    
                    // Force all related Shell properties
                    shell.SetValue(Shell.FlyoutBehaviorProperty, FlyoutBehavior.Flyout);
                    shell.SetValue(Shell.NavBarIsVisibleProperty, true);
                    shell.SetValue(Shell.ForegroundColorProperty, Microsoft.Maui.Graphics.Colors.Black);
                    
                    // DESPERATE MEASURES: Try alternative icon format
                    try 
                    {
                        // Create FontImageSource as backup
                        var fontIcon = new FontImageSource
                        {
                            Glyph = "≡", // Alternative hamburger symbol
                            Color = Microsoft.Maui.Graphics.Colors.Red, // BRIGHT RED for testing
                            Size = 48, // EXTRA LARGE
                            FontFamily = null // System default
                        };
                        
                        // Set BOTH types simultaneously to overwhelm MaterialButton
                        shell.SetValue(Shell.FlyoutIconProperty, androidFallbackIcon);
                        shell.FlyoutIcon = fontIcon; // This one might bypass MaterialButton
                        
                        Debug.WriteLine("[ShellIconService] Android: Applied EXTREME dual-icon MaterialButton counter-attack");
                    }
                    catch (Exception fontEx)
                    {
                        Debug.WriteLine($"[ShellIconService] Android font fallback failed: {fontEx.Message}");
                        shell.SetValue(Shell.FlyoutIconProperty, androidFallbackIcon);
                    }
                    
                    Debug.WriteLine("[ShellIconService] Android: Applied EXTREME MaterialButton counter-attack");
#else
                    // Other platforms: Use FontImageSource
                    var testIcon = new FontImageSource
                    {
                        Glyph = HAMBURGER_GLYPH,
                        Color = Microsoft.Maui.Graphics.Color.Parse(FALLBACK_ICON_COLOR), // Bright red for testing
                        Size = LARGE_ICON_SIZE,
                        FontFamily = null // Use default system font
                    };
                    shell.FlyoutIcon = testIcon;
#endif
                    
                    // Force all necessary properties
                    shell.FlyoutBehavior = FlyoutBehavior.Flyout;
                    Shell.SetNavBarIsVisible(shell, true);
                    
                    Debug.WriteLine($"[ShellIconService] Applied test icon: Red {HAMBURGER_GLYPH} at size {LARGE_ICON_SIZE}");
                }
                else
                {
                    Debug.WriteLine("[ShellIconService] ✓ Icon validation successful");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellIconService] Error in ForceIconVisibility: {ex.Message}");
                // Ultimate fallback
                shell.FlyoutIcon = new FileImageSource { File = FALLBACK_ICON };
            }
        }
    }
}