# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SubExplore is a .NET MAUI cross-platform mobile application for underwater sports community (diving, freediving, snorkeling). The app allows users to discover, share, and manage underwater exploration spots with safety features and community validation.

## Build and Development Commands

### Build Commands
```bash
# Build the solution
dotnet build

# Build for specific platform
dotnet build -f net8.0-android
dotnet build -f net8.0-ios
dotnet build -f net8.0-maccatalyst
dotnet build -f net8.0-windows10.0.19041.0

# Clean build artifacts
dotnet clean
```

### Running the Application
```bash
# Run on Android emulator
dotnet run -f net8.0-android

# Run on iOS simulator
dotnet run -f net8.0-ios

# Run on Windows
dotnet run -f net8.0-windows10.0.19041.0
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## Architecture Overview

### Core Structure
- **MVVM Pattern**: Uses CommunityToolkit.Mvvm for ViewModels with ObservableObject base class
- **Dependency Injection**: Full DI container configuration in MauiProgram.cs
- **Repository Pattern**: Generic repository with specific implementations for each entity
- **Entity Framework Core**: MySQL database with Pomelo provider
- **Clean Architecture**: Separation of concerns with Models, Services, Repositories, ViewModels, and Views

### Key Components

#### Database Layer
- **SubExploreDbContext**: Main EF Core context with MySQL configuration
- **Models/Domain**: Core entities (Spot, User, SpotMedia, SpotType, UserPreferences)
- **Repositories**: Generic and specific repository implementations with async patterns

#### Service Layer
- **ILocationService**: GPS and mapping functionality
- **IMediaService**: Photo/video handling for spots
- **IDatabaseService**: Database operations and migrations
- **INavigationService**: MAUI Shell navigation with ViewModel-based routing
- **IDialogService**: Platform-specific alerts, confirmations, and toasts
- **ISettingsService**: User preferences and app configuration
- **IConnectivityService**: Network connectivity management

#### Presentation Layer
- **ViewModelBase**: Base class with common properties (IsLoading, IsError, Title)
- **Views**: XAML pages with code-behind, organized by feature (Map, Spots, Settings)
- **Components**: Reusable UI components for spot creation workflow

### Database Configuration
- **Multi-Platform Connection Strings**: Different connection strings for Android emulator, physical devices, iOS simulator, and Windows
- **Platform Detection**: Automatic connection string selection based on device type
- **MySQL Backend**: Uses Pomelo.EntityFrameworkCore.MySql provider

### Navigation Structure
- **Shell-based Navigation**: Uses MAUI Shell with disabled flyout
- **ViewModel-First Navigation**: Navigation service routes to ViewModels, not pages directly
- **Dependency Injection**: All pages and ViewModels registered in DI container

## Key Features

### Spot Management
- **Multi-Step Creation**: Spot creation through dedicated components (Location, Characteristics, Photos)
- **Validation System**: Community-based spot validation with safety review system
- **Geographic Features**: Latitude/longitude with precision, depth measurements, current strength
- **Safety Integration**: Required equipment, safety notes, best conditions, and safety flags

### Media Handling
- **Photo Management**: Support for multiple photos per spot with size limits
- **Platform-Specific**: Media service handles platform differences for camera/gallery access

### Maps Integration
- **Microsoft.Maui.Maps**: Integrated mapping with spot visualization
- **Custom Converters**: Coordinate-to-position converters for map display
- **Location Services**: Real-time location tracking and spot positioning

## Development Guidelines

### ViewModels
- Inherit from ViewModelBase for common functionality
- Use CommunityToolkit.Mvvm attributes ([ObservableProperty], [RelayCommand])
- Implement InitializeAsync for page initialization with parameters
- Use async/await patterns for all service calls

### Database Operations
- Always use repository pattern, never direct DbContext access
- Implement proper async patterns with CancellationToken support
- Use transactions for multi-entity operations
- Follow EF Core best practices for relationship configuration

### Platform-Specific Code
- Use conditional compilation (#if ANDROID, #if IOS, etc.)
- Platform-specific implementations registered in MauiProgram.cs
- Database connection strings automatically selected based on platform detection

### Navigation
- Use NavigationService.NavigateToAsync<TViewModel>() for type-safe navigation
- Pass parameters through NavigationService, not direct page construction
- Implement proper back navigation handling in ViewModels

## Configuration Files

### appsettings.json
Contains platform-specific database connection strings and app configuration:
- Connection strings for different platforms and environments
- API base URLs and cache settings
- Default map coordinates and zoom levels
- Maximum photos per spot and other business rules

### MauiProgram.cs
Central dependency injection configuration:
- EF Core DbContext configuration with platform-specific connection strings
- Repository registrations (generic and specific)
- Service registrations (singleton vs scoped based on usage)
- ViewModel and View registrations for navigation

## Testing and Debugging

### Database Testing
- DatabaseTestPage and DatabaseTestViewModel for database connectivity testing
- Manual testing tools integrated into the app for development
- Connection string validation and fallback mechanisms

### Development Settings
- Debug logging enabled in development builds
- Comprehensive error handling with user-friendly messages
- Platform-specific debugging configurations in Properties/launchSettings.json