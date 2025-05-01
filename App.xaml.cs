using SubExplore.Views.Settings;

namespace SubExplore;

public partial class App : Application
{
    public App(DatabaseTestPage databaseTestPage)
    {
        InitializeComponent();

        MainPage = new NavigationPage(databaseTestPage);
    }
}