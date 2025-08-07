namespace SubExplore.Views.Auth;

public partial class BasicTestPage : ContentPage
{
    public BasicTestPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[BasicTestPage] Constructeur sans paramètre démarré");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[BasicTestPage] ✓ BasicTestPage créée avec succès - AUCUN SERVICE REQUIS");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BasicTestPage] ❌ ERREUR CRITIQUE: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BasicTestPage] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}