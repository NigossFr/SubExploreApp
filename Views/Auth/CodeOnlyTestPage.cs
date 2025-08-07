namespace SubExplore.Views.Auth;

public class CodeOnlyTestPage : ContentPage
{
    public CodeOnlyTestPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[CodeOnlyTestPage] === DÉBUT TEST CODE-ONLY ===");
            
            Title = "Code Only Test";
            BackgroundColor = Colors.LightGreen;
            
            var mainLayout = new StackLayout
            {
                Padding = new Thickness(20),
                Spacing = 20,
                BackgroundColor = Colors.White,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            
            var titleLabel = new Label
            {
                Text = "🧪 CODE-ONLY TEST PAGE",
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.DarkBlue,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = Colors.Yellow,
                Padding = new Thickness(10)
            };
            
            var infoLabel = new Label
            {
                Text = "Cette page est créée entièrement en C#",
                FontSize = 16,
                TextColor = Colors.DarkGreen,
                HorizontalOptions = LayoutOptions.Center
            };
            
            var successLabel = new Label
            {
                Text = "✅ Si vous voyez ceci, le rendu MAUI fonctionne",
                FontSize = 14,
                TextColor = Colors.Green,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };
            
            var testButton = new Button
            {
                Text = "Test Button - Code Only",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                FontSize = 16,
                HeightRequest = 50
            };
            
            testButton.Clicked += (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine("[CodeOnlyTestPage] Button clicked - Event working!");
            };
            
            mainLayout.Children.Add(titleLabel);
            mainLayout.Children.Add(infoLabel);
            mainLayout.Children.Add(successLabel);
            mainLayout.Children.Add(testButton);
            
            Content = mainLayout;
            
            System.Diagnostics.Debug.WriteLine("[CodeOnlyTestPage] ✅ Page créée avec succès en code C#");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodeOnlyTestPage] ❌ ERREUR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CodeOnlyTestPage] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}