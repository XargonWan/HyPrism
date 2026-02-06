# Migration Guide: Photino → Avalonia UI

> **PR #299:** [Merge from Photino to AvaloniaUI](https://github.com/yyyumeniku/HyPrism/pull/299)  
> **Status:** In Progress (Draft)  
> **Changes:** +33,151 / -155,144 lines in 897 files

---

## Table of Contents

- [Migration Overview](#-migration-overview)
- [Architectural Changes](#-architectural-changes)
- [File Structure Changes](#-file-structure-changes)
- [AppService Refactoring](#-appservice-refactoring)
- [Dependency Injection](#-dependency-injection)
- [UI Changes](#-ui-changes)
- [Breaking Changes for Users](#-breaking-changes-for-users)
- [For Developers: What You Need to Know](#-for-developers-what-you-need-to-know)
- [Migration Status (PR #299)](#-migration-status-pr-299)

---

## 📋 Migration Overview

### What Was Replaced

| Component | Before (Photino) | After (Avalonia) |
|-----------|------------------|-------------------|
| **UI Framework** | Photino (WebKit) | Avalonia UI 11.3 |
| **Frontend** | HTML/CSS/TypeScript | XAML/C# |
| **Architecture** | SPA + IPC bridge | Native MVVM |
| **Rendering** | WebKit Engine | SkiaSharp |
| **State Management** | JavaScript + Bridge | ReactiveUI |

### Why Migration Was Done

1. **WebKit issues on Linux** — Issue #183 and many similar rendering problems on different distros
2. **Architectural complexity** — IPC bridge between C# and JavaScript created unnecessary complexity
3. **Performance** — WebKit consumed more memory and CPU
4. **Support** — Photino has limited community and less active development
5. **Codebase unification** — All code is now in C#, simplifying development

---

## 🏗️ Architectural Changes

### Before Migration (Photino)

```
┌─────────────────────────────────────────────────────┐
│                    Photino Window                   │
├─────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────┐    │
│  │           WebKit Browser Engine             │    │
│  │  ┌───────────────────────────────────────┐  │    │
│  │  │     HTML/CSS/TypeScript Frontend      │  │    │
│  │  │  (React-like components, SPA routing) │  │    │
│  │  └───────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────┘    │
│                        ↕ IPC Bridge                 │
│  ┌─────────────────────────────────────────────┐    │
│  │              C# Backend (AppService)        │    │
│  │    (Monolithic god-object with all logic)   │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

**Old architecture problems:**
- `AppService.cs` was a "God Object" with 3000+ lines of code
- IPC calls added latency
- Complex debugging (two runtimes: .NET + JavaScript)
- WebKit issues on some Linux distributions

### After Migration (Avalonia)

```
┌─────────────────────────────────────────────────────┐
│                   Avalonia Window                   │
├─────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────┐    │
│  │              Views (XAML)                   │    │
│  │         Pure declarative UI markup          │    │
│  └───────────────────┬─────────────────────────┘    │
│                      ↕ Data Binding                 │
│  ┌─────────────────────────────────────────────┐    │
│  │            ViewModels (ReactiveUI)          │    │
│  │      ObservableProperties + Commands        │    │
│  └───────────────────┬─────────────────────────┘    │
│                      ↓ DI                           │
│  ┌─────────────────────────────────────────────┐    │
│  │         Services (Single Responsibility)    │    │
│  │   GameSessionService, ConfigService, etc.   │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

**New architecture benefits:**
- Clean MVVM pattern
- Single responsibility services
- Dependency Injection via `Bootstrapper.cs`
- Native performance
- Single programming language (C#)

---

## 📁 File Structure Changes

### Removed Directories

```diff
- frontend/                  # All TypeScript/React code
- frontend/node_modules/     
- frontend/src/
- frontend/dist/
- wwwroot/                   # Static resources for WebKit
- Backend/                   # Renamed to Services/
```

### New Directories

```diff
+ Services/
+   Core/                    # Base services (Config, Logger, etc.)
+   Game/                    # Game services (Launch, Download, etc.)
+   User/                    # User services (Profile, Skin)
+ UI/
+   Components/              # Reusable UI components
+   Views/                   # Full-screen views
+   MainWindow/              # Main window and its ViewModel
+   Styles/                  # XAML styles
+   Converters/              # Value Converters
+ Assets/                    # Capitalized (Images, Icons, Locales)
```

### Renames

| Old | New | Reason |
|-----|-----|--------|
| `Backend/AppService.cs` | Split into 20+ services | Single Responsibility |
| `assets/` | `Assets/` | .NET conventions compliance |
| `scripts/` | `Scripts/` | Consistency |
| `packaging/` | `Packaging/` | Consistency |

---

## 🔄 AppService Refactoring

The monolithic `AppService.cs` (~3000 lines) was split into specialized services:

### Core Services (`Services/Core/`)

| Service | Responsibility |
|---------|----------------|
| `ConfigService` | Configuration read/write |
| `LocalizationService` | Localization system |
| `Logger` | Centralized logging |
| `ThemeService` | Theme and accent color management |
| `BrowserService` | Opening external links |
| `ProgressNotificationService` | Progress notifications |
| `FileService` | File operations |
| `GitHubService` | GitHub API operations |
| `NewsService` | News loading |
| `DiscordService` | Discord Rich Presence |

### Game Services (`Services/Game/`)

| Service | Responsibility |
|---------|----------------|
| `GameSessionService` | Game launch orchestration |
| `LaunchService` | Launch process construction |
| `DownloadService` | File downloads |
| `VersionService` | Game version management |
| `InstanceService` | Instance management |
| `ModService` | Mod management |
| `ClientPatcher` | Binary patching |
| `ButlerService` | itch.io Butler integration |
| `AssetService` | Asset management |

### User Services (`Services/User/`)

| Service | Responsibility |
|---------|----------------|
| `ProfileService` | Profile data |
| `ProfileManagementService` | High-level profile operations |
| `SkinService` | User skins |
| `UserIdentityService` | User identification |

---

## 💉 Dependency Injection

New DI system via `Bootstrapper.cs`:

```csharp
public static class Bootstrapper
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();
        
        // Infrastructure
        services.AddSingleton(new AppPathConfiguration(appDir));
        services.AddSingleton<HttpClient>();
        
        // Core Services
        services.AddSingleton<ConfigService>();
        services.AddSingleton<LocalizationService>();
        
        // Game Services
        services.AddSingleton<GameSessionService>();
        services.AddSingleton<LaunchService>();
        
        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        return services.BuildServiceProvider();
    }
}
```

### Getting Services

```csharp
// In App.axaml.cs
var mainVm = Services!.GetRequiredService<MainViewModel>();

// In ViewModel (via constructor)
public DashboardViewModel(GameSessionService gameSession, ...) 
{
    _gameSessionService = gameSession;
}
```

---

## 🎨 UI Changes

### Approach Comparison

| Aspect | Photino (Before) | Avalonia (After) |
|--------|------------------|-------------------|
| **Markup** | HTML + CSS | XAML |
| **Styles** | CSS files | XAML Styles + Resources |
| **Components** | TypeScript classes | UserControl + ViewModel |
| **Binding** | Manual via IPC | Native Data Binding |
| **Animations** | CSS animations | Avalonia Animations |
| **Icons** | `<img src="...">` | `<svg:Svg Path="..."/>` |

### Example: Button

**Before (HTML/CSS):**
```html
<button class="primary-button" onclick="bridge.launch()">
  <img src="icons/play.svg" />
  <span>Play</span>
</button>
```

**After (XAML):**
```xml
<Button Command="{Binding LaunchCommand}" Classes="Primary">
  <StackPanel Orientation="Horizontal">
    <svg:Svg Path="/Assets/Icons/play.svg" Width="16" Height="16"/>
    <TextBlock Text="{Binding PlayButtonText}"/>
  </StackPanel>
</Button>
```

---

## ⚠️ Breaking Changes for Users

### Configuration

- **Configuration path unchanged**: `%APPDATA%/HyPrism` (Windows), `~/.config/HyPrism` (Linux)
- **`config.json` format**: New fields added, old ones preserved for compatibility
- **Locales**: Migration from `.lang` to `.json` (folder `Assets/Locales/`)

### Visual Changes

- New interface design (Avalonia-native look)
- Improved animations and transitions
- System theme support (light/dark)

---

## 🔧 For Developers: What You Need to Know

### 1. No More JavaScript

All frontend code removed. If you previously worked with `frontend/`, everything is now in `UI/`.

### 2. MVVM is Mandatory

ViewModels must not reference Avalonia Controls directly:

```csharp
// ❌ WRONG
public class MyViewModel
{
    public Button MyButton { get; set; } // DON'T DO THIS!
}

// ✅ CORRECT
public class MyViewModel : ReactiveObject
{
    private string _buttonText = "Click me";
    public string ButtonText
    {
        get => _buttonText;
        set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }
    
    public ReactiveCommand<Unit, Unit> ButtonClickCommand { get; }
    
    public MyViewModel()
    {
        ButtonClickCommand = ReactiveCommand.Create(OnButtonClick);
    }
    
    private void OnButtonClick() { }
}
```

### 3. Use ReactiveUI Patterns

```csharp
// Property declaration with RaiseAndSetIfChanged
private string _name;
public string Name
{
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
}
```

### 4. Services via DI

Don't create services directly. Get them via constructor or `App.Current.Services`:

```csharp
// ❌ WRONG
var config = new ConfigService(appDir);

// ✅ CORRECT
public MyViewModel(ConfigService configService) 
{
    _configService = configService;
}
```

---

## 📊 Migration Status (PR #299)

### Completed ✅
- [x] `AppService.cs` refactoring into separate services
- [x] WebKit (Photino) dependency removal
- [x] Main page
- [x] Overlays
- [x] Mod menu (UI only)
- [x] News block
- [x] Brand logos
- [x] Localization
- [x] Themes/Colors
- [x] Build script updates

### In Progress 🔄
- [ ] First run screen
- [ ] Styling
- [ ] Settings menu
- [ ] Profile menu
- [ ] Discord RPC
- [ ] Client events

### Testing Required 🧪
- [ ] UI artifacts
- [ ] Background services
- [ ] Configuration services
- [ ] System dependencies
- [ ] Cross-platform operation
- [ ] Full application lifecycle

---

## 📚 Additional Resources

- [Architecture.md](Architecture.md) — New architecture
- [UIComponentGuide.md](../Development/UIComponentGuide.md) — Creating components
- [ServicesReference.md](ServicesReference.md) — Services reference
