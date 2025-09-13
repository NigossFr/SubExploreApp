using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Organizations;

namespace SubExplore.Views.Organizations;

public partial class OrganizationDetailsPage : ContentPage
{
    private readonly OrganizationDetailsViewModel _viewModel;
    
    public OrganizationDetailsPage(OrganizationDetailsViewModel viewModel)
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
            System.Diagnostics.Debug.WriteLine("[DEBUG] OrganizationDetailsPage.OnAppearing: Checking if initialization needed");
            
            // 🚀 CRITICAL FIX: Only initialize if ApplyQueryAttributes hasn't done it already
            // ApplyQueryAttributes is called before OnAppearing and should handle the initialization
            if (string.IsNullOrEmpty(_viewModel.OrganizationId))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] OrganizationDetailsPage.OnAppearing: No OrganizationId, initializing as fallback");
                await _viewModel.InitializeAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] OrganizationDetailsPage.OnAppearing: OrganizationId '{_viewModel.OrganizationId}' already set, ApplyQueryAttributes should have handled initialization");
            }
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] OrganizationDetailsPage.OnAppearing: Process completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] OrganizationDetailsPage.OnAppearing: Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] OrganizationDetailsPage.OnAppearing: Full Exception: {ex}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrganizationDetailsViewModel.Organization) && sender is OrganizationDetailsViewModel vm)
        {
            UpdateMapLocation(vm);
        }
    }

    private void UpdateMapLocation(OrganizationDetailsViewModel viewModel)
    {
        if (viewModel.Organization == null) return;

        try
        {
            var organization = viewModel.Organization;
            var location = new Location((double)organization.Latitude, (double)organization.Longitude);
            
            // Clear existing pins
            organizationMap.Pins.Clear();
            
            // Add pin for organization
            var pin = new Pin
            {
                Label = organization.Name ?? "Organisation",
                Address = organization.Address ?? "",
                Type = PinType.Place,
                Location = location
            };
            
            organizationMap.Pins.Add(pin);
            
            // Center map on organization location
            var mapSpan = new MapSpan(location, 0.01, 0.01); // Zoom level
            organizationMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating organization map: {ex.Message}");
        }
    }

    private async void OnCustomHamburgerClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[OrganizationDetailsPage] Custom hamburger button clicked - bypassing MAUI Shell bugs");
            
            bool flyoutOpened = false;
            
            // Method 1: Direct Shell access
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutIsPresented = true;
                flyoutOpened = true;
                System.Diagnostics.Debug.WriteLine("[OrganizationDetailsPage] ✅ Flyout opened successfully via Shell.Current");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[OrganizationDetailsPage] ❌ No Shell.Current available - trying MessagingCenter");
                
                // Method 2: MessagingCenter communication (same as SpotDetailsPage solution)
                try
                {
                    // Use MessagingCenter to send flyout request to main application
                    MessagingCenter.Send<object>(this, "OpenFlyoutMenu");
                    System.Diagnostics.Debug.WriteLine("[OrganizationDetailsPage] ✅ Flyout request sent via MessagingCenter");
                    
                    // Give MessagingCenter time to process the request
                    await Task.Delay(100);
                    
                    flyoutOpened = true; // Assume it will work
                }
                catch (Exception msgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrganizationDetailsPage] ❌ MessagingCenter failed: {msgEx.Message}");
                }
            }
            
            if (!flyoutOpened)
            {
                System.Diagnostics.Debug.WriteLine("[OrganizationDetailsPage] ⚠️ All flyout access methods failed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrganizationDetailsPage] ❌ Custom hamburger error: {ex.Message}");
        }
    }
}