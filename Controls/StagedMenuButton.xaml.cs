using System.Windows.Input;

namespace SubExplore.Controls;

public partial class StagedMenuButton : ContentView
{
    public StagedMenuButton()
    {
        InitializeComponent();
        UpdateAccessibilityProperties();
        SetupKeyboardNavigation();
    }

    #region Bindable Properties

    // Title Properties
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), typeof(Color), typeof(StagedMenuButton), Colors.Black);

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public static readonly BindableProperty TitleFontSizeProperty = BindableProperty.Create(
        nameof(TitleFontSize), typeof(double), typeof(StagedMenuButton), 16.0);

    public double TitleFontSize
    {
        get => (double)GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public static readonly BindableProperty TitleFontAttributesProperty = BindableProperty.Create(
        nameof(TitleFontAttributes), typeof(FontAttributes), typeof(StagedMenuButton), FontAttributes.None);

    public FontAttributes TitleFontAttributes
    {
        get => (FontAttributes)GetValue(TitleFontAttributesProperty);
        set => SetValue(TitleFontAttributesProperty, value);
    }

    // Description Properties
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly BindableProperty DescriptionColorProperty = BindableProperty.Create(
        nameof(DescriptionColor), typeof(Color), typeof(StagedMenuButton), Colors.Gray);

    public Color DescriptionColor
    {
        get => (Color)GetValue(DescriptionColorProperty);
        set => SetValue(DescriptionColorProperty, value);
    }

    public static readonly BindableProperty DescriptionFontSizeProperty = BindableProperty.Create(
        nameof(DescriptionFontSize), typeof(double), typeof(StagedMenuButton), 12.0);

    public double DescriptionFontSize
    {
        get => (double)GetValue(DescriptionFontSizeProperty);
        set => SetValue(DescriptionFontSizeProperty, value);
    }

    public static readonly BindableProperty ShowDescriptionProperty = BindableProperty.Create(
        nameof(ShowDescription), typeof(bool), typeof(StagedMenuButton), true);

    public bool ShowDescription
    {
        get => (bool)GetValue(ShowDescriptionProperty);
        set => SetValue(ShowDescriptionProperty, value);
    }

    // Icon Properties
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor), typeof(Color), typeof(StagedMenuButton), Colors.Black);

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly BindableProperty ShowIconProperty = BindableProperty.Create(
        nameof(ShowIcon), typeof(bool), typeof(StagedMenuButton), false);

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    // Stage Properties
    public static readonly BindableProperty StageProperty = BindableProperty.Create(
        nameof(Stage), typeof(MenuButtonStage), typeof(StagedMenuButton), MenuButtonStage.Default,
        propertyChanged: OnStageChanged);

    public MenuButtonStage Stage
    {
        get => (MenuButtonStage)GetValue(StageProperty);
        set => SetValue(StageProperty, value);
    }

    public static readonly BindableProperty StageIndicatorProperty = BindableProperty.Create(
        nameof(StageIndicator), typeof(string), typeof(StagedMenuButton), "▶");

    public string StageIndicator
    {
        get => (string)GetValue(StageIndicatorProperty);
        set => SetValue(StageIndicatorProperty, value);
    }

    public static readonly BindableProperty StageIndicatorColorProperty = BindableProperty.Create(
        nameof(StageIndicatorColor), typeof(Color), typeof(StagedMenuButton), Colors.Gray);

    public Color StageIndicatorColor
    {
        get => (Color)GetValue(StageIndicatorColorProperty);
        set => SetValue(StageIndicatorColorProperty, value);
    }

    public static readonly BindableProperty ShowStageIndicatorProperty = BindableProperty.Create(
        nameof(ShowStageIndicator), typeof(bool), typeof(StagedMenuButton), true);

    public bool ShowStageIndicator
    {
        get => (bool)GetValue(ShowStageIndicatorProperty);
        set => SetValue(ShowStageIndicatorProperty, value);
    }

    // Badge Properties
    public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
        nameof(BadgeText), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public static readonly BindableProperty BadgeTextColorProperty = BindableProperty.Create(
        nameof(BadgeTextColor), typeof(Color), typeof(StagedMenuButton), Colors.White);

    public Color BadgeTextColor
    {
        get => (Color)GetValue(BadgeTextColorProperty);
        set => SetValue(BadgeTextColorProperty, value);
    }

    public static readonly BindableProperty BadgeBackgroundColorProperty = BindableProperty.Create(
        nameof(BadgeBackgroundColor), typeof(Color), typeof(StagedMenuButton), Colors.Red);

    public Color BadgeBackgroundColor
    {
        get => (Color)GetValue(BadgeBackgroundColorProperty);
        set => SetValue(BadgeBackgroundColorProperty, value);
    }

    public static readonly BindableProperty ShowBadgeProperty = BindableProperty.Create(
        nameof(ShowBadge), typeof(bool), typeof(StagedMenuButton), false);

    public bool ShowBadge
    {
        get => (bool)GetValue(ShowBadgeProperty);
        set => SetValue(ShowBadgeProperty, value);
    }

    // Button Styling Properties
    public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
        nameof(ButtonBackgroundColor), typeof(Color), typeof(StagedMenuButton), Colors.Transparent);

    public Color ButtonBackgroundColor
    {
        get => (Color)GetValue(ButtonBackgroundColorProperty);
        set => SetValue(ButtonBackgroundColorProperty, value);
    }

    public static readonly BindableProperty ButtonBorderColorProperty = BindableProperty.Create(
        nameof(ButtonBorderColor), typeof(Color), typeof(StagedMenuButton), Colors.Transparent);

    public Color ButtonBorderColor
    {
        get => (Color)GetValue(ButtonBorderColorProperty);
        set => SetValue(ButtonBorderColorProperty, value);
    }

    // Layout Properties
    public static readonly BindableProperty ContentMarginProperty = BindableProperty.Create(
        nameof(ContentMargin), typeof(Thickness), typeof(StagedMenuButton), new Thickness(10, 0, 10, 0));

    public Thickness ContentMargin
    {
        get => (Thickness)GetValue(ContentMarginProperty);
        set => SetValue(ContentMarginProperty, value);
    }

    // Command Properties
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(StagedMenuButton));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(StagedMenuButton));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    // Event
    public event EventHandler<StagedMenuButtonTappedEventArgs>? Tapped;

    // Accessibility Properties
    public static readonly BindableProperty AccessibilityNameProperty = BindableProperty.Create(
        nameof(AccessibilityName), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string AccessibilityName
    {
        get => (string)GetValue(AccessibilityNameProperty);
        set => SetValue(AccessibilityNameProperty, value);
    }

    public static readonly BindableProperty AccessibilityDescriptionProperty = BindableProperty.Create(
        nameof(AccessibilityDescription), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string AccessibilityDescription
    {
        get => (string)GetValue(AccessibilityDescriptionProperty);
        set => SetValue(AccessibilityDescriptionProperty, value);
    }

    public static readonly BindableProperty AccessibilityRoleProperty = BindableProperty.Create(
        nameof(AccessibilityRole), typeof(string), typeof(StagedMenuButton), "Button");

    public string AccessibilityRole
    {
        get => (string)GetValue(AccessibilityRoleProperty);
        set => SetValue(AccessibilityRoleProperty, value);
    }

    public static readonly BindableProperty AccessibilityHintProperty = BindableProperty.Create(
        nameof(AccessibilityHint), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string AccessibilityHint
    {
        get => (string)GetValue(AccessibilityHintProperty);
        set => SetValue(AccessibilityHintProperty, value);
    }

    public static readonly BindableProperty SemanticHeadingLevelProperty = BindableProperty.Create(
        nameof(SemanticHeadingLevel), typeof(SemanticHeadingLevel), typeof(StagedMenuButton), SemanticHeadingLevel.None);

    public SemanticHeadingLevel SemanticHeadingLevel
    {
        get => (SemanticHeadingLevel)GetValue(SemanticHeadingLevelProperty);
        set => SetValue(SemanticHeadingLevelProperty, value);
    }

    public static readonly BindableProperty TitleSemanticLevelProperty = BindableProperty.Create(
        nameof(TitleSemanticLevel), typeof(SemanticHeadingLevel), typeof(StagedMenuButton), SemanticHeadingLevel.Level3);

    public SemanticHeadingLevel TitleSemanticLevel
    {
        get => (SemanticHeadingLevel)GetValue(TitleSemanticLevelProperty);
        set => SetValue(TitleSemanticLevelProperty, value);
    }

    public static readonly BindableProperty IconAccessibilityTextProperty = BindableProperty.Create(
        nameof(IconAccessibilityText), typeof(string), typeof(StagedMenuButton), string.Empty);

    public string IconAccessibilityText
    {
        get => (string)GetValue(IconAccessibilityTextProperty);
        set => SetValue(IconAccessibilityTextProperty, value);
    }

    #endregion

    #region Event Handlers

    private void OnButtonTapped(object? sender, EventArgs e)
    {
        if (Stage == MenuButtonStage.Disabled)
            return;

        // Execute command if available
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        // Raise custom event
        Tapped?.Invoke(this, new StagedMenuButtonTappedEventArgs(Stage, Title));
    }

    private static void OnStageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is StagedMenuButton button && newValue is MenuButtonStage stage)
        {
            button.UpdateStageVisuals(stage);
            button.UpdateAccessibilityProperties();
        }
    }

    private void UpdateStageVisuals(MenuButtonStage stage)
    {
        var isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
        var isHighContrast = IsHighContrastMode();
        
        // Update text colors based on stage with high contrast support
        switch (stage)
        {
            case MenuButtonStage.Active:
                TitleColor = isHighContrast ? Colors.White : Colors.White;
                DescriptionColor = isHighContrast ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#CCFFFFFF");
                IconColor = isHighContrast ? Colors.White : Colors.Black;
                StageIndicatorColor = isHighContrast ? Colors.White : Colors.White;
                break;

            case MenuButtonStage.Warning:
                TitleColor = isHighContrast ? Colors.Black : Colors.White;
                DescriptionColor = isHighContrast ? Colors.Black : Color.FromArgb("#CCFFFFFF");
                IconColor = isHighContrast ? Colors.Black : Colors.Black;
                StageIndicatorColor = isHighContrast ? Colors.Black : Colors.White;
                break;

            case MenuButtonStage.Success:
                TitleColor = isHighContrast ? Colors.White : Colors.White;
                DescriptionColor = isHighContrast ? Colors.White : Color.FromArgb("#CCFFFFFF");
                IconColor = isHighContrast ? Colors.White : Colors.Black;
                StageIndicatorColor = isHighContrast ? Colors.White : Colors.White;
                break;

            case MenuButtonStage.Error:
                TitleColor = isHighContrast ? Colors.White : Colors.White;
                DescriptionColor = isHighContrast ? Colors.White : Color.FromArgb("#CCFFFFFF");
                IconColor = isHighContrast ? Colors.White : Colors.Black;
                StageIndicatorColor = isHighContrast ? Colors.White : Colors.White;
                break;

            case MenuButtonStage.Disabled:
                TitleColor = isHighContrast ? Color.FromArgb("#808080") : Colors.Gray;
                DescriptionColor = isHighContrast ? Color.FromArgb("#606060") : Colors.LightGray;
                IconColor = isHighContrast ? Color.FromArgb("#808080") : Colors.Gray;
                StageIndicatorColor = isHighContrast ? Color.FromArgb("#808080") : Colors.Gray;
                break;

            case MenuButtonStage.Default:
            default:
                // Use theme-based colors with high contrast support
                if (isHighContrast)
                {
                    TitleColor = isDarkTheme ? Colors.White : Colors.Black;
                    DescriptionColor = isDarkTheme ? Color.FromArgb("#CCCCCC") : Color.FromArgb("#333333");
                }
                else
                {
                    if (Application.Current?.Resources.TryGetValue("TextPrimary", out var primaryColor) == true && primaryColor is Color primary)
                        TitleColor = primary;
                    else
                        TitleColor = isDarkTheme ? Colors.White : Colors.Black;

                    if (Application.Current?.Resources.TryGetValue("TextSecondary", out var secondaryColor) == true && secondaryColor is Color secondary)
                        DescriptionColor = secondary;
                    else
                        DescriptionColor = Colors.Gray;
                }

                IconColor = TitleColor;
                StageIndicatorColor = DescriptionColor;
                break;
        }

        // Update stage indicator based on stage
        switch (stage)
        {
            case MenuButtonStage.Active:
                StageIndicator = "●";
                break;
            case MenuButtonStage.Warning:
                StageIndicator = "!";
                break;
            case MenuButtonStage.Success:
                StageIndicator = "✓";
                break;
            case MenuButtonStage.Error:
                StageIndicator = "✗";
                break;
            case MenuButtonStage.Disabled:
                StageIndicator = "○";
                break;
            case MenuButtonStage.Default:
            default:
                StageIndicator = "▶";
                break;
        }
    }

    private bool IsHighContrastMode()
    {
        // Check if high contrast mode is enabled
        // This is a simplified check - in a real app, you'd check system settings
        try
        {
            return Application.Current?.Resources.ContainsKey("HighContrast") == true &&
                   Application.Current.Resources.TryGetValue("HighContrast", out var value) &&
                   value is bool isHighContrast && isHighContrast;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateAccessibilityProperties()
    {
        // Update accessibility name if not explicitly set
        if (string.IsNullOrEmpty(AccessibilityName))
        {
            var stageName = Stage switch
            {
                MenuButtonStage.Active => "actif",
                MenuButtonStage.Warning => "attention",
                MenuButtonStage.Success => "succès",
                MenuButtonStage.Error => "erreur",
                MenuButtonStage.Disabled => "désactivé",
                _ => "défaut"
            };
            
            AccessibilityName = $"{Title}, état {stageName}";
        }

        // Update accessibility description if not explicitly set
        if (string.IsNullOrEmpty(AccessibilityDescription))
        {
            var description = !string.IsNullOrEmpty(Description) ? Description : "";
            
            if (ShowBadge && !string.IsNullOrEmpty(BadgeText))
            {
                description += $" Badge: {BadgeText}";
            }
            
            var actionHint = Stage == MenuButtonStage.Disabled ? "" : ", appuyez deux fois pour activer";
            AccessibilityDescription = $"{description}{actionHint}".Trim();
        }

        // Update icon accessibility text if not explicitly set
        if (string.IsNullOrEmpty(IconAccessibilityText) && !string.IsNullOrEmpty(Icon))
        {
            IconAccessibilityText = $"Icône {Icon}";
        }

        // Update accessibility hint based on stage
        if (string.IsNullOrEmpty(AccessibilityHint))
        {
            AccessibilityHint = Stage switch
            {
                MenuButtonStage.Warning => "Attention requise",
                MenuButtonStage.Error => "Erreur détectée",
                MenuButtonStage.Success => "Terminé avec succès",
                MenuButtonStage.Disabled => "Non disponible",
                _ => "Élément de menu"
            };
        }
    }

    private void SetupKeyboardNavigation()
    {
        // Add keyboard event handlers for the main frame
        this.Loaded += OnControlLoaded;
    }
    
    private void OnControlLoaded(object? sender, EventArgs e)
    {
        // Setup focus handling on the button frame
        var buttonFrame = this.FindByName<Frame>("ButtonFrame");
        if (buttonFrame != null)
        {
            buttonFrame.Focused += OnFocused;
            buttonFrame.Unfocused += OnUnfocused;
        }
    }

    private void OnFocused(object? sender, FocusEventArgs e)
    {
        // Update visual state for focus
        if (ButtonFrame != null)
        {
            ButtonFrame.BorderColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Colors.White 
                : Colors.Black;
            ButtonFrame.HasShadow = true;
        }
        
        // Announce focus for screen readers
        SemanticScreenReader.Announce($"Focus sur {AccessibilityName}");
    }

    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        // Reset visual state
        if (ButtonFrame != null)
        {
            ButtonFrame.BorderColor = ButtonBorderColor;
            ButtonFrame.HasShadow = false;
        }
    }


    #endregion
}

// Enums and Event Args
public enum MenuButtonStage
{
    Default,
    Active,
    Disabled,
    Warning,
    Success,
    Error
}

public class StagedMenuButtonTappedEventArgs : EventArgs
{
    public MenuButtonStage Stage { get; }
    public string Title { get; }

    public StagedMenuButtonTappedEventArgs(MenuButtonStage stage, string title)
    {
        Stage = stage;
        Title = title;
    }
}