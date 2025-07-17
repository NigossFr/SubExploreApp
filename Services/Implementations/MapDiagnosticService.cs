using Microsoft.Extensions.Configuration;
using SubExplore.Services.Interfaces;
using System.Text;

#if ANDROID
using Android.Content.PM;
#endif

namespace SubExplore.Services.Implementations
{
    public class MapDiagnosticService : IMapDiagnosticService
    {
        private readonly IConfiguration _configuration;
        private readonly IDialogService _dialogService;

        public MapDiagnosticService(IConfiguration configuration, IDialogService dialogService)
        {
            _configuration = configuration;
            _dialogService = dialogService;
        }

        public async Task<bool> CheckGoogleMapsConfigurationAsync()
        {
            try
            {
                var diagnosticInfo = await GetMapDiagnosticInfoAsync();
                System.Diagnostics.Debug.WriteLine($"[MAP DIAGNOSTIC] {diagnosticInfo}");
                
                // Check if API key is configured
#if ANDROID
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var packageManager = context.PackageManager;
                var packageName = context.PackageName;
                
                if (packageManager != null && packageName != null)
                {
                    var appInfo = packageManager.GetApplicationInfo(packageName, Android.Content.PM.PackageInfoFlags.MetaData);
                    var metaData = appInfo?.MetaData;
                    
                    if (metaData != null)
                    {
                        var apiKey = metaData.GetString("com.google.android.geo.API_KEY");
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            System.Diagnostics.Debug.WriteLine($"[INFO] Google Maps API key found: {apiKey[..Math.Min(10, apiKey.Length)]}...");
                            return true;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[ERROR] Google Maps API key not found in AndroidManifest.xml");
                return false;
#else
                System.Diagnostics.Debug.WriteLine("[INFO] Platform-specific Google Maps check not available");
                return true;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Map configuration check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetMapDiagnosticInfoAsync()
        {
            var info = new StringBuilder();
            
            try
            {
                // Platform information
                info.AppendLine($"Platform: {DeviceInfo.Platform}");
                info.AppendLine($"Device Type: {DeviceInfo.DeviceType}");
                info.AppendLine($"OS Version: {DeviceInfo.VersionString}");
                
                // Configuration
                var defaultLat = _configuration.GetValue<double>("AppSettings:DefaultLatitude", 0);
                var defaultLng = _configuration.GetValue<double>("AppSettings:DefaultLongitude", 0);
                var defaultZoom = _configuration.GetValue<double>("AppSettings:DefaultZoomLevel", 0);
                
                info.AppendLine($"Default coordinates: {defaultLat}, {defaultLng}");
                info.AppendLine($"Default zoom: {defaultZoom}");
                
                // Network connectivity
                var connectivity = Connectivity.Current;
                info.AppendLine($"Network Access: {connectivity.NetworkAccess}");
                info.AppendLine($"Connection Profiles: {string.Join(", ", connectivity.ConnectionProfiles)}");
                
                // Location permissions
                var locationPermission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                info.AppendLine($"Location Permission: {locationPermission}");

#if ANDROID
                // Android-specific checks
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                if (context != null)
                {
                    var packageManager = context.PackageManager;
                    var packageName = context.PackageName;
                    
                    if (packageManager != null && packageName != null)
                    {
                        var appInfo = packageManager.GetApplicationInfo(packageName, Android.Content.PM.PackageInfoFlags.MetaData);
                        var metaData = appInfo?.MetaData;
                        
                        if (metaData != null)
                        {
                            var apiKey = metaData.GetString("com.google.android.geo.API_KEY");
                            info.AppendLine($"Google Maps API Key: {(!string.IsNullOrEmpty(apiKey) ? "Present" : "Missing")}");
                        }
                    }
                }
#endif
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting diagnostic info: {ex.Message}";
            }
        }

        public async Task<bool> TestMapTileLoadingAsync()
        {
            try
            {
                // Test if Google Maps tiles are accessible
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var testUrl = "https://maps.googleapis.com/maps/api/js?key=AIzaSyDAKkZk5ceq0-hFQDO00D26tWfjSp2RCaM&callback=test";
                var response = await httpClient.GetAsync(testUrl);
                
                var isSuccess = response.IsSuccessStatusCode;
                System.Diagnostics.Debug.WriteLine($"[INFO] Google Maps API test: {(isSuccess ? "Success" : "Failed")} - {response.StatusCode}");
                
                return isSuccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Map tile test failed: {ex.Message}");
                return false;
            }
        }
    }
}