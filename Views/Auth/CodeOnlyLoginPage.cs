using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public class CodeOnlyLoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private Entry _emailEntry;
    private Entry _passwordEntry;
    private Button _loginButton;
    private Label _errorLabel;
    private ActivityIndicator _loadingIndicator;

    public CodeOnlyLoginPage(LoginViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[CodeOnlyLoginPage] === DÉBUT LOGIN CODE-ONLY ===");
            
            _viewModel = viewModel;
            BindingContext = viewModel;
            
            Title = "SubExplore Login";
            BackgroundColor = Colors.LightBlue;
            
            CreateLoginUI();
            
            // Initialize ViewModel
            Device.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.InitializeAsync();
            });
            
            System.Diagnostics.Debug.WriteLine("[CodeOnlyLoginPage] ✅ Page de login code-only créée avec succès");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodeOnlyLoginPage] ❌ ERREUR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CodeOnlyLoginPage] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void CreateLoginUI()
    {
        var scrollView = new ScrollView();
        var mainLayout = new StackLayout
        {
            Padding = new Thickness(30, 60, 30, 30),
            Spacing = 25,
            BackgroundColor = Colors.White
        };

        // Header
        var headerLayout = new StackLayout
        {
            Spacing = 20,
            HorizontalOptions = LayoutOptions.Center
        };

        var logoFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#006994"),
            CornerRadius = 35,
            HeightRequest = 70,
            WidthRequest = 70,
            HorizontalOptions = LayoutOptions.Center,
            HasShadow = false,
            BorderColor = Colors.Transparent,
            Content = new Label
            {
                Text = "S",
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.White
            }
        };

        var titleLabel = new Label
        {
            Text = "Bienvenue sur SubExplore",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#006994"),
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitleLabel = new Label
        {
            Text = "Découvrez et partagez les plus beaux spots de plongée",
            FontSize = 16,
            TextColor = Color.FromArgb("#666666"),
            HorizontalOptions = LayoutOptions.Center
        };

        headerLayout.Children.Add(logoFrame);
        headerLayout.Children.Add(titleLabel);
        headerLayout.Children.Add(subtitleLabel);

        // Error Message
        _errorLabel = new Label
        {
            TextColor = Colors.Red,
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        _errorLabel.SetBinding(Label.TextProperty, nameof(LoginViewModel.LoginErrorMessage));
        _errorLabel.SetBinding(Label.IsVisibleProperty, nameof(LoginViewModel.HasLoginError));

        // Login Form
        var formFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 12,
            HasShadow = false,
            Padding = new Thickness(25, 30)
        };

        var formLayout = new StackLayout
        {
            Spacing = 25
        };

        // Email Field
        var emailLabel = new Label
        {
            Text = "Adresse email",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

        _emailEntry = new Entry
        {
            Placeholder = "admin@subexplore.com",
            Keyboard = Keyboard.Email,
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            HeightRequest = 50
        };
        _emailEntry.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Email));

        // Password Field
        var passwordLabel = new Label
        {
            Text = "Mot de passe",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

        _passwordEntry = new Entry
        {
            Placeholder = "Minimum 8 caractères",
            IsPassword = true,
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            HeightRequest = 50
        };
        _passwordEntry.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Password));

        // Login Button
        _loginButton = new Button
        {
            Text = "Se connecter",
            BackgroundColor = Color.FromArgb("#006994"),
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 55,
            Margin = new Thickness(0, 20, 0, 0)
        };
        _loginButton.SetBinding(Button.CommandProperty, nameof(LoginViewModel.LoginCommand));
        _loginButton.SetBinding(Button.IsEnabledProperty, nameof(LoginViewModel.CanLogin));

        // Loading Indicator
        _loadingIndicator = new ActivityIndicator
        {
            Color = Color.FromArgb("#006994"),
            HeightRequest = 30
        };
        _loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(LoginViewModel.IsLoginInProgress));
        _loadingIndicator.SetBinding(ActivityIndicator.IsVisibleProperty, nameof(LoginViewModel.IsLoginInProgress));

        // Version Info
        var versionLabel = new Label
        {
            Text = "SubExplore v1.0 - Code-Only Login",
            FontSize = 12,
            TextColor = Color.FromArgb("#919191"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 30, 0, 0)
        };

        formLayout.Children.Add(emailLabel);
        formLayout.Children.Add(_emailEntry);
        formLayout.Children.Add(passwordLabel);
        formLayout.Children.Add(_passwordEntry);
        formLayout.Children.Add(_loginButton);
        formLayout.Children.Add(_loadingIndicator);

        formFrame.Content = formLayout;

        mainLayout.Children.Add(headerLayout);
        mainLayout.Children.Add(_errorLabel);
        mainLayout.Children.Add(formFrame);
        mainLayout.Children.Add(versionLabel);

        scrollView.Content = mainLayout;
        Content = scrollView;
    }
}