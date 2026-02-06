# Breaking Changes

> List of breaking changes during migration from Photino to Avalonia UI (PR #299).

---

## Table of Contents

- [Overview](#-overview)
- [Critical Changes](#-critical-changes)
  - [1. Frontend Directory Removal](#1-frontend-directory-removal)
  - [2. Backend → Services Rename](#2-backend--services-rename)
  - [3. Dependency Injection](#3-dependency-injection)
  - [4. Configuration Changes](#4-configuration-changes)
  - [5. Localization](#5-localization)
  - [6. Assets Path](#6-assets-path)
  - [7. ViewModels](#7-viewmodels)
  - [8. Commands](#8-commands)
  - [9. Logging](#9-logging)
  - [10. UI Icons](#10-ui-icons)
- [Potential Issues](#-potential-issues)
- [Migration Script](#-migration-script)

---

## 📋 Overview

Migration to Avalonia UI includes significant architectural changes that may affect:
- Existing forks
- Custom modifications
- Build scripts
- Configuration files

---

## 🔴 Critical Changes

### 1. Frontend Directory Removal

**Was:** `frontend/` — TypeScript/React SPA  
**Now:** Completely removed

**Impact:**
- All custom JS/TS modifications are invalid
- Complete UI customization rework required

**Migration:**
- Study `UI/` directory
- Recreate components in Avalonia XAML
- See [UIComponentGuide.md](../Development/UIComponentGuide.md)

---

### 2. Backend → Services Rename

**Was:** `Backend/AppService.cs` — monolithic service  
**Now:** `Services/` — modular services

**Structure changes:**

```diff
- Backend/
-   AppService.cs
-   AuthService.cs
-   ClientPatcher.cs
+ Services/
+   Core/
+     ConfigService.cs
+     LocalizationService.cs
+     Logger.cs
+   Game/
+     GameSessionService.cs
+     LaunchService.cs
+     ClientPatcher.cs
+   User/
+     ProfileService.cs
+     SkinService.cs
```

**Migration:**
- Update imports
- `AppService` functions distributed across specialized services
- See [ServicesReference.md](ServicesReference.md)

---

### 3. Dependency Injection

**Was:** Direct service creation  
**Now:** DI via `Bootstrapper.cs`

```csharp
// ❌ Old code (doesn't work)
var appService = new AppService();

// ✅ New code
public class MyClass
{
    public MyClass(GameSessionService gameService) { }
}

// Or via ServiceProvider
var service = App.Current.Services!.GetRequiredService<GameSessionService>();
```

**Migration:**
- Register services in `Bootstrapper.cs`
- Get dependencies via constructor

---

### 4. Configuration Changes

**Was:** `settings.json`  
**Now:** `config.json`

**Changed fields:**

| Old Field | New Field | Note |
|-----------|-----------|------|
| `GamePath` | — | Removed (Instances manages) |
| `Theme` | — | Removed (dark theme only) |
| `JavaArgs` | — | Removed |
| — | `VersionType` | Added (`release`/`pre-release`) |
| — | `AccentColor` | Added (HEX color) |
| `Language` | `Language` | Format changed |

**Language format:**

```diff
- "Language": "English"
+ "Language": "en-US"
```

**Migration:**
- Create new `config.json` based on `Models/Config.cs` model
- Or delete old one — it will be created automatically

---

### 5. Localization

**Was:** `assets/game-lang/{CODE}/client.lang` — custom format  
**Now:** `Assets/Locales/{code}.json` — standard JSON

**Format:**

```diff
- # Old format (.lang)
- Buttons.Play=Play Game
- Buttons.Settings=Settings

+ // New format (.json)
+ {
+   "_langName": "English",
+   "_langCode": "en-US",
+   "button": {
+     "play": "Play",
+     "settings": "Settings"
+   }
+ }
```

**Migration:**
- Convert `.lang` to `.json`
- Use nested key structure

---

### 6. Assets Path

**Was:** `assets/` (lowercase)  
**Now:** `Assets/` (PascalCase)

```diff
- assets/icons/play.svg
+ Assets/Icons/play.svg
```

**Migration:**
- Update all paths in code
- Move resources to `Assets/`

---

### 7. ViewModels

**Was:** Inheriting from custom `ViewModelBase`  
**Now:** Inheriting from `ReactiveObject`

```csharp
// ❌ Old code
public class MyViewModel : ViewModelBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}

// ✅ New code
public class MyViewModel : ReactiveObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
}
```

**Migration:**
- Replace base class with `ReactiveObject`
- Use `RaiseAndSetIfChanged` for property setters

---

### 8. Commands

**Was:** Manual `ICommand` creation  
**Now:** `ReactiveCommand`

```csharp
// ❌ Old code
public ICommand PlayCommand { get; }
public MyViewModel()
{
    PlayCommand = new RelayCommand(Play);
}

// ✅ New code
public ReactiveCommand<Unit, Unit> PlayCommand { get; }
public MyViewModel()
{
    PlayCommand = ReactiveCommand.Create(Play);
}
private void Play()
{
    // ...
}
```

---

### 9. Logging

**Was:** `Logger.Log("message")`  
**Now:** `Logger.Info("category", "message")`

```csharp
// ❌ Old code
Logger.Log("Starting download");

// ✅ New code
Logger.Info("Download", "Starting download");
Logger.Success("Download", "Completed");
Logger.Error("Download", "Failed");
```

---

### 10. UI Icons

**Was:** `<Image Source="...">`  
**Now:** `<svg:Svg Path="...">`

```xml
<!-- ❌ Old code -->
<Image Source="/assets/icons/play.png"/>

<!-- ✅ New code -->
<svg:Svg Path="/Assets/Icons/play.svg" Width="24" Height="24"/>
```

---

## ⚠️ Potential Issues

### Compilation

| Error | Cause | Solution |
|-------|-------|----------|
| `CS0246: AppService not found` | Removed | Use specialized services |
| `CS0246: ViewModelBase not found` | Removed | Use `ReactiveObject` |
| `File not found: assets/` | Renamed | Change to `Assets/` |

### Runtime

| Error | Cause | Solution |
|-------|-------|----------|
| `InvalidOperationException: Service not registered` | DI not configured | Register in `Bootstrapper.cs` |
| `FileNotFoundException: config.json` | Format changed | Delete old file |

---

## 🔧 Migration Script

For automatic migration of some changes:

```bash
# Rename assets → Assets
mv assets Assets 2>/dev/null || true

# Remove old configs
rm -f settings.json auth.json

# Rebuild
dotnet clean
dotnet build
```

---

## 📚 Additional Resources

- [MigrationGuide.md](MigrationGuide.md) — Complete migration guide
- [Architecture.md](Architecture.md) — New architecture
- [ServicesReference.md](ServicesReference.md) — Services reference
