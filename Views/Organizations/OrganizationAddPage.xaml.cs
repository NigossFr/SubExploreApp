using SubExplore.ViewModels.Organizations;
using Microsoft.Extensions.Logging;

namespace SubExplore.Views.Organizations
{
    public partial class OrganizationAddPage : ContentPage
    {
        private readonly OrganizationAddViewModel _viewModel;
        private readonly ILogger<OrganizationAddPage> _logger;

        public OrganizationAddPage(OrganizationAddViewModel viewModel, ILogger<OrganizationAddPage> logger)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _logger.LogDebug("OrganizationAddPage OnAppearing called");

            try
            {
                // Récupérer les paramètres de navigation depuis les query parameters
                var parameters = new Dictionary<string, object>();

                // Gérer les paramètres de navigation depuis Shell
                if (Shell.Current.CurrentState?.Location?.OriginalString?.Contains("Latitude") == true)
                {
                    var uri = new Uri(Shell.Current.CurrentState.Location.OriginalString);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                    if (double.TryParse(query["Latitude"], out var lat))
                        parameters["Latitude"] = lat;

                    if (double.TryParse(query["Longitude"], out var lon))
                        parameters["Longitude"] = lon;

                    if (!string.IsNullOrEmpty(query["Mode"]))
                        parameters["Mode"] = query["Mode"];
                }

                await _viewModel.InitializeAsync(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: OrganizationAddPage OnAppearing failed");
            }
        }
    }
}