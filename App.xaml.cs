using SubExplore.Views.Settings;
using SubExplore.Views.Map;

namespace SubExplore;

public partial class App : Application
{
    public App(MapPage mapPage)
    {
        InitializeComponent();

        MainPage = new NavigationPage(mapPage)
        {
            BarBackgroundColor = (Color)Resources["Primary"],
            BarTextColor = Colors.White
        };
    }
}