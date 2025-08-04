using Microsoft.Maui.Controls;
using SubExplore.Views.Map;
using SubExplore.Views.Auth;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using System.Diagnostics;

namespace SubExplore
{
    public partial class App : Application
    {
        // Option 1: Définir MainPage dans le constructeur APRES InitializeComponent
        // en utilisant le service provider qui devrait être disponible à ce stade.
        public App(IServiceProvider services) // MAUI peut injecter IServiceProvider ici
        {
            InitializeComponent();

            Debug.WriteLine("[App.xaml.cs] Constructeur App - Après InitializeComponent()");

            // PROPER AUTHENTICATION FLOW: Initialize authentication before setting MainPage
            try
            {
                Debug.WriteLine("[App.xaml.cs] Starting authentication-based initialization");
                
                // Set a loading page initially
                MainPage = new ContentPage
                {
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new ActivityIndicator { IsRunning = true, Color = Colors.Blue },
                            new Label 
                            { 
                                Text = "Initializing SubExplore...", 
                                HorizontalOptions = LayoutOptions.Center,
                                FontSize = 16
                            }
                        },
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 20
                    }
                };
                
                // Initialize authentication system and set appropriate MainPage
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await InitializeAuthenticationAndNavigationAsync(services);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App.xaml.cs] CRITICAL ERROR: {ex.Message}");
                Debug.WriteLine($"[App.xaml.cs] Stack trace: {ex.StackTrace}");
                
                // Last resort: Create a simple error page
                MainPage = new ContentPage 
                { 
                    Content = new Label 
                    { 
                        Text = $"Error: {ex.Message}", 
                        TextColor = Colors.Red,
                        FontSize = 16,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    } 
                };
            }
        }

        private async Task InitializeAuthenticationAndNavigationAsync(IServiceProvider services)
        {
            try
            {
                Debug.WriteLine("[App.xaml.cs] Initializing database and authentication services");
                
                // Apply username migration first to fix unique constraint issues
                try
                {
                    Debug.WriteLine("[App.xaml.cs] Applying username migration...");
                    await SubExplore.Helpers.UsernameMigrationHelper.ApplyUsernameMigrationAsync();
                    Debug.WriteLine("[App.xaml.cs] ✓ Username migration completed");
                }
                catch (Exception migrationEx)
                {
                    Debug.WriteLine($"[App.xaml.cs] ⚠️ Username migration warning: {migrationEx.Message}");
                    // Continue anyway - migration might already be applied
                }

                // Apply role hierarchy migration second
                try
                {
                    Debug.WriteLine("[App.xaml.cs] Applying role hierarchy migration...");
                    await MigrationHelper.ApplyMigrationsAsync();
                    Debug.WriteLine("[App.xaml.cs] ✓ Role hierarchy migration completed");
                }
                catch (Exception migrationEx)
                {
                    Debug.WriteLine($"[App.xaml.cs] ⚠️ Migration warning: {migrationEx.Message}");
                    // Continue anyway - migration might already be applied
                }

                // Apply IsEmailConfirmed migration to fix login issue
                try
                {
                    Debug.WriteLine("[App.xaml.cs] Applying IsEmailConfirmed migration...");
                    await SubExplore.Helpers.IsEmailConfirmedMigrationHelper.ApplyIsEmailConfirmedMigrationAsync();
                    Debug.WriteLine("[App.xaml.cs] ✓ IsEmailConfirmed migration completed");
                }
                catch (Exception migrationEx)
                {
                    Debug.WriteLine($"[App.xaml.cs] ⚠️ IsEmailConfirmed migration warning: {migrationEx.Message}");
                    // Continue anyway - migration might already be applied
                }

                // Apply admin spot validation migration to fix existing admin spots
                try
                {
                    Debug.WriteLine("[App.xaml.cs] Applying admin spot validation migration using AdminSpotMigrationHelper...");
                    await SubExplore.Helpers.AdminSpotMigrationHelper.MigrateAdminSpotsToApprovedAsync();
                    Debug.WriteLine("[App.xaml.cs] ✓ Admin spot validation migration completed successfully");
                }
                catch (Exception migrationEx)
                {
                    Debug.WriteLine($"[App.xaml.cs] ⚠️ Admin spot migration warning: {migrationEx.Message}");
                    // Continue anyway - migration might already be applied
                }

                // Initialize database first with enhanced error handling
                var dbInitService = services.GetService<IDatabaseInitializationService>();
                if (dbInitService != null)
                {
                    try
                    {
                        await dbInitService.InitializeDatabaseAsync();
                        Debug.WriteLine("[App.xaml.cs] Database initialization completed");
                        
                        // Verify critical tables exist
                        var isInitialized = await dbInitService.IsDatabaseInitializedAsync();
                        Debug.WriteLine($"[App.xaml.cs] Database verification: {(isInitialized ? "✓ READY" : "✗ FAILED")}");
                        
                        if (!isInitialized)
                        {
                            Debug.WriteLine("[App.xaml.cs] 🚨 CRITICAL: Database not properly initialized - forcing table creation");
                            await dbInitService.EnsureUserFavoriteSpotsTableAsync();
                        }
                        
                        // Database initialization and verification completed
                        Debug.WriteLine("[App.xaml.cs] ✅ Database initialization and verification completed successfully");
                        
                        // Initialize test data for spot validation workflow
                        try
                        {
                            var testDataService = services.GetService<TestDataService>();
                            if (testDataService != null)
                            {
                                await testDataService.CreateTestSpotsAsync();
                                Debug.WriteLine("[App.xaml.cs] ✅ Test data initialization completed");
                            }
                        }
                        catch (Exception testDataEx)
                        {
                            Debug.WriteLine($"[App.xaml.cs] ⚠️ Test data initialization warning: {testDataEx.Message}");
                            // Continue anyway - not critical for app functionality
                        }
                        
                        // Run ultra-deep database diagnostic
                        await DatabaseDiagnostic.RunUltraDeepDatabaseTestAsync(services);
                    }
                    catch (Exception dbEx)
                    {
                        Debug.WriteLine($"[App.xaml.cs] 🚨 DATABASE INITIALIZATION FAILED: {dbEx.Message}");
                        Debug.WriteLine($"[App.xaml.cs] Database Stack Trace: {dbEx.StackTrace}");
                        // Continue anyway - some functionality might still work
                    }
                }
                
                var authService = services.GetService<IAuthenticationService>();
                if (authService != null)
                {
                    await authService.InitializeAsync();
                    Debug.WriteLine($"[App.xaml.cs] Authentication service initialized. IsAuthenticated: {authService.IsAuthenticated}");
                    
                    // Determine initial page based on authentication status
                    if (authService.IsAuthenticated)
                    {
                        Debug.WriteLine("[App.xaml.cs] User is authenticated, setting AppShell as MainPage");
                        MainPage = new AppShell();
                        Debug.WriteLine("[App.xaml.cs] ✓ AppShell set as MainPage - Shell navigation enabled");
                    }
                    else
                    {
                        Debug.WriteLine("[App.xaml.cs] User not authenticated, showing LoginPage");
                        ShowLoginPage(services);
                    }
                }
                else
                {
                    Debug.WriteLine("[App.xaml.cs] ERROR: Authentication service not found, showing LoginPage");
                    // If authentication service is not available, show login page
                    ShowLoginPage(services);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App.xaml.cs] ERROR: Failed to initialize authentication and navigation: {ex.Message}");
                Debug.WriteLine($"[App.xaml.cs] Stack trace: {ex.StackTrace}");
                
                // Show login page as fallback
                Debug.WriteLine("[App.xaml.cs] Showing login page as error fallback");
                ShowLoginPage(services);
            }
        }

        private void ShowLoginPage(IServiceProvider services)
        {
            try
            {
                Debug.WriteLine("[App.xaml.cs] === DETAILED LOGIN PAGE DIAGNOSIS ===");
                
                // Test each service individually
                var loginViewModel = services.GetService<SubExplore.ViewModels.Auth.LoginViewModel>();
                Debug.WriteLine($"[App.xaml.cs] LoginViewModel: {(loginViewModel != null ? "✓ Found" : "✗ Not found")}");
                
                var loginPageInstance = services.GetService<LoginPage>();
                Debug.WriteLine($"[App.xaml.cs] LoginPage: {(loginPageInstance != null ? "✓ Found" : "✗ Not found")}");
                
                var authService = services.GetService<IAuthenticationService>();
                Debug.WriteLine($"[App.xaml.cs] AuthenticationService: {(authService != null ? "✓ Found" : "✗ Not found")}");
                
                var dialogService = services.GetService<IDialogService>();
                Debug.WriteLine($"[App.xaml.cs] DialogService: {(dialogService != null ? "✓ Found" : "✗ Not found")}");
                
                var navService = services.GetService<INavigationService>();
                Debug.WriteLine($"[App.xaml.cs] NavigationService: {(navService != null ? "✓ Found" : "✗ Not found")}");
                
                // Try to create the beautiful LoginPage
                if (loginViewModel != null)
                {
                    Debug.WriteLine("[App.xaml.cs] Creating MinimalLoginPage - ultra-simple to avoid performance issues");
                    try
                    {
                        // Use the minimal login page to avoid OpenGL/rendering performance issues
                        var minimalLoginPage = new SubExplore.Views.Auth.MinimalLoginPage(loginViewModel);
                        Debug.WriteLine("[App.xaml.cs] MinimalLoginPage created successfully");
                        
                        // For authentication, use NavigationPage temporarily
                        // After login success, we'll switch to AppShell
                        MainPage = new NavigationPage(minimalLoginPage);
                        Debug.WriteLine("[App.xaml.cs] ✓ MinimalLoginPage set as MainPage successfully (temp NavigationPage)");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App.xaml.cs] Original LoginPage failed: {ex.Message}");
                        Debug.WriteLine($"[App.xaml.cs] Original LoginPage error details: {ex.StackTrace}");
                        
                        // Fallback to DiagnosticLoginPage
                        try
                        {
                            var diagnosticLoginPage = new SubExplore.Views.Auth.DiagnosticLoginPage(loginViewModel);
                            Debug.WriteLine("[App.xaml.cs] DiagnosticLoginPage created successfully as fallback");
                            
                            MainPage = new NavigationPage(diagnosticLoginPage);
                            Debug.WriteLine("[App.xaml.cs] ✓ DiagnosticLoginPage set as MainPage successfully");
                        }
                        catch (Exception diagEx)
                        {
                            Debug.WriteLine($"[App.xaml.cs] DiagnosticLoginPage also failed: {diagEx.Message}");
                            
                            // Fallback to SimpleLoginPage
                            Debug.WriteLine("[App.xaml.cs] Falling back to SimpleLoginPage");
                            try
                            {
                                var simpleLoginPage = new SubExplore.Views.Auth.SimpleLoginPage(loginViewModel);
                                Debug.WriteLine("[App.xaml.cs] SimpleLoginPage created successfully");
                                
                                MainPage = new NavigationPage(simpleLoginPage);
                                Debug.WriteLine("[App.xaml.cs] ✓ SimpleLoginPage set as MainPage successfully");
                            }
                            catch (Exception simpleEx)
                            {
                                Debug.WriteLine($"[App.xaml.cs] ✗ SimpleLoginPage also failed: {simpleEx.Message}");
                                
                                // Last resort: Create basic manual login page
                                CreateBasicLoginPage();
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("[App.xaml.cs] ✗ Cannot create LoginPage - ViewModel not found");
                    MainPage = new ContentPage 
                    { 
                        Content = new StackLayout
                        {
                            Children =
                            {
                                new Label { Text = "Services d'authentification non trouvés", TextColor = Colors.Red, FontSize = 16 },
                                new Label { Text = $"LoginViewModel: {(loginViewModel != null ? "D'accord" : "MISSING")}", FontSize = 14 },
                                new Label { Text = $"AuthService: {(authService != null ? "D'accord" : "MISSING")}", FontSize = 14 },
                                new Label { Text = $"DialogService: {(dialogService != null ? "D'accord" : "MISSING")}", FontSize = 14 },
                                new Label { Text = $"NavigationService: {(navService != null ? "D'accord" : "MISSING")}", FontSize = 14 }
                            },
                            Padding = 20,
                            Spacing = 10
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App.xaml.cs] ✗ CRITICAL ERROR in ShowLoginPage: {ex.Message}");
                Debug.WriteLine($"[App.xaml.cs] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[App.xaml.cs] Inner exception: {ex.InnerException.Message}");
                }
                
                MainPage = new ContentPage 
                { 
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new Label { Text = "Erreur critique:", TextColor = Colors.Red, FontSize = 16, FontAttributes = FontAttributes.Bold },
                            new Label { Text = ex.Message, TextColor = Colors.Red, FontSize = 14 },
                            new Label { Text = "Voir les logs pour plus de détails", FontSize = 12, TextColor = Colors.Gray }
                        },
                        Padding = 20,
                        Spacing = 10
                    }
                };
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window = base.CreateWindow(activationState);

            window.Created += (s, e) =>
            {
                Debug.WriteLine("[App.xaml.cs] Window.Created event fired.");
                // La logique de configuration de la barre de navigation peut rester ici,
                // car elle s'applique à la MainPage déjà assignée.
                if (MainPage is NavigationPage navPage)
                {
                    Debug.WriteLine("[App.xaml.cs] MainPage is NavigationPage. Attempting to set BarBackgroundColor via Window.Created.");
                    try
                    {
                        if (Application.Current.Resources.TryGetValue("Primary", out var primaryColorResource) && primaryColorResource is Color castedPrimaryColor)
                        {
                            navPage.BarBackgroundColor = castedPrimaryColor;
                        }
                        else
                        {
                            Debug.WriteLine("[App.xaml.cs] ERREUR: Ressource 'Primary' non trouvée dans Window.Created. Utilisation d'une couleur par défaut pour la barre.");
                            navPage.BarBackgroundColor = Colors.SlateBlue;
                        }
                        navPage.BarTextColor = Colors.White;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App.xaml.cs] Exception dans Window.Created lors de la config de NavigationPage: {ex.Message}");
                        navPage.BarBackgroundColor = Colors.DarkGray;
                        navPage.BarTextColor = Colors.White;
                    }
                }
            };
            return window;
        }

        private void ShowSimpleTestPage(IServiceProvider services)
        {
            try
            {
                Debug.WriteLine("[App.xaml.cs] Creating SimpleTestPage for debugging");
                var testPage = new SubExplore.Views.Auth.SimpleTestPage(services);
                MainPage = new NavigationPage(testPage);
                Debug.WriteLine("[App.xaml.cs] ✓ SimpleTestPage set as MainPage successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App.xaml.cs] ✗ CRITICAL ERROR creating SimpleTestPage: {ex.Message}");
                
                // Last resort: basic error page
                CreateBasicLoginPage();
            }
        }

        private void CreateBasicLoginPage()
        {
            Debug.WriteLine("[App.xaml.cs] Creating basic manual login page");
            
            MainPage = new ContentPage 
            { 
                Title = "SubExplore Login",
                BackgroundColor = Colors.LightBlue,
                Content = new ScrollView
                {
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new Label 
                            { 
                                Text = "🌊 SubExplore", 
                                FontSize = 28, 
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.DarkBlue,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 50, 0, 30)
                            },
                            new Label 
                            { 
                                Text = "Connexion", 
                                FontSize = 20,
                                TextColor = Colors.DarkBlue,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 0, 0, 30)
                            },
                            new Entry 
                            { 
                                Placeholder = "admin@subexplore.com",
                                Text = "admin@subexplore.com",
                                FontSize = 16,
                                Margin = new Thickness(20, 10)
                            },
                            new Entry 
                            { 
                                Placeholder = "Admin123!",
                                Text = "Admin123!",
                                IsPassword = true,
                                FontSize = 16,
                                Margin = new Thickness(20, 10)
                            },
                            new Button 
                            { 
                                Text = "Se connecter",
                                BackgroundColor = Colors.DarkBlue,
                                TextColor = Colors.White,
                                FontSize = 16,
                                HeightRequest = 50,
                                Margin = new Thickness(20, 20)
                            },
                            new Label 
                            { 
                                Text = "Page de connexion basique - Fonctionnalité limitée",
                                FontSize = 12,
                                TextColor = Colors.Gray,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 30, 0, 0)
                            }
                        },
                        Padding = 20,
                        Spacing = 10
                    }
                }
            };
            
            Debug.WriteLine("[App.xaml.cs] ✓ Basic login page created successfully");
        }
    }
}