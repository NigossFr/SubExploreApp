using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Businesses;

namespace SubExplore.Views.Businesses;

public partial class BusinessDetailsPage : ContentPage
{
    private readonly BusinessDetailsViewModel _viewModel;
    
    public BusinessDetailsPage(BusinessDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Subscribe to property changes to update map
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] BusinessDetailsPage.OnAppearing: Checking if initialization needed");
            
            // 🚀 CRITICAL FIX: Only initialize if ApplyQueryAttributes hasn't done it already
            // ApplyQueryAttributes is called before OnAppearing and should handle the initialization
            if (string.IsNullOrEmpty(_viewModel.BusinessId))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] BusinessDetailsPage.OnAppearing: No BusinessId, initializing as fallback");
                await _viewModel.InitializeAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsPage.OnAppearing: BusinessId '{_viewModel.BusinessId}' already set, ApplyQueryAttributes should have handled initialization");
            }
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] BusinessDetailsPage.OnAppearing: Process completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] BusinessDetailsPage.OnAppearing: Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] BusinessDetailsPage.OnAppearing: Full Exception: {ex}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BusinessDetailsViewModel.Business) && sender is BusinessDetailsViewModel vm)
        {
            UpdateMapLocation(vm);
        }
    }

    private void UpdateMapLocation(BusinessDetailsViewModel viewModel)
    {
        if (viewModel.Business == null) return;

        try
        {
            var business = viewModel.Business;
            var location = new Location((double)business.Latitude, (double)business.Longitude);
            
            // Clear existing pins
            businessMap.Pins.Clear();
            
            // Add pin for business
            var pin = new Pin
            {
                Label = business.Name ?? "Commerce",
                Address = business.Address ?? "",
                Type = PinType.Place,
                Location = location
            };
            
            businessMap.Pins.Add(pin);
            
            // Center map on business location
            var mapSpan = new MapSpan(location, 0.01, 0.01); // Zoom level
            businessMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating business map: {ex.Message}");
        }
    }

    private async void OnCustomHamburgerClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[BusinessDetailsPage] Custom hamburger button clicked - bypassing MAUI Shell bugs");
            
            bool flyoutOpened = false;
            
            // Method 1: Direct Shell access
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutIsPresented = true;
                flyoutOpened = true;
                System.Diagnostics.Debug.WriteLine("[BusinessDetailsPage] ✅ Flyout opened successfully via Shell.Current");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BusinessDetailsPage] ❌ No Shell.Current available - trying MessagingCenter");
                
                // Method 2: MessagingCenter communication (same as SpotDetailsPage solution)
                try
                {
                    // Use MessagingCenter to send flyout request to main application
                    MessagingCenter.Send<object>(this, "OpenFlyoutMenu");
                    System.Diagnostics.Debug.WriteLine("[BusinessDetailsPage] ✅ Flyout request sent via MessagingCenter");
                    
                    // Give MessagingCenter time to process the request
                    await Task.Delay(100);
                    
                    flyoutOpened = true; // Assume it will work
                }
                catch (Exception msgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[BusinessDetailsPage] ❌ MessagingCenter failed: {msgEx.Message}");
                }
            }
            
            if (!flyoutOpened)
            {
                System.Diagnostics.Debug.WriteLine("[BusinessDetailsPage] ⚠️ All flyout access methods failed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BusinessDetailsPage] ❌ Custom hamburger error: {ex.Message}");
        }
    }
}