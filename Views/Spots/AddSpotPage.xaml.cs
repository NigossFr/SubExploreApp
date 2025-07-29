using SubExplore.ViewModels.Spots;

namespace SubExplore.Views.Spots
{
    [QueryProperty(nameof(LocationParameter), "location")]
    [QueryProperty(nameof(LatitudeParameter), "latitude")]
    [QueryProperty(nameof(LongitudeParameter), "longitude")]
    [QueryProperty(nameof(SpotIdParameter), "spotid")]
    [QueryProperty(nameof(ModeParameter), "mode")]
    [QueryProperty(nameof(SpotNameParameter), "spotname")]
    public partial class AddSpotPage : ContentPage
    {
        private readonly AddSpotViewModel _viewModel;
        
        public string LocationParameter { get; set; }
        public string LatitudeParameter { get; set; }
        public string LongitudeParameter { get; set; }
        public string SpotIdParameter { get; set; }
        public string ModeParameter { get; set; }
        public string SpotNameParameter { get; set; }

        public AddSpotPage(AddSpotViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: SpotIdParameter={SpotIdParameter}, ModeParameter={ModeParameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: LatitudeParameter={LatitudeParameter}, LongitudeParameter={LongitudeParameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: SpotNameParameter={SpotNameParameter}, LocationParameter={LocationParameter}");
                
                // Initialize with parameters if provided
                object parameter = null;
                
                // Check if we have spot ID for edit mode
                if (!string.IsNullOrEmpty(SpotIdParameter) && int.TryParse(SpotIdParameter, out int spotId) && spotId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: Creating edit mode parameter with SpotId={spotId}");
                    
                    // Parse coordinates if available
                    decimal? lat = null, lng = null;
                    if (!string.IsNullOrEmpty(LatitudeParameter) && decimal.TryParse(LatitudeParameter, out decimal latValue))
                        lat = latValue;
                    if (!string.IsNullOrEmpty(LongitudeParameter) && decimal.TryParse(LongitudeParameter, out decimal lngValue))
                        lng = lngValue;
                    
                    // Create parameter object with all available data
                    parameter = new { 
                        spotid = spotId,
                        mode = ModeParameter ?? "edit",
                        spotname = SpotNameParameter,
                        Latitude = lat,
                        Longitude = lng,
                        LocationParameter 
                    };
                }
                else if (!string.IsNullOrEmpty(LatitudeParameter) && !string.IsNullOrEmpty(LongitudeParameter))
                {
                    // Legacy location-only parameter
                    if (decimal.TryParse(LatitudeParameter, out decimal lat) && decimal.TryParse(LongitudeParameter, out decimal lng))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: Creating location-only parameter with Lat={lat}, Lng={lng}");
                        parameter = new { Latitude = lat, Longitude = lng, LocationParameter };
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] AddSpotPage.OnAppearing: Calling InitializeAsync with parameter={parameter?.GetType().Name}");
                await _viewModel.InitializeAsync(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] AddSpotPage OnAppearing failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}