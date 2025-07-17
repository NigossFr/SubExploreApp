using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Services.Interfaces;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace SubExplore.Services.Implementations
{
    public class DialogService : IDialogService
    {
        public Task ShowAlertAsync(string title, string message, string buttonText)
        {
            return Application.Current.Dispatcher.DispatchAsync(() =>
                Application.Current.MainPage.DisplayAlert(title, message, buttonText));
        }

        public Task<bool> ShowConfirmationAsync(string title, string message, string okText, string cancelText)
        {
            return Application.Current.Dispatcher.DispatchAsync(() =>
                Application.Current.MainPage.DisplayAlert(title, message, okText, cancelText));
        }

        public Task<string> ShowPromptAsync(string title, string message, string okText, string cancelText, string placeholder = "", string initialValue = "")
        {
            return Application.Current.Dispatcher.DispatchAsync(() =>
                Application.Current.MainPage.DisplayPromptAsync(title, message, okText, cancelText, placeholder, initialValue: initialValue));
        }

        public async Task ShowToastAsync(string message, int durationInSeconds = 2)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                var toast = Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
                await toast.Show(cancellationTokenSource.Token);
            });
        }

        public async Task<IDisposable> ShowLoadingAsync(string message = "Chargement...")
        {
            var loadingPage = new LoadingIndicator(message);
            await Application.Current.Dispatcher.DispatchAsync(async () =>
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(loadingPage, false);
            });

            return new LoadingDisposable(loadingPage);
        }

        private class LoadingDisposable : IDisposable
        {
            private Page _loadingPage;

            public LoadingDisposable(Page loadingPage)
            {
                _loadingPage = loadingPage;
            }

            public void Dispose()
            {
                if (_loadingPage != null)
                {
                    Application.Current.Dispatcher.Dispatch(async () =>
                    {
                        if (_loadingPage != null && Application.Current?.MainPage?.Navigation != null)
                        {
                            await Application.Current.MainPage.Navigation.PopModalAsync(false);
                            _loadingPage = null;
                        }
                    });
                }
            }
        }
    }

    // Cette classe représente la page d'indicateur de chargement
    public class LoadingIndicator : ContentPage
    {
        public LoadingIndicator(string message)
        {
            BackgroundColor = Color.FromArgb("#80000000");

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Frame
                    {
                        CornerRadius = 10,
                        BackgroundColor = Colors.White,
                        Content = new StackLayout
                        {
                            Padding = 20,
                            Spacing = 15,
                            Children =
                            {
                                new ActivityIndicator
                                {
                                    IsRunning = true,
                                    Color = Color.FromArgb("#006994"),
                                    HeightRequest = 50,
                                    WidthRequest = 50,
                                    HorizontalOptions = LayoutOptions.Center
                                },
                                new Label
                                {
                                    Text = message,
                                    HorizontalOptions = LayoutOptions.Center,
                                    FontSize = 16
                                }
                            }
                        }
                    }
                }
            };
        }
    }



}
