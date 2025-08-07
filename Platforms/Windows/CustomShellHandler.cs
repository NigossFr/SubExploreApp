// Alternative approach: Use a static method to force icon display
namespace SubExplore.Platforms.Windows
{
    public static class ShellIconHelper
    {
        public static void ForceSetFlyoutIcon(Microsoft.Maui.Controls.Shell shell)
        {
            try
            {
                // Force the icon multiple ways
                shell.FlyoutIcon = new Microsoft.Maui.Controls.FileImageSource { File = "menu_icon.png" };
                
                // Additional force with dispatcher
                Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.StartTimer(TimeSpan.FromMilliseconds(100), () =>
                {
                    try
                    {
                        shell.FlyoutIcon = new Microsoft.Maui.Controls.FileImageSource { File = "menu_icon.png" };
                        System.Diagnostics.Debug.WriteLine("[ShellIconHelper] FlyoutIcon forced on Windows");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ShellIconHelper] Timer error: {ex.Message}");
                    }
                    return false;
                });
                
                System.Diagnostics.Debug.WriteLine("[ShellIconHelper] FlyoutIcon helper called");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellIconHelper] ForceSetFlyoutIcon error: {ex.Message}");
            }
        }
    }
}