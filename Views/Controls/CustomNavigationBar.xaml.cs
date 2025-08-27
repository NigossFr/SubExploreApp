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

        private void OnHamburgerClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[CustomNavigationBar] Hamburger button clicked - opening flyout");
                
                // Method 1: Direct Shell access
                if (Shell.Current != null)
                {
                    Shell.Current.FlyoutIsPresented = true;
                    Debug.WriteLine("[CustomNavigationBar] ✅ Flyout opened via Shell.Current");
                }
                else
                {
                    Debug.WriteLine("[CustomNavigationBar] ❌ No Shell.Current available");
                }
                
                // Method 2: Fire event for parent handling
                HamburgerClicked?.Invoke(this, e);
                
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
    }
}