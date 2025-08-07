using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public partial class DebugLoginPage : ContentPage
{
    public DebugLoginPage(LoginViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DebugLoginPage] Constructeur démarré");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[DebugLoginPage] InitializeComponent() terminé");
            
            BindingContext = viewModel;
            System.Diagnostics.Debug.WriteLine("[DebugLoginPage] BindingContext assigné");
            
            // Force une initialisation simple du ViewModel
            viewModel.Email = "debug@test.com";
            viewModel.Password = "DebugPassword123!";
            
            System.Diagnostics.Debug.WriteLine("[DebugLoginPage] ViewModel initialisé avec des valeurs de test");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DebugLoginPage] ERREUR dans le constructeur: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DebugLoginPage] Stack trace: {ex.StackTrace}");
            throw; // Re-lancer l'exception pour debugging
        }
    }
}