using SubExplore.ViewModels.Base;

namespace SubExplore.Views.Common;

public partial class PageHeaderView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), 
        typeof(string), 
        typeof(PageHeaderView), 
        string.Empty);

    public static readonly BindableProperty ShowBackButtonProperty = BindableProperty.Create(
        nameof(ShowBackButton), 
        typeof(bool), 
        typeof(PageHeaderView), 
        false);

    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(
        nameof(BackCommand), 
        typeof(System.Windows.Input.ICommand), 
        typeof(PageHeaderView), 
        null);

    public static readonly BindableProperty ShowSearchBarProperty = BindableProperty.Create(
        nameof(ShowSearchBar), 
        typeof(bool), 
        typeof(PageHeaderView), 
        false);

    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(
        nameof(SearchText), 
        typeof(string), 
        typeof(PageHeaderView), 
        string.Empty);

    public static readonly BindableProperty SearchPlaceholderProperty = BindableProperty.Create(
        nameof(SearchPlaceholder), 
        typeof(string), 
        typeof(PageHeaderView), 
        "Rechercher...");

    public static readonly BindableProperty SearchCommandProperty = BindableProperty.Create(
        nameof(SearchCommand), 
        typeof(System.Windows.Input.ICommand), 
        typeof(PageHeaderView), 
        null);

    public static readonly BindableProperty SearchTextChangedCommandProperty = BindableProperty.Create(
        nameof(SearchTextChangedCommand), 
        typeof(System.Windows.Input.ICommand), 
        typeof(PageHeaderView), 
        null);

    public static readonly BindableProperty IsSearchingProperty = BindableProperty.Create(
        nameof(IsSearching), 
        typeof(bool), 
        typeof(PageHeaderView), 
        false);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public System.Windows.Input.ICommand BackCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public bool ShowSearchBar
    {
        get => (bool)GetValue(ShowSearchBarProperty);
        set => SetValue(ShowSearchBarProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public System.Windows.Input.ICommand SearchCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public System.Windows.Input.ICommand SearchTextChangedCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(SearchTextChangedCommandProperty);
        set => SetValue(SearchTextChangedCommandProperty, value);
    }

    public bool IsSearching
    {
        get => (bool)GetValue(IsSearchingProperty);
        set => SetValue(IsSearchingProperty, value);
    }

    public PageHeaderView()
    {
        InitializeComponent();
        BindingContext = this;
    }

}