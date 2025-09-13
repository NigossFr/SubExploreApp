using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace SubExplore.Views.Controls
{
    public partial class CustomNavigationBar : ContentView
    {
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(CustomNavigationBar), "Page Title");

        public static readonly BindableProperty ShowActionButtonProperty =
            BindableProperty.Create(nameof(ShowActionButton), typeof(bool), typeof(CustomNavigationBar), false,
                propertyChanged: OnShowActionButtonChanged);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool ShowActionButton
        {
            get => (bool)GetValue(ShowActionButtonProperty);
            set => SetValue(ShowActionButtonProperty, value);
        }

        public event EventHandler? HamburgerClicked;
        public event EventHandler? ActionClicked;

        public CustomNavigationBar()
        {
            InitializeComponent();
            Debug.WriteLine("[CustomNavigationBar] Initialized - bypassing MAUI Shell flyout icon bugs");
        }

        private async void OnHamburgerClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[CustomNavigationBar] Hamburger button clicked - opening flyout");
                
                bool flyoutOpened = false;
                
                // Method 1: Direct Shell access
                if (Shell.Current != null)
                {
                    Shell.Current.FlyoutIsPresented = true;
                    flyoutOpened = true;
                    Debug.WriteLine("[CustomNavigationBar] ✅ Flyout opened via Shell.Current");
                }
                else
                {
                    Debug.WriteLine("[CustomNavigationBar] ❌ No Shell.Current available - trying alternative methods");
                    
                    // Method 2: Try to get Shell from Application.Current.MainPage
                    if (Application.Current?.MainPage is Shell appShell)
                    {
                        appShell.FlyoutIsPresented = true;
                        flyoutOpened = true;
                        Debug.WriteLine("[CustomNavigationBar] ✅ Flyout opened via Application.Current.MainPage");
                    }
                    // Method 3: Try to force flyout on any MainPage that might be a Shell
                    else if (Application.Current?.MainPage != null)
                    {
                        Debug.WriteLine("[CustomNavigationBar] 🔄 Trying to force flyout on existing MainPage");
                        try
                        {
                            // Try to cast MainPage to Shell and open flyout
                            var mainPageShell = Application.Current.MainPage;
                            if (mainPageShell.GetType().Name.Contains("Shell"))
                            {
                                // Use reflection to set FlyoutIsPresented
                                var flyoutProperty = mainPageShell.GetType().GetProperty("FlyoutIsPresented");
                                if (flyoutProperty != null)
                                {
                                    flyoutProperty.SetValue(mainPageShell, true);
                                    flyoutOpened = true;
                                    Debug.WriteLine("[CustomNavigationBar] ✅ Opened flyout via reflection on MainPage");
                                }
                            }
                        }
                        catch (Exception reflEx)
                        {
                            Debug.WriteLine($"[CustomNavigationBar] ❌ Reflection failed: {reflEx.Message}");
                        }
                    }
                }
                
                // Method 4: MessagingCenter communication (same as SpotDetailsPage solution)
                if (!flyoutOpened)
                {
                    try
                    {
                        Debug.WriteLine("[CustomNavigationBar] 🔄 All Shell access methods failed - trying MessagingCenter");
                        
                        // Use MessagingCenter to send flyout request to main application
                        MessagingCenter.Send<object>(this, "OpenFlyoutMenu");
                        Debug.WriteLine("[CustomNavigationBar] ✅ Flyout request sent via MessagingCenter");
                        
                        // Give MessagingCenter time to process
                        await Task.Delay(50);
                        
                        flyoutOpened = true; // Assume it will work
                    }
                    catch (Exception msgEx)
                    {
                        Debug.WriteLine($"[CustomNavigationBar] ❌ MessagingCenter failed: {msgEx.Message}");
                        
                        // Method 5: Fire event for parent handling (last resort)
                        try
                        {
                            HamburgerClicked?.Invoke(this, e);
                            Debug.WriteLine("[CustomNavigationBar] ✅ Fallback event fired to parent");
                        }
                        catch (Exception eventEx)
                        {
                            Debug.WriteLine($"[CustomNavigationBar] ❌ Parent event handling failed: {eventEx.Message}");
                        }
                    }
                }
                
                if (!flyoutOpened)
                {
                    Debug.WriteLine("[CustomNavigationBar] ⚠️ All flyout access methods failed");
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomNavigationBar] ❌ Error opening flyout: {ex.Message}");
            }
        }

        private void OnActionClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[CustomNavigationBar] Action button clicked");
                ActionClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomNavigationBar] Error in action button: {ex.Message}");
            }
        }

        private static void OnShowActionButtonChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomNavigationBar navbar && navbar.ActionButton != null)
            {
                navbar.ActionButton.IsVisible = (bool)newValue;
            }
        }

        /// <summary>
        /// Update the hamburger button style for better visibility
        /// </summary>
        public void SetHamburgerStyle(Color textColor, Color backgroundColor, double fontSize = 24)
        {
            if (HamburgerButton != null)
            {
                HamburgerButton.TextColor = textColor;
                HamburgerButton.BackgroundColor = backgroundColor;
                HamburgerButton.FontSize = fontSize;
                Debug.WriteLine($"[CustomNavigationBar] Hamburger style updated - Color: {textColor}, Size: {fontSize}");
            }
        }

        /// <summary>
        /// Set the title programmatically
        /// </summary>
        public void SetTitle(string title)
        {
            Title = title;
            if (TitleLabel != null)
            {
                TitleLabel.Text = title;
            }
            Debug.WriteLine($"[CustomNavigationBar] Title set to: {title}");
        }

        /// <summary>
        /// Find Shell instance in the application hierarchy
        /// </summary>
        private Shell FindShellInApplication()
        {
            try
            {
                // Try Application.Current.MainPage first
                if (Application.Current?.MainPage is Shell mainShell)
                    return mainShell;

                // Try to find Shell in visual tree if MainPage exists
                if (Application.Current?.MainPage != null)
                {
                    return FindShellInVisualTree(Application.Current.MainPage);
                }

                // Try reflection as last resort
                var appType = Application.Current?.GetType();
                var mainPageProperty = appType?.GetProperty("MainPage");
                var mainPage = mainPageProperty?.GetValue(Application.Current);
                if (mainPage is Shell reflectionShell)
                    return reflectionShell;

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomNavigationBar] Error finding Shell in application: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find Shell in visual tree by traversing all elements
        /// </summary>
        private Shell FindShellInVisualTree(Element element)
        {
            try
            {
                if (element is Shell shell)
                    return shell;

                if (element is IVisualTreeElement visualElement)
                {
                    foreach (var child in visualElement.GetVisualChildren())
                    {
                        if (child is Element childElement)
                        {
                            var foundShell = FindShellInVisualTree(childElement);
                            if (foundShell != null)
                                return foundShell;
                        }
                    }
                }

                // Also try LogicalChildren for older MAUI versions
                if (element.LogicalChildren != null)
                {
                    foreach (var child in element.LogicalChildren)
                    {
                        if (child is Element childElement)
                        {
                            var foundShell = FindShellInVisualTree(childElement);
                            if (foundShell != null)
                                return foundShell;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomNavigationBar] Error finding Shell in visual tree: {ex.Message}");
                return null;
            }
        }
    }
}