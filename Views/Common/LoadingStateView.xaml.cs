using Microsoft.Maui.Controls;

namespace SubExplore.Views.Common;

public partial class LoadingStateView : ContentView
{
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading),
        typeof(bool),
        typeof(LoadingStateView),
        false);

    public static readonly BindableProperty LoadingMessageProperty = BindableProperty.Create(
        nameof(LoadingMessage),
        typeof(string),
        typeof(LoadingStateView),
        "Chargement...");

    public static readonly BindableProperty IsErrorProperty = BindableProperty.Create(
        nameof(IsError),
        typeof(bool),
        typeof(LoadingStateView),
        false);

    public static readonly BindableProperty ErrorTitleProperty = BindableProperty.Create(
        nameof(ErrorTitle),
        typeof(string),
        typeof(LoadingStateView),
        "Erreur");

    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage),
        typeof(string),
        typeof(LoadingStateView),
        "Une erreur s'est produite");

    public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(
        nameof(RetryCommand),
        typeof(Command),
        typeof(LoadingStateView),
        null);

    public static readonly BindableProperty CanRetryProperty = BindableProperty.Create(
        nameof(CanRetry),
        typeof(bool),
        typeof(LoadingStateView),
        true);

    public static readonly BindableProperty IsEmptyProperty = BindableProperty.Create(
        nameof(IsEmpty),
        typeof(bool),
        typeof(LoadingStateView),
        false);

    public static readonly BindableProperty EmptyIconProperty = BindableProperty.Create(
        nameof(EmptyIcon),
        typeof(string),
        typeof(LoadingStateView),
        "🏊");

    public static readonly BindableProperty EmptyTitleProperty = BindableProperty.Create(
        nameof(EmptyTitle),
        typeof(string),
        typeof(LoadingStateView),
        "Aucun élément trouvé");

    public static readonly BindableProperty EmptyMessageProperty = BindableProperty.Create(
        nameof(EmptyMessage),
        typeof(string),
        typeof(LoadingStateView),
        "Aucun élément à afficher pour le moment");

    public static readonly BindableProperty EmptyActionTextProperty = BindableProperty.Create(
        nameof(EmptyActionText),
        typeof(string),
        typeof(LoadingStateView),
        "Actualiser");

    public static readonly BindableProperty EmptyActionCommandProperty = BindableProperty.Create(
        nameof(EmptyActionCommand),
        typeof(Command),
        typeof(LoadingStateView),
        null);

    public static readonly BindableProperty HasEmptyActionProperty = BindableProperty.Create(
        nameof(HasEmptyAction),
        typeof(bool),
        typeof(LoadingStateView),
        false);

    public LoadingStateView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string LoadingMessage
    {
        get => (string)GetValue(LoadingMessageProperty);
        set => SetValue(LoadingMessageProperty, value);
    }

    public bool IsError
    {
        get => (bool)GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    public string ErrorTitle
    {
        get => (string)GetValue(ErrorTitleProperty);
        set => SetValue(ErrorTitleProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public Command RetryCommand
    {
        get => (Command)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public bool CanRetry
    {
        get => (bool)GetValue(CanRetryProperty);
        set => SetValue(CanRetryProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public string EmptyIcon
    {
        get => (string)GetValue(EmptyIconProperty);
        set => SetValue(EmptyIconProperty, value);
    }

    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public string EmptyActionText
    {
        get => (string)GetValue(EmptyActionTextProperty);
        set => SetValue(EmptyActionTextProperty, value);
    }

    public Command EmptyActionCommand
    {
        get => (Command)GetValue(EmptyActionCommandProperty);
        set => SetValue(EmptyActionCommandProperty, value);
    }

    public bool HasEmptyAction
    {
        get => (bool)GetValue(HasEmptyActionProperty);
        set => SetValue(HasEmptyActionProperty, value);
    }
}