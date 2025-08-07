using SubExplore.ViewModels.Auth;

namespace SubExplore.Views.Auth;

public class CompleteRegistrationPage : ContentPage
{
    private readonly RegistrationViewModel _viewModel;
    
    // UI Elements
    private Entry _firstNameEntry;
    private Entry _lastNameEntry;
    private Entry _emailEntry;
    private Entry _passwordEntry;
    private Entry _confirmPasswordEntry;
    private CheckBox _termsCheckBox;
    private CheckBox _newsletterCheckBox;
    private Button _registerButton;
    private Button _backToLoginButton;
    private Label _errorLabel;
    private ActivityIndicator _loadingIndicator;
    private Button _togglePasswordButton;
    private Button _toggleConfirmPasswordButton;

    public CompleteRegistrationPage(RegistrationViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[CompleteRegistrationPage] === CRÉATION PAGE D'INSCRIPTION COMPLÈTE ===");
            
            _viewModel = viewModel;
            BindingContext = viewModel;
            
            Title = "Créer un compte";
            BackgroundColor = Color.FromArgb("#F8FDFF");
            
            CreateCompleteRegistrationUI();
            SetupEventHandlers();
            
            // Initialize ViewModel
            Device.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.InitializeAsync();
            });
            
            System.Diagnostics.Debug.WriteLine("[CompleteRegistrationPage] ✅ Page d'inscription complète créée avec succès");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CompleteRegistrationPage] ❌ ERREUR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CompleteRegistrationPage] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void CreateCompleteRegistrationUI()
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
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 30)
        };

        // Back button
        var backButton = new Button
        {
            Text = "← Retour",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#006994"),
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Thickness(0)
        };
        backButton.Clicked += async (s, e) => await Navigation.PopAsync();

        // Title
        var titleLabel = new Label
        {
            Text = "Rejoignez SubExplore",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#006994"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var subtitleLabel = new Label
        {
            Text = "Créez votre compte pour découvrir les plus beaux spots de plongée",
            FontSize = 16,
            TextColor = Color.FromArgb("#666666"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        headerStack.Children.Add(backButton);
        headerStack.Children.Add(titleLabel);
        headerStack.Children.Add(subtitleLabel);

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
        
        // Registration Form Card
        CreateRegistrationCard(formStack);
        
        // Terms and Newsletter
        CreateTermsSection(formStack);
        
        // Register Button
        CreateRegisterButton(formStack);

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
        errorFrame.SetBinding(IsVisibleProperty, nameof(RegistrationViewModel.HasRegistrationError));
        _errorLabel.SetBinding(Label.TextProperty, nameof(RegistrationViewModel.RegistrationErrorMessage));

        parent.Children.Add(errorFrame);
    }

    private void CreateRegistrationCard(StackLayout parent)
    {
        var registrationFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 12,
            HasShadow = false,
            Padding = new Thickness(25, 30),
            Margin = new Thickness(0, 10)
        };

        var registrationStack = new StackLayout
        {
            Spacing = 25
        };

        // Name Fields
        CreateNameFields(registrationStack);
        
        // Email Field
        CreateEmailField(registrationStack);
        
        // Password Fields
        CreatePasswordFields(registrationStack);

        registrationFrame.Content = registrationStack;
        parent.Children.Add(registrationFrame);
    }

    private void CreateNameFields(StackLayout parent)
    {
        var nameGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(10) },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        // First Name
        var firstNameStack = new StackLayout
        {
            Spacing = 10
        };

        var firstNameLabel = new Label
        {
            Text = "Prénom",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

        var firstNameFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(0)
        };

        _firstNameEntry = new Entry
        {
            Placeholder = "Votre prénom",
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(15, 0),
            HeightRequest = 45,
            ReturnType = ReturnType.Next
        };
        _firstNameEntry.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.FirstName));

        firstNameFrame.Content = _firstNameEntry;
        firstNameStack.Children.Add(firstNameLabel);
        firstNameStack.Children.Add(firstNameFrame);

        // Last Name
        var lastNameStack = new StackLayout
        {
            Spacing = 10
        };

        var lastNameLabel = new Label
        {
            Text = "Nom",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

        var lastNameFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(0)
        };

        _lastNameEntry = new Entry
        {
            Placeholder = "Votre nom",
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(15, 0),
            HeightRequest = 45,
            ReturnType = ReturnType.Next
        };
        _lastNameEntry.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.LastName));

        lastNameFrame.Content = _lastNameEntry;
        lastNameStack.Children.Add(lastNameLabel);
        lastNameStack.Children.Add(lastNameFrame);

        Grid.SetColumn(firstNameStack, 0);
        Grid.SetColumn(lastNameStack, 2);

        nameGrid.Children.Add(firstNameStack);
        nameGrid.Children.Add(lastNameStack);

        parent.Children.Add(nameGrid);
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
        emailValidIcon.SetBinding(Label.IsVisibleProperty, nameof(RegistrationViewModel.IsEmailValid));

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
        _emailEntry.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.Email));

        emailFrame.Content = _emailEntry;

        emailStack.Children.Add(emailLabelStack);
        emailStack.Children.Add(emailFrame);

        parent.Children.Add(emailStack);
    }

    private void CreatePasswordFields(StackLayout parent)
    {
        // Password Field
        var passwordStack = new StackLayout
        {
            Spacing = 10
        };

        var passwordLabel = new Label
        {
            Text = "Mot de passe",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

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
            ReturnType = ReturnType.Next
        };
        _passwordEntry.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.Password));

        _togglePasswordButton = new Button
        {
            Text = "👁️",
            FontSize = 18,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6E6E6E"),
            Padding = new Thickness(15)
        };

        Grid.SetColumn(_passwordEntry, 0);
        Grid.SetColumn(_togglePasswordButton, 1);

        passwordGrid.Children.Add(_passwordEntry);
        passwordGrid.Children.Add(_togglePasswordButton);
        passwordFrame.Content = passwordGrid;

        passwordStack.Children.Add(passwordLabel);
        passwordStack.Children.Add(passwordFrame);

        // Confirm Password Field
        var confirmPasswordStack = new StackLayout
        {
            Spacing = 10
        };

        var confirmPasswordLabelStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 8
        };

        var confirmPasswordLabel = new Label
        {
            Text = "Confirmer le mot de passe",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        };

        var passwordMatchIcon = new Label
        {
            Text = "✓",
            FontSize = 14,
            TextColor = Color.FromArgb("#28a745")
        };
        passwordMatchIcon.SetBinding(Label.IsVisibleProperty, nameof(RegistrationViewModel.IsPasswordConfirmationValid));

        confirmPasswordLabelStack.Children.Add(confirmPasswordLabel);
        confirmPasswordLabelStack.Children.Add(passwordMatchIcon);

        var confirmPasswordFrame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#C8C8C8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(0)
        };

        var confirmPasswordGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        _confirmPasswordEntry = new Entry
        {
            Placeholder = "Confirmez votre mot de passe",
            IsPassword = true,
            FontSize = 16,
            TextColor = Color.FromArgb("#333333"),
            PlaceholderColor = Color.FromArgb("#919191"),
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(20, 0, 0, 0),
            HeightRequest = 50,
            ReturnType = ReturnType.Done
        };
        _confirmPasswordEntry.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.ConfirmPassword));

        _toggleConfirmPasswordButton = new Button
        {
            Text = "👁️",
            FontSize = 18,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6E6E6E"),
            Padding = new Thickness(15)
        };

        Grid.SetColumn(_confirmPasswordEntry, 0);
        Grid.SetColumn(_toggleConfirmPasswordButton, 1);

        confirmPasswordGrid.Children.Add(_confirmPasswordEntry);
        confirmPasswordGrid.Children.Add(_toggleConfirmPasswordButton);
        confirmPasswordFrame.Content = confirmPasswordGrid;

        confirmPasswordStack.Children.Add(confirmPasswordLabelStack);
        confirmPasswordStack.Children.Add(confirmPasswordFrame);

        // Password Requirements
        var requirementsLabel = new Label
        {
            Text = "• Au moins 8 caractères\n• Une lettre majuscule\n• Une lettre minuscule\n• Un chiffre\n• Un caractère spécial",
            FontSize = 12,
            TextColor = Color.FromArgb("#666666"),
            Margin = new Thickness(0, 5, 0, 0)
        };

        parent.Children.Add(passwordStack);
        parent.Children.Add(confirmPasswordStack);
        parent.Children.Add(requirementsLabel);
    }

    private void CreateTermsSection(StackLayout parent)
    {
        var termsStack = new StackLayout
        {
            Spacing = 15
        };

        // Terms and Conditions
        var termsCheckStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 12
        };

        _termsCheckBox = new CheckBox
        {
            Color = Color.FromArgb("#006994")
        };
        _termsCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(RegistrationViewModel.AcceptTermsAndConditions));

        var termsLabel = new Label
        {
            Text = "J'accepte les conditions d'utilisation et la politique de confidentialité",
            FontSize = 14,
            TextColor = Color.FromArgb("#333333"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        // Make clickable
        var termsTap = new TapGestureRecognizer();
        termsTap.Tapped += (s, e) => _termsCheckBox.IsChecked = !_termsCheckBox.IsChecked;
        termsLabel.GestureRecognizers.Add(termsTap);

        termsCheckStack.Children.Add(_termsCheckBox);
        termsCheckStack.Children.Add(termsLabel);

        // Newsletter
        var newsletterCheckStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 12
        };

        _newsletterCheckBox = new CheckBox
        {
            Color = Color.FromArgb("#006994")
        };
        _newsletterCheckBox.SetBinding(CheckBox.IsCheckedProperty, nameof(RegistrationViewModel.AcceptNewsletter));

        var newsletterLabel = new Label
        {
            Text = "Je souhaite recevoir des informations sur les nouveaux spots et les actualités",
            FontSize = 14,
            TextColor = Color.FromArgb("#666666"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        var newsletterTap = new TapGestureRecognizer();
        newsletterTap.Tapped += (s, e) => _newsletterCheckBox.IsChecked = !_newsletterCheckBox.IsChecked;
        newsletterLabel.GestureRecognizers.Add(newsletterTap);

        newsletterCheckStack.Children.Add(_newsletterCheckBox);
        newsletterCheckStack.Children.Add(newsletterLabel);

        termsStack.Children.Add(termsCheckStack);
        termsStack.Children.Add(newsletterCheckStack);

        parent.Children.Add(termsStack);
    }

    private void CreateRegisterButton(StackLayout parent)
    {
        var registerStack = new StackLayout
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
        progressBar.SetBinding(ProgressBar.ProgressProperty, nameof(RegistrationViewModel.RegistrationProgress));
        progressBar.SetBinding(ProgressBar.IsVisibleProperty, nameof(RegistrationViewModel.IsRegistrationInProgress));

        _registerButton = new Button
        {
            Text = "Créer mon compte",
            BackgroundColor = Color.FromArgb("#006994"),
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 55,
            Margin = new Thickness(0, 20, 0, 5)
        };

        _registerButton.SetBinding(Button.CommandProperty, nameof(RegistrationViewModel.RegisterCommand));
        _registerButton.SetBinding(Button.IsEnabledProperty, nameof(RegistrationViewModel.CanRegister));

        registerStack.Children.Add(progressBar);
        registerStack.Children.Add(_registerButton);

        parent.Children.Add(registerStack);
    }

    private void CreateFooterSection(Grid parentGrid, int row)
    {
        var footerStack = new StackLayout
        {
            Spacing = 15,
            Margin = new Thickness(0, 30, 0, 20)
        };

        // Back to Login Link
        var loginStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8
        };

        var hasAccountLabel = new Label
        {
            Text = "Vous avez déjà un compte ?",
            FontSize = 15,
            TextColor = Color.FromArgb("#666666"),
            VerticalOptions = LayoutOptions.Center
        };

        _backToLoginButton = new Button
        {
            Text = "Se connecter",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#006994"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(0),
            VerticalOptions = LayoutOptions.Center
        };

        loginStack.Children.Add(hasAccountLabel);
        loginStack.Children.Add(_backToLoginButton);

        footerStack.Children.Add(loginStack);

        Grid.SetRow(footerStack, row);
        parentGrid.Children.Add(footerStack);
    }

    private void SetupEventHandlers()
    {
        // Field navigation
        _firstNameEntry.Completed += (s, e) => _lastNameEntry.Focus();
        _lastNameEntry.Completed += (s, e) => _emailEntry.Focus();
        _emailEntry.Completed += (s, e) => _passwordEntry.Focus();
        _passwordEntry.Completed += (s, e) => _confirmPasswordEntry.Focus();

        _confirmPasswordEntry.Completed += async (s, e) =>
        {
            if (_viewModel.CanRegister && _viewModel.RegisterCommand.CanExecute(null))
            {
                await _viewModel.RegisterCommand.ExecuteAsync(null);
            }
        };

        // Password visibility toggles
        _togglePasswordButton.Clicked += (s, e) =>
        {
            _passwordEntry.IsPassword = !_passwordEntry.IsPassword;
        };

        _toggleConfirmPasswordButton.Clicked += (s, e) =>
        {
            _confirmPasswordEntry.IsPassword = !_confirmPasswordEntry.IsPassword;
        };

        // Back to login
        _backToLoginButton.Clicked += async (s, e) => await Navigation.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Auto-focus first name field
        Device.BeginInvokeOnMainThread(() =>
        {
            _firstNameEntry?.Focus();
        });
    }
}