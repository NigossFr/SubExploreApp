using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using System.Text;
using MapControl = Microsoft.Maui.Controls.Maps.Map;

#if ANDROID
using Android.Content.PM;
using Microsoft.Maui.Maps.Handlers;
using Android.Gms.Maps;
using Microsoft.Maui.Platform;
#endif

#if IOS
using Foundation;
#endif

namespace SubExplore.Services.Implementations
{
    public class PlatformMapService : IPlatformMapService
    {
        private readonly IConfiguration _configuration;
        private readonly IDialogService _dialogService;
        private readonly IConnectivityService _connectivityService;

        public event EventHandler<MapPinClickedEventArgs> PinClicked;

        public PlatformMapService(
            IConfiguration configuration,
            IDialogService dialogService,
            IConnectivityService connectivityService)
        {
            _configuration = configuration;
            _dialogService = dialogService;
            _connectivityService = connectivityService;
        }

        public async Task<bool> InitializePlatformMapAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[INFO] Initializing map for platform: {DeviceInfo.Platform}");

#if ANDROID
                return await InitializeAndroidMapAsync();
#elif IOS
                return await InitializeiOSMapAsync();
#elif WINDOWS
                return await InitializeWindowsMapAsync();
#else
                System.Diagnostics.Debug.WriteLine("[WARNING] Unsupported platform for map initialization");
                return true; // Default to success for unknown platforms
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Platform map initialization failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateMapConfigurationAsync()
        {
            try
            {
                var diagnostics = await GetPlatformDiagnosticsAsync();
                System.Diagnostics.Debug.WriteLine($"[VALIDATION] {diagnostics}");

                // Check network connectivity
                if (!_connectivityService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] No network connectivity for map tiles");
                    return false;
                }

                // Platform-specific validation
#if ANDROID
                return await ValidateAndroidConfigurationAsync();
#elif IOS
                return await ValidateiOSConfigurationAsync();
#else
                return true;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Map configuration validation failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetPlatformDiagnosticsAsync()
        {
            var info = new StringBuilder();

            try
            {
                info.AppendLine($"Platform: {DeviceInfo.Platform}");
                info.AppendLine($"Device Type: {DeviceInfo.DeviceType}");
                info.AppendLine($"OS Version: {DeviceInfo.VersionString}");
                info.AppendLine($"Network Access: {_connectivityService.NetworkAccess}");

#if ANDROID
                await AppendAndroidDiagnosticsAsync(info);
#elif IOS
                await AppendiOSDiagnosticsAsync(info);
#elif WINDOWS
                await AppendWindowsDiagnosticsAsync(info);
#endif

                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting platform diagnostics: {ex.Message}";
            }
        }

        public async Task<bool> TestMapTileAccessibilityAsync()
        {
            try
            {
                if (!_connectivityService.IsConnected)
                {
                    return false;
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Test different tile sources based on platform
                var testUrls = GetPlatformTestUrls();

                foreach (var url in testUrls)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SUCCESS] Map tile test passed: {url}");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING] Map tile test failed for {url}: {ex.Message}");
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Map tile accessibility test failed: {ex.Message}");
                return false;
            }
        }

        public string GetRecommendedMapType()
        {
#if ANDROID
            // Android with Google Maps SDK works well with all types
            return "Street";
#elif IOS
            // iOS Maps integration works best with Street view
            return "Street";
#elif WINDOWS
            // Windows maps prefer Hybrid for better performance
            return "Hybrid";
#else
            return "Street";
#endif
        }

        public void ApplyMapOptimizations(MapControl map)
        {
            try
            {
                // Apply platform-specific optimizations without enum dependencies
                System.Diagnostics.Debug.WriteLine($"[INFO] Applying {DeviceInfo.Platform} map optimizations");

#if ANDROID
                ApplyAndroidOptimizations(map);
#elif IOS
                ApplyiOSOptimizations(map);
#elif WINDOWS
                ApplyWindowsOptimizations(map);
#endif

                System.Diagnostics.Debug.WriteLine($"[INFO] Applied {DeviceInfo.Platform} optimizations to map");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to apply map optimizations: {ex.Message}");
            }
        }

        // Platform-specific implementations
#if ANDROID
        private async Task<bool> InitializeAndroidMapAsync()
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var packageManager = context?.PackageManager;
                var packageName = context?.PackageName;

                if (packageManager != null && packageName != null)
                {
                    var appInfo = packageManager.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
                    var metaData = appInfo?.MetaData;

                    if (metaData != null)
                    {
                        var apiKey = metaData.GetString("com.google.android.geo.API_KEY");
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            System.Diagnostics.Debug.WriteLine($"[SUCCESS] Android Google Maps initialized with API key");
                            return true;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("[ERROR] Android Google Maps API key not found");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Android map initialization failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ValidateAndroidConfigurationAsync()
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context == null) return false;

            var packageManager = context.PackageManager;
            var packageName = context.PackageName;

            if (packageManager != null && packageName != null)
            {
                var appInfo = packageManager.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
                var apiKey = appInfo?.MetaData?.GetString("com.google.android.geo.API_KEY");
                return !string.IsNullOrEmpty(apiKey);
            }

            return false;
        }

        private async Task AppendAndroidDiagnosticsAsync(StringBuilder info)
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context != null)
            {
                var packageManager = context.PackageManager;
                var packageName = context.PackageName;

                if (packageManager != null && packageName != null)
                {
                    var appInfo = packageManager.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
                    var apiKey = appInfo?.MetaData?.GetString("com.google.android.geo.API_KEY");
                    info.AppendLine($"Google Maps API Key: {(!string.IsNullOrEmpty(apiKey) ? "Present" : "Missing")}");
                    
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        info.AppendLine($"API Key Preview: {apiKey[..Math.Min(10, apiKey.Length)]}...");
                    }
                }
            }
        }

        private void ApplyAndroidOptimizations(MapControl map)
        {
            // Android-specific optimizations
            map.IsTrafficEnabled = false; // Disable traffic for better performance

            var handler = map.Handler as MapHandler;
            if (handler?.Map is GoogleMap googleMap)
            {
                // Disable the default map toolbar that appears on marker click
                googleMap.UiSettings.MapToolbarEnabled = false;

                // Handle marker clicks directly
                googleMap.MarkerClick += (sender, e) =>
                {
                    var marker = e.Marker;
                    if (marker != null)
                    {
                        // Find the corresponding .NET MAUI Pin.
                        // We match by location with a small tolerance to handle floating point differences.
                                                                        var pin = map.Pins.FirstOrDefault(p => p.BindingContext is Spot spot && spot.Id.ToString() == marker.Snippet);

                        if (pin != null)
                        {
                            // Raise our custom event
                            PinClicked?.Invoke(this, new MapPinClickedEventArgs(pin));
                            System.Diagnostics.Debug.WriteLine($"[INFO] Native pin clicked: {pin.Label}");
                        }
                    }
                    // Mark the event as handled to prevent the default Google Maps behavior
                    e.Handled = true;
                };
            }
        }
#endif

#if IOS
        private async Task<bool> InitializeiOSMapAsync()
        {
            try
            {
                var apiKey = NSBundle.MainBundle.ObjectForInfoDictionary("GoogleMapsAPIKey")?.ToString();
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine($"[SUCCESS] iOS Google Maps initialized with API key");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine("[ERROR] iOS Google Maps API key not found in Info.plist");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] iOS map initialization failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ValidateiOSConfigurationAsync()
        {
            var apiKey = NSBundle.MainBundle.ObjectForInfoDictionary("GoogleMapsAPIKey")?.ToString();
            return !string.IsNullOrEmpty(apiKey);
        }

        private async Task AppendiOSDiagnosticsAsync(StringBuilder info)
        {
            var apiKey = NSBundle.MainBundle.ObjectForInfoDictionary("GoogleMapsAPIKey")?.ToString();
            info.AppendLine($"Google Maps API Key: {(!string.IsNullOrEmpty(apiKey) ? "Present" : "Missing")}");
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                info.AppendLine($"API Key Preview: {apiKey[..Math.Min(10, apiKey.Length)]}...");
            }
        }

        private void ApplyiOSOptimizations(MapControl map)
        {
            // iOS-specific optimizations
            map.IsTrafficEnabled = false;
        }
#endif

#if WINDOWS
        private async Task<bool> InitializeWindowsMapAsync()
        {
            System.Diagnostics.Debug.WriteLine("[INFO] Windows map initialization - using default provider");
            return true;
        }

        private async Task AppendWindowsDiagnosticsAsync(StringBuilder info)
        {
            info.AppendLine("Windows Maps: Using default provider");
        }

        private void ApplyWindowsOptimizations(MapControl map)
        {
            // Windows-specific optimizations
            // Type already set in ApplyMapOptimizations
        }
#endif

        private List<string> GetPlatformTestUrls()
        {
            var urls = new List<string>();

#if ANDROID
            urls.Add("https://maps.googleapis.com/maps/api/js?key=AIzaSyDAKkZk5ceq0-hFQDO00D26tWfjSp2RCaM&callback=test");
#elif IOS
            urls.Add("https://maps.googleapis.com/maps/api/js?key=AIzaSyDAKkZk5ceq0-hFQDO00D26tWfjSp2RCaM&callback=test");
#endif
            
            // Fallback test URLs
            urls.Add("https://www.google.com/maps");
            urls.Add("https://api.mapbox.com/v1/");

            return urls;
        }
        
        public void RefreshMapDisplay(MapControl map)
        {
            try
            {
                if (map == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WARNING] RefreshMapDisplay called with null map");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[INFO] Refreshing map display for platform: {DeviceInfo.Platform}");
                
                // Simple map refresh without breaking changes
                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        // Force a simple layout update to refresh map display
                        var currentVisible = map.IsVisible;
                        if (currentVisible)
                        {
                            // Use a minimal opacity change to trigger refresh
                            var currentOpacity = map.Opacity;
                            map.Opacity = 0.99;
                            map.Opacity = currentOpacity;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[INFO] Map display refreshed successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to refresh map display: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshMapDisplay failed: {ex.Message}");
            }
        }
    }
}
