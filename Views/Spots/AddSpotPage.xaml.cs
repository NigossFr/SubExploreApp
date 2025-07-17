using SubExplore.ViewModels.Spots;

namespace SubExplore.Views.Spots
{
    [QueryProperty(nameof(LocationParameter), "location")]
    [QueryProperty(nameof(LatitudeParameter), "latitude")]
    [QueryProperty(nameof(LongitudeParameter), "longitude")]
    public partial class AddSpotPage : ContentPage
    {
        private readonly AddSpotViewModel _viewModel;
        
        public string LocationParameter { get; set; }
        public string LatitudeParameter { get; set; }
        public string LongitudeParameter { get; set; }

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
                // Initialize with location parameters if provided
                object parameter = null;
                if (!string.IsNullOrEmpty(LatitudeParameter) && !string.IsNullOrEmpty(LongitudeParameter))
                {
                    if (decimal.TryParse(LatitudeParameter, out decimal lat) && decimal.TryParse(LongitudeParameter, out decimal lng))
                    {
                        parameter = new { Latitude = lat, Longitude = lng, LocationParameter };
                    }
                }
                
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