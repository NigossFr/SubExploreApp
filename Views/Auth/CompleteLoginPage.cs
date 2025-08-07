using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public class CompleteLoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    
    // UI Elements
    private Entry _emailEntry;
    private Entry _passwordEntry;
    private Button _loginButton;
    private Button _registerButton;
    private Button _forgotPasswordButton;
    private CheckBox _rememberMeCheckBox;
    private Label _rememberMeLabel;
    private Label _errorLabel;
    private ActivityIndicator _loadingIndicator;
    private Button _togglePasswordButton;
    private Frame _socialLoginFrame;
    private Button _googleLoginButton;
    private Button _appleLoginButton;

    public CompleteLoginPage(LoginViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[CompleteLoginPage] === CRÉATION PAGE DE CONNEXION COMPLÈTE ===");
            
            _viewModel = viewModel;
            BindingContext = viewModel;
            
            Title = "SubExplore - Connexion";
            BackgroundColor = Color.FromArgb("#F8FDFF");
            
            CreateCompleteLoginUI();
            SetupEventHandlers();
            
            // Initialize ViewModel
            Device.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.InitializeAsync();
            });
            
            System.Diagnostics.Debug.WriteLine("[CompleteLoginPage] ✅ Page de connexion complète créée avec succès");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CompleteLoginPage] ❌ ERREUR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CompleteLoginPage] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void CreateCompleteLoginUI()
    {
        var scrollView = new ScrollView();
        var mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }, // Header
                new RowDefinition { Height = GridLength.Star }, // Form
                new RowDefinition { Height = GridLength.Auto }  // Footer
            },
            Padding = new Thickness(30, 60, 30, 30)
        };

        // Header Section
        CreateHeaderSection(mainGrid, 0);
        
        // Main Form Section
        CreateFormSection(mainGrid, 1);
        
        // Footer Section
        CreateFooterSection(mainGrid, 2);

        scrollView.Content = mainGrid;
        Content = scrollView;
    }

    private void CreateHeaderSection(Grid parentGrid, int row)
    {
        var headerStack = new StackLayout
        {
            Spacing = 30,
            Margin = new Thickness(0, 0, 0, 50)
        };

        // App Logo
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

        // Welcome Text
        var welcomeStack = new StackLayout
        {
            Spacing = 12
        };

        var titleLabel = new Label
        {
            Text = "Bienvenue sur SubExplore",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#006994"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var subtitleLabel = new Label
        {
            Text = "Découvrez et partagez les plus beaux spots de plongée",
            FontSize = 16,
            TextColor = Color.FromArgb("#666666"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        welcomeStack.Children.Add(titleLabel);
        welcomeStack.Children.Add(subtitleLabel);

        headerStack.Children.Add(logoFrame);
        headerStack.Children.Add(welcomeStack);

        Grid.SetRow(headerStack, row);
        parentGrid.Children.Add(headerStack);
    }

    private void CreateFormSection(Grid parentGrid, int row)
    {
        var formStack = new StackLayout
        {
            Spacing = 25
        };

        // Error Message
        CreateErrorMessage(formStack);
        
        // Login Credentials Card
        CreateCredentialsCard(formStack);
        
        // Login Button with Progress
        CreateLoginButton(formStack);
        
        // Social Login Section
        CreateSocialLoginSection(formStack);
        
        // Forgot Password Link
        CreateForgotPasswordLink(formStack);

        Grid.SetRow(formStack, row);
        parentGrid.Children.Add(formStack);
    }

    private void CreateErrorMessage(StackLayout parent)
    {
        var errorFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#E63946"),
            CornerRadius = 12,
            Padding = new Thickness(20, 15),
            BorderColor = Colors.Transparent,
            HasShadow = false,
            Content = new Label
            {
                TextColor = Colors.White,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };

        _errorLabel = (Label)errorFrame.Content;

        // Binding pour l'erreur
        errorFrame.SetBinding(IsVisibleProperty, nameof(LoginViewModel.HasLoginError));
        _errorLabel.SetBinding(Label.TextProperty, nameof(LoginViewModel.LoginErrorMessage));

        parent.Children.Add(errorFrame);
    }

    private void CreateCredentialsCard(StackLayout parent)
    {
        var credentialsFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 12,
            HasShadow = false,
            Padding = new Thickness(25, 30),
            Margin = new Thickness(0, 10)
        };

        var credentialsStack = new StackLayout
        {
            Spacing = 25
        };

        // Email Field
        CreateEmailField(credentialsStack);
        
        // Password Field
        CreatePasswordField(credentialsStack);
        
        // Remember Me
        CreateRememberMeSection(credentialsStack);

        credentialsFrame.Content = credentialsStack;
        parent.Children.Add(credentialsFrame);
    }

    private void CreateEmailField(StackLayout parent)
    {
        var emailStack = new StackLayout
        {
            Spacing = 10
        };

        var emailLabelStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 8
        };

        var emailLabel = new Label
        {
            Text = "Adresse email",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333"),
            VerticalOptions = LayoutOptions.Center
        };

        var emailValidIcon = new Label
        {
            Text = "✓",
            FontSize = 14,
            TextColor = Color.FromArgb("#28a745"),
            VerticalOptions = LayoutOptions.Center
        };
        emailValidIcon.SetBinding(Label.IsVisibleProperty, nameof(LoginViewModel.IsEmailValid));

        emailLabelStack.Children.Add(emailLabel);
        emailLabelStack.Children.Add(emailValidIcon);

        var emailFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(0)
        };

        _emailEntry = new Entry
        {
            Placeholder = "votre@email.com",
            Keyboard = Keyboard.Email,
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(20, 0),
            HeightRequest = 50,
            ReturnType = ReturnType.Next
        };

        _emailEntry.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Email));

        // Visual feedback for valid email
        _emailEntry.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == Entry.TextProperty.PropertyName)
            {
                if (_viewModel.IsEmailValid)
                {
                    emailFrame.BorderColor = Color.FromArgb("#28a745");
                }
                else
                {
                    emailFrame.BorderColor = Color.FromArgb("#C8C8C8");
                }
            }
        };

        emailFrame.Content = _emailEntry;

        emailStack.Children.Add(emailLabelStack);
        emailStack.Children.Add(emailFrame);

        parent.Children.Add(emailStack);
    }

    private void CreatePasswordField(StackLayout parent)
    {
        var passwordStack = new StackLayout
        {
            Spacing = 10
        };

        var passwordLabelStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 8
        };

        var passwordLabel = new Label
        {
            Text = "Mot de passe",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333"),
            VerticalOptions = LayoutOptions.Center
        };

        var passwordValidIcon = new Label
        {
            Text = "✓",
            FontSize = 14,
            TextColor = Color.FromArgb("#28a745"),
            VerticalOptions = LayoutOptions.Center
        };
        passwordValidIcon.SetBinding(Label.IsVisibleProperty, nameof(LoginViewModel.IsPasswordValid));

        passwordLabelStack.Children.Add(passwordLabel);
        passwordLabelStack.Children.Add(passwordValidIcon);

        var passwordFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(0)
        };

        var passwordGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        _passwordEntry = new Entry
        {
            Placeholder = "Minimum 8 caractères",
            IsPassword = true,
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(20, 0, 0, 0),
            HeightRequest = 50,
            ReturnType = ReturnType.Go
        };

        _passwordEntry.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Password));
        _passwordEntry.SetBinding(Entry.IsPasswordProperty, nameof(LoginViewModel.IsPasswordVisible), converter: new InvertBoolConverter());

        _togglePasswordButton = new Button
        {
            Text = "👁️",
            FontSize = 18,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6E6E6E"),
            Padding = new Thickness(15)
        };
        _togglePasswordButton.SetBinding(Button.CommandProperty, nameof(LoginViewModel.TogglePasswordVisibilityCommand));

        // Visual feedback for valid password
        _passwordEntry.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == Entry.TextProperty.PropertyName)
            {
                if (_viewModel.IsPasswordValid)
                {
                    passwordFrame.BorderColor = Color.FromArgb("#28a745");
                }
                else
                {
                    passwordFrame.BorderColor = Color.FromArgb("#C8C8C8");
                }
            }
        };

        Grid.SetColumn(_passwordEntry, 0);
        Grid.SetColumn(_togglePasswordButton, 1);

        passwordGrid.Children.Add(_passwordEntry);
        passwordGrid.Children.Add(_togglePasswordButton);

        passwordFrame.Content = passwordGrid;

        passwordStack.Children.Add(passwordLabelStack);
        passwordStack.Children.Add(passwordFrame);

        parent.Children.Add(passwordStack);
    }

    private void CreateRememberMeSection(StackLayout parent)
    {
        var rememberMeStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 5, 0, 0)
        };

        _rememberMeCheckBox = new CheckBox
        {
            Color = Color.FromArgb("#006994")
        };
        _rememberMeCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(LoginViewModel.RememberMe));

        _rememberMeLabel = new Label
        {
            Text = "Se souvenir de moi sur cet appareil",
            FontSize = 14,
            TextColor = Color.FromArgb("#666666"),
            VerticalOptions = LayoutOptions.Center
        };

        // Make label clickable
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += (s, e) =>
        {
            _rememberMeCheckBox.IsChecked = !_rememberMeCheckBox.IsChecked;
        };
        _rememberMeLabel.GestureRecognizers.Add(tapGestureRecognizer);

        rememberMeStack.Children.Add(_rememberMeCheckBox);
        rememberMeStack.Children.Add(_rememberMeLabel);

        parent.Children.Add(rememberMeStack);
    }

    private void CreateLoginButton(StackLayout parent)
    {
        var loginStack = new StackLayout
        {
            Spacing = 8
        };

        // Progress Bar
        var progressBar = new ProgressBar
        {
            ProgressColor = Color.FromArgb("#006994"),
            BackgroundColor = Color.FromArgb("#E0E0E0"),
            HeightRequest = 4
        };
        progressBar.SetBinding(ProgressBar.ProgressProperty, nameof(LoginViewModel.LoginProgress));
        progressBar.SetBinding(ProgressBar.IsVisibleProperty, nameof(LoginViewModel.IsLoginInProgress));

        _loginButton = new Button
        {
            Text = "Se connecter",
            BackgroundColor = Color.FromArgb("#006994"),
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 55,
            Margin = new Thickness(0, 5)
        };

        _loginButton.SetBinding(Button.CommandProperty, nameof(LoginViewModel.LoginCommand));
        _loginButton.SetBinding(Button.IsEnabledProperty, nameof(LoginViewModel.CanLogin));

        // Dynamic button text and color
        _loginButton.PropertyChanged += (s, e) =>
        {
            if (_viewModel.IsLoginInProgress)
            {
                _loginButton.Text = "⏳ Connexion...";
                _loginButton.BackgroundColor = Color.FromArgb("#4A90A4");
            }
            else
            {
                _loginButton.Text = "Se connecter";
                _loginButton.BackgroundColor = _viewModel.CanLogin ? Color.FromArgb("#006994") : Color.FromArgb("#CCCCCC");
            }
        };

        loginStack.Children.Add(progressBar);
        loginStack.Children.Add(_loginButton);

        parent.Children.Add(loginStack);
    }

    private void CreateSocialLoginSection(StackLayout parent)
    {
        // Divider
        var dividerStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 15,
            Margin = new Thickness(0, 30, 0, 20)
        };

        var leftLine = new BoxView
        {
            BackgroundColor = Color.FromArgb("#ACACAC"),
            HeightRequest = 1,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        var orLabel = new Label
        {
            Text = "ou",
            FontSize = 14,
            TextColor = Color.FromArgb("#6E6E6E"),
            VerticalOptions = LayoutOptions.Center
        };

        var rightLine = new BoxView
        {
            BackgroundColor = Color.FromArgb("#ACACAC"),
            HeightRequest = 1,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        dividerStack.Children.Add(leftLine);
        dividerStack.Children.Add(orLabel);
        dividerStack.Children.Add(rightLine);

        // Social Login Buttons
        var socialStack = new StackLayout
        {
            Spacing = 15
        };

        _googleLoginButton = new Button
        {
            Text = "🔍 Continuer avec Google",
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#333333"),
            FontSize = 15,
            CornerRadius = 12,
            HeightRequest = 50,
            BorderColor = Color.FromArgb("#C8C8C8"),
            BorderWidth = 1
        };

        _appleLoginButton = new Button
        {
            Text = "🍎 Continuer avec Apple",
            BackgroundColor = Color.FromArgb("#000000"),
            TextColor = Colors.White,
            FontSize = 15,
            CornerRadius = 12,
            HeightRequest = 50
        };

        // Only show Apple login on iOS
        _appleLoginButton.IsVisible = DeviceInfo.Platform == DevicePlatform.iOS;

        socialStack.Children.Add(_googleLoginButton);
        if (_appleLoginButton.IsVisible)
        {
            socialStack.Children.Add(_appleLoginButton);
        }

        parent.Children.Add(dividerStack);
        parent.Children.Add(socialStack);
    }

    private void CreateForgotPasswordLink(StackLayout parent)
    {
        _forgotPasswordButton = new Button
        {
            Text = "Mot de passe oublié ?",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#006994"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(0),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 15, 0, 0)
        };

        _forgotPasswordButton.SetBinding(Button.CommandProperty, nameof(LoginViewModel.NavigateToForgotPasswordCommand));

        parent.Children.Add(_forgotPasswordButton);
    }

    private void CreateFooterSection(Grid parentGrid, int row)
    {
        var footerStack = new StackLayout
        {
            Spacing = 15,
            Margin = new Thickness(0, 40, 0, 20)
        };

        // Registration Link
        var registrationStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8
        };

        var noAccountLabel = new Label
        {
            Text = "Pas encore de compte ?",
            FontSize = 15,
            TextColor = Color.FromArgb("#666666"),
            VerticalOptions = LayoutOptions.Center
        };

        _registerButton = new Button
        {
            Text = "Créer un compte",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#006994"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(0),
            VerticalOptions = LayoutOptions.Center
        };

        _registerButton.SetBinding(Button.CommandProperty, nameof(LoginViewModel.NavigateToRegistrationCommand));

        registrationStack.Children.Add(noAccountLabel);
        registrationStack.Children.Add(_registerButton);

        // Version Info
        var versionLabel = new Label
        {
            Text = "SubExplore v1.0 - Explorez en toute sécurité",
            FontSize = 12,
            TextColor = Color.FromArgb("#919191"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 15, 0, 0)
        };

        footerStack.Children.Add(registrationStack);
        footerStack.Children.Add(versionLabel);

        Grid.SetRow(footerStack, row);
        parentGrid.Children.Add(footerStack);
    }

    private void SetupEventHandlers()
    {
        // Handle Enter key on password field
        _passwordEntry.Completed += async (s, e) =>
        {
            if (_viewModel.CanLogin && _viewModel.LoginCommand.CanExecute(null))
            {
                await _viewModel.LoginCommand.ExecuteAsync(null);
            }
        };

        // Focus management
        _emailEntry.Completed += (s, e) => _passwordEntry.Focus();

        // Social login handlers
        _googleLoginButton.Clicked += OnGoogleLoginClicked;
        _appleLoginButton.Clicked += OnAppleLoginClicked;
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // TODO: Implement Google OAuth
            await DisplayAlert("Google Login", 
                "Connexion Google sera implémentée dans une future version", 
                "D'accord");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CompleteLoginPage] Google login error: {ex.Message}");
        }
    }

    private async void OnAppleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // TODO: Implement Apple Sign In
            await DisplayAlert("Apple Sign In", 
                "Connexion Apple sera implémentée dans une future version", 
                "D'accord");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CompleteLoginPage] Apple login error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Auto-focus email field
        Device.BeginInvokeOnMainThread(() =>
        {
            _emailEntry?.Focus();
        });
    }
}

// Converter Helper
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return !(bool)value;
    }
}