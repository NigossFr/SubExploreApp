namespace SubExplore.Views.Auth;

public partial class XamlDiagnosticPage : ContentPage
{
    public XamlDiagnosticPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[XamlDiagnosticPage] === DÉBUT TEST XAML ===");
            System.Diagnostics.Debug.WriteLine("[XamlDiagnosticPage] Avant InitializeComponent()");
            
            InitializeComponent();
            
            System.Diagnostics.Debug.WriteLine("[XamlDiagnosticPage] ✅ InitializeComponent() réussi");
            System.Diagnostics.Debug.WriteLine("[XamlDiagnosticPage] === XAML FONCTIONNEL ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[XamlDiagnosticPage] ❌ ERREUR XAML: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[XamlDiagnosticPage] Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[XamlDiagnosticPage] Inner exception: {ex.InnerException.Message}");
            }
            
            throw;
        }
    }
}