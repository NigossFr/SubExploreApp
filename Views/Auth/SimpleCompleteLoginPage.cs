using SubExplore.ViewModels.Auth;
using SubExplore.Helpers.Converters;

namespace SubExplore.Views.Auth;

public partial class SimpleCompleteLoginPage : ContentPage
{
    private readonly SimpleLoginViewModel _viewModel;
    
    // UI Elements
    private Entry _emailEntry;
    private Entry _passwordEntry;
    private Button _loginButton;
    private Button _registerButton;
    private Button _forgotPasswordButton;
    private Button _testModeButton;
    private CheckBox _rememberMeCheckBox;
    private Label _rememberMeLabel;
    private Label _errorLabel;
    private ActivityIndicator _loadingIndicator;
    private Button _togglePasswordButton;

    public SimpleCompleteLoginPage(SimpleLoginViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] === CRÉATION PAGE DE CONNEXION SUPABASE ===");
            
            InitializeComponent(); // Initialize XAML
            _viewModel = viewModel;
            BindingContext = viewModel;
            CreateCompleteLoginUI();
            SetupEventHandlers();
            
            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] ✅ Page de connexion créée avec succès");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCompleteLoginPage] ❌ Erreur: {ex.Message}");
            throw;
        }
    }

    private void CreateCompleteLoginUI()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] Création de l'interface utilisateur...");

            // Configuration principale
            Title = "Connexion";
            BackgroundColor = Color.FromArgb("#F8FDFF");

            // Header avec logo
            var headerStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Spacing = 15,
                Margin = new Thickness(0, 40, 0, 30),
                Children =
                {
                    new Label
                    {
                        Text = "🌊",
                        FontSize = 60,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Color.FromArgb("#1976D2")
                    },
                    new Label
                    {
                        Text = "SubExplore",
                        FontSize = 32,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Color.FromArgb("#1976D2")
                    },
                    new Label
                    {
                        Text = "Explorez les profondeurs",
                        FontSize = 16,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Color.FromArgb("#666666")
                    }
                }
            };

            // Email Entry
            _emailEntry = new Entry
            {
                Placeholder = "Adresse email",
                Keyboard = Keyboard.Email,
                FontSize = 16,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#333333"),
                Margin = new Thickness(0, 5)
            };
            _emailEntry.SetBinding(Entry.TextProperty, nameof(_viewModel.Email));

            // Password Entry avec toggle
            var passwordStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { }
            };

            _passwordEntry = new Entry
            {
                Placeholder = "Mot de passe",
                IsPassword = true,
                FontSize = 16,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#333333"),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            _passwordEntry.SetBinding(Entry.TextProperty, nameof(_viewModel.Password));
            _passwordEntry.SetBinding(Entry.IsPasswordProperty, nameof(_viewModel.IsPasswordVisible), converter: new InverseBoolConverter());

            _togglePasswordButton = new Button
            {
                Text = "👁",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#666666"),
                FontSize = 16,
                WidthRequest = 40,
                HeightRequest = 40
            };
            _togglePasswordButton.SetBinding(Button.CommandProperty, nameof(_viewModel.TogglePasswordVisibilityCommand));

            var passwordFrame = new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                CornerRadius = 8,
                Padding = new Thickness(15, 0),
                HasShadow = false,
                Margin = new Thickness(0, 5),
                Content = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Children = { _passwordEntry, _togglePasswordButton }
                }
            };

            // Remember Me
            var rememberMeStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 10),
                Children = { }
            };

            _rememberMeCheckBox = new CheckBox
            {
                Color = Color.FromArgb("#1976D2")
            };
            _rememberMeCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(_viewModel.RememberMe));

            _rememberMeLabel = new Label
            {
                Text = "Se souvenir de moi",
                FontSize = 14,
                TextColor = Color.FromArgb("#666666"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            rememberMeStack.Children.Add(_rememberMeCheckBox);
            rememberMeStack.Children.Add(_rememberMeLabel);

            // Error Label
            _errorLabel = new Label
            {
                FontSize = 14,
                TextColor = Colors.Red,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10),
                IsVisible = false
            };
            _errorLabel.SetBinding(Label.TextProperty, nameof(_viewModel.LoginErrorMessage));
            _errorLabel.SetBinding(Label.IsVisibleProperty, nameof(_viewModel.HasLoginError));

            // Loading Indicator
            _loadingIndicator = new ActivityIndicator
            {
                Color = Color.FromArgb("#1976D2"),
                IsVisible = false,
                Margin = new Thickness(0, 10)
            };
            _loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(_viewModel.IsLoginInProgress));
            _loadingIndicator.SetBinding(ActivityIndicator.IsVisibleProperty, nameof(_viewModel.IsLoginInProgress));

            // Login Button
            _loginButton = new Button
            {
                Text = "Se connecter",
                BackgroundColor = Color.FromArgb("#1976D2"),
                TextColor = Colors.White,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 50,
                Margin = new Thickness(0, 20, 0, 10)
            };
            _loginButton.SetBinding(Button.CommandProperty, nameof(_viewModel.LoginCommand));
            _loginButton.SetBinding(Button.IsEnabledProperty, nameof(_viewModel.IsLoginInProgress), converter: new InverseBoolConverter());

            // Test Mode Button
            _testModeButton = new Button
            {
                Text = "🧪 Mode Test (sans connexion)",
                BackgroundColor = Color.FromArgb("#FF9800"),
                TextColor = Colors.White,
                FontSize = 14,
                CornerRadius = 8,
                HeightRequest = 40,
                Margin = new Thickness(0, 10)
            };
            _testModeButton.SetBinding(Button.CommandProperty, nameof(_viewModel.TestModeCommand));

            // Register Button
            _registerButton = new Button
            {
                Text = "Créer un compte",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#1976D2"),
                FontSize = 16,
                Margin = new Thickness(0, 10)
            };
            _registerButton.SetBinding(Button.CommandProperty, nameof(_viewModel.RegisterCommand));

            // Forgot Password Button
            _forgotPasswordButton = new Button
            {
                Text = "Mot de passe oublié ?",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#666666"),
                FontSize = 14,
                Margin = new Thickness(0, 5)
            };
            _forgotPasswordButton.SetBinding(Button.CommandProperty, nameof(_viewModel.ForgotPasswordCommand));

            // Main Layout
            var mainStack = new StackLayout
            {
                Spacing = 15,
                Padding = new Thickness(30, 20),
                Children =
                {
                    headerStack,
                    new Frame
                    {
                        BackgroundColor = Colors.White,
                        BorderColor = Color.FromArgb("#E0E0E0"),
                        CornerRadius = 8,
                        Padding = new Thickness(15),
                        HasShadow = false,
                        Content = _emailEntry
                    },
                    passwordFrame,
                    rememberMeStack,
                    _errorLabel,
                    _loadingIndicator,
                    _loginButton,
                    _testModeButton,
                    _registerButton,
                    _forgotPasswordButton
                }
            };

            Content = new ScrollView
            {
                Content = mainStack
            };

            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] ✅ Interface utilisateur créée");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCompleteLoginPage] ❌ Erreur création UI: {ex.Message}");
            throw;
        }
    }

    private void SetupEventHandlers()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] Configuration des événements...");

            // Tap gesture for Remember Me
            var rememberMeTapGesture = new TapGestureRecognizer();
            rememberMeTapGesture.Tapped += (s, e) => _rememberMeCheckBox.IsChecked = !_rememberMeCheckBox.IsChecked;
            _rememberMeLabel.GestureRecognizers.Add(rememberMeTapGesture);

            System.Diagnostics.Debug.WriteLine("[SimpleCompleteLoginPage] ✅ Événements configurés");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCompleteLoginPage] ❌ Erreur événements: {ex.Message}");
        }
    }
}