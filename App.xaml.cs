using Microsoft.Maui.Controls;
using SubExplore.Views.Map;
using System.Diagnostics;

namespace SubExplore
{
    public partial class App : Application
    {
        // Option 1: Définir MainPage dans le constructeur APRES InitializeComponent
        // en utilisant le service provider qui devrait être disponible à ce stade.
        public App(IServiceProvider services) // MAUI peut injecter IServiceProvider ici
        {
            InitializeComponent();

            Debug.WriteLine("[App.xaml.cs] Constructeur App - Après InitializeComponent()");

            var mapPageInstance = services.GetService<MapPage>();
            if (mapPageInstance == null)
            {
                Debug.WriteLine("[App.xaml.cs] ERREUR: Impossible de résoudre MapPage depuis le conteneur de services dans le constructeur App!");
                // Solution de secours très basique
                MainPage = new ContentPage { Content = new Label { Text = "Erreur: MapPage non résolue au démarrage", TextColor = Colors.Red } };
            }
            else
            {
                Debug.WriteLine("[App.xaml.cs] MapPage résolue dans le constructeur App. Création de NavigationPage.");
                MainPage = new NavigationPage(mapPageInstance);
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window = base.CreateWindow(activationState);

            window.Created += (s, e) =>
            {
                Debug.WriteLine("[App.xaml.cs] Window.Created event fired.");
                // La logique de configuration de la barre de navigation peut rester ici,
                // car elle s'applique à la MainPage déjà assignée.
                if (MainPage is NavigationPage navPage)
                {
                    Debug.WriteLine("[App.xaml.cs] MainPage is NavigationPage. Attempting to set BarBackgroundColor via Window.Created.");
                    try
                    {
                        if (Application.Current.Resources.TryGetValue("Primary", out var primaryColorResource) && primaryColorResource is Color castedPrimaryColor)
                        {
                            navPage.BarBackgroundColor = castedPrimaryColor;
                        }
                        else
                        {
                            Debug.WriteLine("[App.xaml.cs] ERREUR: Ressource 'Primary' non trouvée dans Window.Created. Utilisation d'une couleur par défaut pour la barre.");
                            navPage.BarBackgroundColor = Colors.SlateBlue;
                        }
                        navPage.BarTextColor = Colors.White;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App.xaml.cs] Exception dans Window.Created lors de la config de NavigationPage: {ex.Message}");
                        navPage.BarBackgroundColor = Colors.DarkGray;
                        navPage.BarTextColor = Colors.White;
                    }
                }
            };
            return window;
        }
    }
}