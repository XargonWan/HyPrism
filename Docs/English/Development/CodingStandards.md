# Coding Standards

To ensure quality, maintainability, and consistency of code, all contributors must follow these standards.

---

## Table of Contents

- [General C# Guidelines](#-general-c-guidelines)
- [Architecture and Design](#-architecture-and-design)
- [Asynchronous Programming](#asynchronous-programming)
- [Null Safety](#null-safety)
- [Application Layers](#-application-layers)
- [Dependency Injection](#-dependency-injection)
- [Comments and Documentation](#-comments-and-documentation)
- [UI Specific Rules](#-ui-specific-rules)
- [Critical Components](#-critical-components)
- [Pre-Commit Checklist](#-pre-commit-checklist)

---

## 💻 General C# Guidelines

### Language Version

- **Target Framework:** .NET 10 (Preview)
- **C# Version:** 13 (latest)
- Use the newest language features

### Formatting

- **Indentation:** 4 spaces (not tabs)
- **Braces:** Allman style (braces on new line)
- **Recommended:** use `.editorconfig`

```csharp
// ✅ Correct (Allman style)
public void DoSomething()
{
    if (condition)
    {
        // ...
    }
}
```

### Naming Conventions

| Type | Style | Example |
|------|-------|---------|
| Classes, methods, properties | PascalCase | `GameSessionService`, `LoadAsync()` |
| Local variables | camelCase | `userName`, `isValid` |
| Private fields | _camelCase | `_configService`, `_isLoading` |
| Interfaces | IPrefix | `IService`, `IDisposable` |
| Constants | PascalCase | `MaxRetryCount` |
| Enum values | PascalCase | `ConnectionState.Connected` |

---

## 🏗️ Architecture and Design

### MVVM (Model-View-ViewModel)

#### Strict Separation

ViewModels **must not** reference Avalonia Controls:

```csharp
// ❌ WRONG!
public class MyViewModel
{
    public Button PlayButton { get; set; }
    public Window MainWindow { get; set; }
}

// ✅ CORRECT
public class MyViewModel : ReactiveObject
{
    private string _buttonText;
    public string ButtonText
    {
        get => _buttonText;
        set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }
    
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    
    public MyViewModel()
    {
        PlayCommand = ReactiveCommand.Create(() => { });
    }
}
```

#### Properties

Use `RaiseAndSetIfChanged` from ReactiveUI:

```csharp
// ❌ WRONG (manual INotifyPropertyChanged implementation)
private string _name;
public string Name
{
    get => _name;
    set
    {
        if (_name != value)
        {
            _name = value;
            OnPropertyChanged();
        }
    }
}

// ✅ CORRECT (ReactiveUI)
private string _name;
public string Name
{
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
}
```

#### Commands

Use `ReactiveCommand` from ReactiveUI:

```csharp
// ✅ Synchronous command
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public MyViewModel(IConfigService configService)
{
    SaveCommand = ReactiveCommand.Create(() => configService.Save());
}

// ✅ Asynchronous command
public ReactiveCommand<Unit, Unit> LoadCommand { get; }

public MyViewModel(IService service)
{
    LoadCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        Data = await service.LoadAsync();
    });
}

// ✅ With CanExecute condition
private string _name;
public string Name
{
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
}

public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public MyViewModel()
{
    var canSave = this.WhenAnyValue(x => x.Name)
        .Select(name => !string.IsNullOrEmpty(name));
    
    SaveCommand = ReactiveCommand.Create(() => { }, canSave);
}
```

---

### Asynchronous Programming

#### Rules

1. **`Async` suffix** for all asynchronous methods
2. **`async`/`await`** for I/O operations
3. **Avoid `Task.Run`** for I/O (only for CPU-intensive work)
4. **Handle exceptions** in `async void` methods

```csharp
// ✅ CORRECT
public async Task<Config> LoadConfigAsync()
{
    var json = await File.ReadAllTextAsync(path);
    return JsonConvert.DeserializeObject<Config>(json);
}

// ❌ WRONG (no suffix)
public async Task<Config> LoadConfig() { }

// ❌ WRONG (Task.Run for I/O)
public async Task<Config> LoadConfigAsync()
{
    return await Task.Run(() => File.ReadAllText(path));
}
```

#### Exception Handling in async void

```csharp
// ✅ CORRECT
private async void OnButtonClick()
{
    try
    {
        await LoadDataAsync();
    }
    catch (Exception ex)
    {
        Logger.Error("Load", ex.Message);
    }
}
```

---

### Null Safety

**Nullable Reference Types** are enabled (`<Nullable>enable</Nullable>`).

#### Rules

1. **Avoid `!`** (null-forgiving operator) — use only when absolutely certain
2. **Check null** via pattern matching
3. **Use `?`** for nullable types

```csharp
// ❌ WRONG
var name = user!.Name; // Dangerous!

// ✅ CORRECT
if (user is not null)
{
    var name = user.Name;
}

// ✅ CORRECT (null-coalescing)
var name = user?.Name ?? "Unknown";
```

---

## 📐 Application Layers

### UI Layer

- **Only** display logic
- **Minimal** code-behind
- Complex calculations → services

```csharp
// ❌ WRONG (logic in code-behind)
private async void Button_Click(object sender, RoutedEventArgs e)
{
    var data = await FetchFromApi();
    ProcessData(data);
    UpdateUI(data);
}

// ✅ CORRECT (logic in ViewModel)
public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

public MyViewModel(IService service)
{
    LoadDataCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        Data = await service.LoadAsync();
    });
}
```

### Service Layer

- **Stateless** singleton services
- **Single Responsibility** — one service = one task
- State → models or session managers

```csharp
// ✅ Example of good service
public class ConfigService
{
    private readonly string _configPath;
    
    public Config Load() => JsonConvert.DeserializeObject<Config>(
        File.ReadAllText(_configPath));
    
    public void Save(Config config) => 
        File.WriteAllText(_configPath, JsonConvert.SerializeObject(config));
}
```

---

## 💉 Dependency Injection

### Rules

1. **Don't create services manually** — get them through DI
2. **Use constructor injection**
3. **Register in `Bootstrapper.cs`**

```csharp
// ❌ WRONG
public MyViewModel()
{
    _service = new MyService();
}

// ✅ CORRECT
public MyViewModel(MyService service)
{
    _service = service;
}
```

### Registration

```csharp
// Bootstrapper.cs
services.AddSingleton<ConfigService>();
services.AddSingleton<GameSessionService>();
services.AddTransient<SettingsViewModel>();
```

---

## 📝 Comments and Documentation

### XML Documentation

Public APIs should have XML documentation:

```csharp
/// <summary>
/// Loads configuration from file.
/// </summary>
/// <param name="path">Path to configuration file.</param>
/// <returns>Loaded configuration or null.</returns>
/// <exception cref="FileNotFoundException">File not found.</exception>
public Config? LoadConfig(string path)
```

### Code Comments

Explain **why**, not **what**:

```csharp
// ❌ WRONG (describes what)
// Increment counter by 1
counter++;

// ✅ CORRECT (explains why)
// Compensate for off-by-one in API response
counter++;
```

---

## 🎨 UI Specific Rules

### Styles

```xml
<!-- ❌ WRONG (hardcoded colors) -->
<Button Background="#FFA845"/>

<!-- ✅ CORRECT (via resources) -->
<Button Background="{DynamicResource SystemAccentBrush}"/>
```

### Icons

```xml
<!-- ❌ WRONG (Bitmap) -->
<Image Source="/Assets/Icons/play.png"/>

<!-- ✅ CORRECT (SVG) -->
<svg:Svg Path="/Assets/Icons/play.svg"/>
```

### Data Binding

```xml
<!-- ✅ With compile-time checking -->
<UserControl x:DataType="vm:MyViewModel">
    <TextBlock Text="{Binding Name}"/>
</UserControl>
```

---

## ⚠️ Critical Components

### ClientPatcher

**File:** `Services/Game/ClientPatcher.cs`

> ⚠️ **WARNING:** Modify only with full understanding of consequences!
> This component affects game integrity.

---

## ✅ Pre-Commit Checklist

- [ ] Code compiles without warnings
- [ ] `<Nullable>enable</Nullable>` — no `!` without reason
- [ ] Async methods have `Async` suffix
- [ ] ViewModels don't reference UI Controls
- [ ] Services obtained via DI
- [ ] Public methods are documented
- [ ] No hardcoded colors in XAML
- [ ] SVG for icons (not Bitmap)

---

## 📚 Additional Resources

- [MVVMPatterns.md](MVVMPatterns.md) — MVVM Patterns
- [UIComponentGuide.md](UIComponentGuide.md) — Creating Components
- [StylingGuide.md](StylingGuide.md) — Styling
