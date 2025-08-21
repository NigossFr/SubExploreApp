using SubExplore.ViewModels.Admin;

namespace SubExplore.Views.Admin;

public partial class SpotDiagnosticPage : ContentPage
{
    public SpotDiagnosticPage(SpotDiagnosticViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}