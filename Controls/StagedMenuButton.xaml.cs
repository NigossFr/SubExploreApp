using System.Windows.Input;

namespace SubExplore.Controls;

public partial class StagedMenuButton : ContentView
{
    public StagedMenuButton()
    {
        InitializeComponent();
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
        }
    }

    private void UpdateStageVisuals(MenuButtonStage stage)
    {
        // Update text colors based on stage
        switch (stage)
        {
            case MenuButtonStage.Active:
                TitleColor = Colors.White;
                DescriptionColor = Color.FromArgb("#CCFFFFFF");
                IconColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.Black;
                StageIndicatorColor = Colors.White;
                break;

            case MenuButtonStage.Warning:
                TitleColor = Colors.White;
                DescriptionColor = Color.FromArgb("#CCFFFFFF");
                IconColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.Black;
                StageIndicatorColor = Colors.White;
                break;

            case MenuButtonStage.Success:
                TitleColor = Colors.White;
                DescriptionColor = Color.FromArgb("#CCFFFFFF");
                IconColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.Black;
                StageIndicatorColor = Colors.White;
                break;

            case MenuButtonStage.Error:
                TitleColor = Colors.White;
                DescriptionColor = Color.FromArgb("#CCFFFFFF");
                IconColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.Black;
                StageIndicatorColor = Colors.White;
                break;

            case MenuButtonStage.Disabled:
                TitleColor = Colors.Gray;
                DescriptionColor = Colors.LightGray;
                IconColor = Colors.Gray;
                StageIndicatorColor = Colors.Gray;
                break;

            case MenuButtonStage.Default:
            default:
                // Use theme-based colors
                if (Application.Current?.Resources.TryGetValue("TextPrimary", out var primaryColor) == true && primaryColor is Color primary)
                    TitleColor = primary;
                else
                    TitleColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black;

                if (Application.Current?.Resources.TryGetValue("TextSecondary", out var secondaryColor) == true && secondaryColor is Color secondary)
                    DescriptionColor = secondary;
                else
                    DescriptionColor = Colors.Gray;

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
                StageIndicator = "⚠";
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