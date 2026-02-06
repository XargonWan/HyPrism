# Паттерны MVVM в HyPrism

> Руководство по паттернам Model-View-ViewModel в HyPrism с ReactiveUI.

---

## Содержание

- [Обзор](#-обзор)
- [Компоненты архитектуры](#-компоненты-архитектуры)
- [Реактивные свойства](#-реактивные-свойства)
- [Команды](#-команды)
- [Навигация](#-навигация)
- [Dependency Injection](#-dependency-injection)
- [Асинхронные паттерны](#-асинхронные-паттерны)
- [Валидация](#-валидация)
- [Unit тестирование ViewModels](#-unit-тестирование-viewmodels)
- [Анти-паттерны](#-анти-паттерны)

---

## 📋 Обзор

HyPrism использует **MVVM** (Model-View-ViewModel) архитектуру:

```
┌─────────────────────────────────────────────────────────────┐
│                          View                               │
│                    (XAML разметка)                          │
│         Button Command="{Binding PlayCommand}"              │
└────────────────────────┬────────────────────────────────────┘
                         │ Data Binding
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                       ViewModel                             │
│                   (ReactiveObject)                          │
│    RaiseAndSetIfChanged, ReactiveCommand, WhenAnyValue      │
└────────────────────────┬────────────────────────────────────┘
                         │ Dependency Injection
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                        Services                             │
│         (GameSessionService, ConfigService, etc.)           │
└────────────────────────┬────────────────────────────────────┘
                         │ CRUD Operations
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                         Model                               │
│            (Config, Profile, ModInfo, etc.)                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧩 Компоненты архитектуры

### Model

**Расположение:** `Models/`

Модели — простые классы данных без бизнес-логики:

```csharp
// Models/Profile.cs
public class Profile
{
    public string UUID { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? SkinPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### View

**Расположение:** `UI/Views/`, `UI/Components/`

XAML разметка с декларативными привязками:

```xml
<UserControl x:DataType="vm:DashboardViewModel">
    <TextBlock Text="{Binding WelcomeMessage}"/>
    <Button Content="Play" Command="{Binding PlayCommand}"/>
</UserControl>
```

### ViewModel

**Расположение:** `UI/Views/*/`, `UI/MainWindow/`, `UI/Components/*/`

Логика представления с реактивными свойствами:

```csharp
public class DashboardViewModel : ReactiveObject
{
    private readonly GameSessionService _gameService;
    
    private string _welcomeMessage = "Welcome!";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => this.RaiseAndSetIfChanged(ref _welcomeMessage, value);
    }
    
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    
    public DashboardViewModel(GameSessionService gameService)
    {
        _gameService = gameService;
        PlayCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await _gameService.LaunchAsync();
        });
    }
}
```

---

## ⚛️ ReactiveUI

### ReactiveObject

Базовый класс для всех ViewModel:

```csharp
public class MyViewModel : ReactiveObject
{
    // Свойства и логика
}
```

### Ручное объявление свойств

```csharp
private string _status = string.Empty;
public string Status
{
    get => _status;
    set => this.RaiseAndSetIfChanged(ref _status, value);
}
```

### WhenAnyValue

Реактивное наблюдение за изменениями свойств:

```csharp
public MyViewModel()
{
    // Реагировать на изменение одного свойства
    this.WhenAnyValue(x => x.Username)
        .Subscribe(username => 
        {
            IsValid = !string.IsNullOrEmpty(username);
        });
    
    // Комбинировать несколько свойств
    this.WhenAnyValue(
        x => x.Username, 
        x => x.Password,
        (user, pass) => !string.IsNullOrEmpty(user) && pass.Length >= 8)
        .ToPropertyEx(this, x => x.CanLogin);
}
```

### ObservableAsPropertyHelper (OAPH)

Вычисляемые свойства на основе других:

```csharp
private readonly ObservableAsPropertyHelper<bool> _canLogin;
public bool CanLogin => _canLogin.Value;

public MyViewModel()
{
    _canLogin = this.WhenAnyValue(
        x => x.Username,
        x => x.Password,
        (u, p) => !string.IsNullOrEmpty(u) && p.Length >= 8)
        .ToProperty(this, x => x.CanLogin);
}
```

### ReactiveCommand

Команды с реактивной поддержкой:

```csharp
// Синхронная команда
public ReactiveCommand<Unit, Unit> DoSomething { get; }

// Асинхронная команда
public ReactiveCommand<Unit, Unit> LoadData { get; }

// С параметром
public ReactiveCommand<string, Unit> Search { get; }

// С условием выполнения
public ReactiveCommand<Unit, Unit> Submit { get; }

public MyViewModel()
{
    DoSomething = ReactiveCommand.Create(() => 
    {
        // Синхронная логика
    });
    
    LoadData = ReactiveCommand.CreateFromTask(async () =>
    {
        await _service.LoadAsync();
    });
    
    Search = ReactiveCommand.CreateFromTask<string>(async query =>
    {
        Results = await _service.SearchAsync(query);
    });
    
    // Команда активна только когда CanSubmit = true
    var canSubmit = this.WhenAnyValue(x => x.CanSubmit);
    Submit = ReactiveCommand.Create(() => { }, canSubmit);
}
```

---

## 💉 Dependency Injection

### Регистрация в Bootstrapper

```csharp
public static class Bootstrapper
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();
        
        // Сервисы - Singleton (один экземпляр)
        services.AddSingleton<ConfigService>();
        services.AddSingleton<GameSessionService>();
        
        // ViewModels - Singleton для главных
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        
        // ViewModels - Transient для модальных
        services.AddTransient<SettingsViewModel>();
        
        return services.BuildServiceProvider();
    }
}
```

### Инъекция зависимостей

```csharp
public partial class DashboardViewModel : ReactiveObject
{
    private readonly GameSessionService _gameService;
    private readonly ConfigService _configService;
    private readonly LocalizationService _localization;
    
    public DashboardViewModel(
        GameSessionService gameService,
        ConfigService configService,
        LocalizationService localization)
    {
        _gameService = gameService;
        _configService = configService;
        _localization = localization;
        
        // Инициализация
    }
}
```

### Получение сервисов вручную

```csharp
// В App.axaml.cs доступен провайдер
var vm = App.Current.Services!.GetRequiredService<MyViewModel>();

// Или через GetService (возвращает null если не найден)
var optional = App.Current.Services!.GetService<OptionalService>();
```

---

## 🔗 Привязка данных

### Односторонняя привязка

```xml
<!-- Source → Target (ViewModel → View) -->
<TextBlock Text="{Binding UserName}"/>
```

### Двусторонняя привязка

```xml
<!-- Source ↔ Target -->
<TextBox Text="{Binding UserName, Mode=TwoWay}"/>
```

### Привязка команд

```xml
<Button Command="{Binding SaveCommand}"
        CommandParameter="{Binding SelectedItem}"/>
```

### Привязка к элементам

```xml
<Slider x:Name="VolumeSlider" Maximum="100"/>
<TextBlock Text="{Binding #VolumeSlider.Value}"/>
```

### Конвертеры

```xml
<Image IsVisible="{Binding HasImage, 
                          Converter={StaticResource BoolToVisibility}}"/>
```

---

## 🧪 Паттерны в HyPrism

### MainViewModel + Дочерние ViewModel

```csharp
public class MainViewModel : ReactiveObject
{
    // Дочерние ViewModel
    public LoadingViewModel LoadingVm { get; }
    public DashboardViewModel DashboardVm { get; }
    
    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    public MainViewModel(
        LoadingViewModel loadingVm,
        DashboardViewModel dashboardVm)
    {
        LoadingVm = loadingVm;
        DashboardVm = dashboardVm;
    }
    
    public async Task InitializeAsync()
    {
        await LoadingVm.LoadAsync();
        IsLoading = false;
    }
}
```

### View с динамическим контентом

```xml
<Grid>
    <!-- Loading View -->
    <views:LoadingView DataContext="{Binding LoadingVm}"
                       IsVisible="{Binding IsLoading}"/>
    
    <!-- Main Dashboard -->
    <views:DashboardView DataContext="{Binding DashboardVm}"
                         IsVisible="{Binding !IsLoading}"
                         Opacity="{Binding MainContentOpacity}"/>
</Grid>
```

### Overlay Pattern

```csharp
public class DashboardViewModel : ReactiveObject
{
    private bool _isSettingsOpen;
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => this.RaiseAndSetIfChanged(ref _isSettingsOpen, value);
    }
    
    private bool _isProfileEditorOpen;
    public bool IsProfileEditorOpen
    {
        get => _isProfileEditorOpen;
        set => this.RaiseAndSetIfChanged(ref _isProfileEditorOpen, value);
    }
    
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseSettingsCommand { get; }
    
    public DashboardViewModel()
    {
        OpenSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = true);
        CloseSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = false);
    }
}
```

```xml
<Grid>
    <!-- Main Content -->
    <ContentControl Content="{Binding MainContent}"/>
    
    <!-- Settings Overlay -->
    <Panel IsVisible="{Binding IsSettingsOpen}"
           Background="#80000000">
        <views:SettingsView/>
    </Panel>
</Grid>
```

---

## ⚠️ Антипаттерны

### ❌ Ссылки на UI в ViewModel

```csharp
// НЕПРАВИЛЬНО!
public class MyViewModel
{
    public Button PlayButton { get; set; }
    public Window MainWindow { get; set; }
}
```

### ✅ Только данные и команды

```csharp
// ПРАВИЛЬНО
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

### ❌ Бизнес-логика в Code-Behind

```csharp
// НЕПРАВИЛЬНО!
private async void Button_Click(object sender, RoutedEventArgs e)
{
    var data = await LoadFromDatabase();
    ProcessData(data);
}
```

### ✅ Логика в ViewModel или сервисах

```csharp
// ПРАВИЛЬНО
public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

public MyViewModel(IDataService dataService, IProcessor processor)
{
    LoadDataCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        var data = await dataService.LoadAsync();
        processor.Process(data);
    });
}
```

### ❌ Создание сервисов напрямую

```csharp
// НЕПРАВИЛЬНО!
public MyViewModel()
{
    _service = new MyService();
}
```

### ✅ Dependency Injection

```csharp
// ПРАВИЛЬНО
public MyViewModel(MyService service)
{
    _service = service;
}
```

---

## 📚 Примеры из кодовой базы

### DashboardViewModel

**Файл:** `UI/Views/DashboardView/DashboardViewModel.cs`

Демонстрирует:
- Инъекцию множества сервисов
- Реактивные свойства
- Команды для UI действий
- Управление overlay-ами

### MainViewModel

**Файл:** `UI/MainWindow/MainViewModel.cs`

Демонстрирует:
- Владение дочерними ViewModel
- Асинхронную инициализацию
- Управление состоянием загрузки

### SettingsViewModel

**Файл:** `UI/Views/SettingsView/SettingsViewModel.cs`

Демонстрирует:
- Двустороннюю привязку настроек
- Валидацию
- Сохранение через сервисы

---

## 📚 Дополнительные ресурсы

- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [Avalonia Data Binding](https://docs.avaloniaui.net/docs/data-binding/)
- [UIComponentGuide.md](UIComponentGuide.md) — Создание компонентов
