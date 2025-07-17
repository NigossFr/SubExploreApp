using Android.App;
using Android.Runtime;
using Android.Content.PM;

namespace SubExplore
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize Google Maps
            try
            {
                var metaData = PackageManager?.GetApplicationInfo(PackageName!, PackageInfoFlags.MetaData)?.MetaData;
                if (metaData != null)
                {
                    var apiKey = metaData.GetString("com.google.android.geo.API_KEY");
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"[INFO] Google Maps API key found: {apiKey[..10]}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ERROR] Google Maps API key not found in manifest");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to initialize Google Maps: {ex.Message}");
            }
        }
    }
}
