# Архитектура системы

HyPrism использует паттерн **Model-View-ViewModel (MVVM)** со строгим разделением UI и бизнес-логики.

> **Миграция:** Проект перешёл с Photino (WebKit) на Avalonia UI. См. [MigrationGuide.md](MigrationGuide.md).

---

## Содержание

- [Высокоуровневый обзор](#-высокоуровневый-обзор)
- [Слои архитектуры](#-слои-архитектуры)
- [Поток данных](#-поток-данных)
- [Dependency Injection](#-dependency-injection)
- [Структура ViewModel](#-структура-viewmodel)
- [Жизненный цикл приложения](#-жизненный-цикл-приложения)
- [Коммуникация между компонентами](#-коммуникация-между-компонентами)
- [Архитектурные принципы](#-архитектурные-принципы)

---

## 🏗️ Высокоуровневый обзор

```mermaid
graph TD
    subgraph "UI Layer"
        User[Пользователь] --> View[View<br/>XAML разметка]
        View -->|Data Binding| ViewModel[ViewModel<br/>ReactiveObject]
    end
    
    subgraph "Service Layer"
        ViewModel -->|DI| Services[Services<br/>Бизнес-логика]
        Services --> Core[Core Services]
        Services --> Game[Game Services]
        Services --> UserSvc[User Services]
    end
    
    subgraph "Data Layer"
        Core --> Config[(Config)]
        Game --> Disk[(Файловая система)]
        Game --> Network[(Сеть)]
        UserSvc --> Profile[(Профили)]
    end
```

---

## 📦 Слои архитектуры

### 1. Presentation Layer (UI)

**Расположение:** `UI/`

| Компонент | Описание |
|-----------|----------|
| **Views** | Полноэкранные XAML представления (`DashboardView`, `SettingsView`) |
| **Components** | Переиспользуемые UI элементы (`PrimaryButton`, `NewsCard`) |
| **MainWindow** | Главное окно и корневой `MainViewModel` |
| **Converters** | Value Converters для преобразования данных |
| **Styles** | Глобальные стили и анимации |

**Принципы:**
- Code-behind минимален (только конструктор)
- Вся логика в ViewModel
- Используется `x:DataType` для compile-time проверки binding

### 2. ViewModel Layer

**Расположение:** `UI/Views/*/`, `UI/MainWindow/`, `UI/Components/*/`

ViewModels наследуют `ReactiveObject` и используют:
- `RaiseAndSetIfChanged` — реактивные свойства
- `ReactiveCommand` — команды для UI
- `WhenAnyValue` — реактивные подписки
- `ObservableAsPropertyHelper` — вычисляемые свойства

**Ключевые ViewModel:**

| ViewModel | Ответственность |
|-----------|-----------------|
| `MainViewModel` | Корневой VM, владеет Loading и Dashboard |
| `DashboardViewModel` | Главный UI state, управление overlay-ами |
| `SettingsViewModel` | Настройки приложения |
| `LoadingViewModel` | Экран загрузки |

### 3. Service Layer

**Расположение:** `Services/`

Сервисы организованы по доменам:

```
Services/
├── Core/           # Инфраструктура (Config, Logger, Localization)
├── Game/           # Игровая логика (Launch, Download, Mods)
└── User/           # Пользователь (Profile, Skin)
```

**Принципы:**
- Singleton паттерн через DI
- Единая ответственность (SRP)
- Сервисы не зависят от UI

### 4. Model Layer

**Расположение:** `Models/`

Модели — простые POCO классы:
- `Config` — конфигурация приложения
- `Profile` — профиль пользователя
- `ModInfo` — информация о моде
- `InstalledInstance` — установленный инстанс игры

---

## 💉 Dependency Injection

HyPrism использует `Microsoft.Extensions.DependencyInjection`.

### Bootstrapper.cs

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
        services.AddSingleton<Logger>();
        
        // Game Services
        services.AddSingleton<GameSessionService>();
        services.AddSingleton<VersionService>();
        services.AddSingleton<LaunchService>();
        
        // User Services
        services.AddSingleton<ProfileService>();
        services.AddSingleton<SkinService>();
        
        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        return services.BuildServiceProvider();
    }
}
```

### Получение зависимостей

```csharp
// В App.axaml.cs
Services = Bootstrapper.Initialize();
var mainVm = Services.GetRequiredService<MainViewModel>();

// В ViewModel через конструктор
public DashboardViewModel(
    GameSessionService gameSession,
    ConfigService config,
    LocalizationService localization)
{
    _gameSession = gameSession;
    _config = config;
    _localization = localization;
}
```

---

## 📚 Библиотеки и зависимости

| Библиотека | Версия | Назначение |
|------------|--------|------------|
| **Avalonia** | 11.3.11 | UI Framework |
| **ReactiveUI** | 11.3.9 | Reactive MVVM |
| **SkiaSharp** | 3.116.1 | Рендеринг графики |
| **Serilog** | 4.3.0 | Логирование |
| **Newtonsoft.Json** | 13.0.3 | JSON сериализация |
| **M.E.DependencyInjection** | 10.0.2 | DI контейнер |

---

## 🔄 Поток данных: Запуск игры

```mermaid
sequenceDiagram
    participant User
    participant View as DashboardView
    participant VM as DashboardViewModel
    participant GS as GameSessionService
    participant VS as VersionService
    participant DS as DownloadService
    participant LS as LaunchService
    
    User->>View: Клик "Play"
    View->>VM: PlayCommand.Execute()
    VM->>GS: DownloadAndLaunchAsync()
    
    GS->>VS: GetVersionListAsync()
    VS-->>GS: версии
    
    alt Нужна загрузка
        GS->>DS: DownloadFileAsync()
        DS-->>GS: progress events
        GS-->>VM: OnProgressChanged
        VM-->>View: ProgressPercent binding
    end
    
    GS->>LS: LaunchAsync()
    LS-->>GS: Process started
    GS-->>VM: Game running
```

### Детальный процесс

1. **View:** Пользователь нажимает кнопку "Play"
2. **ViewModel:** `PlayCommand` вызывает `GameSessionService.DownloadAndLaunchAsync()`
3. **GameSessionService:**
   - Получает список версий через `VersionService`
   - Определяет целевую версию
   - Проверяет наличие игры через `InstanceService`
   - Загружает/обновляет через `ButlerService` + `DownloadService`
   - Применяет моды через `ModService`
   - Применяет скины через `SkinService`
   - Запускает через `LaunchService`
4. **ViewModel:** Подписан на `ProgressNotificationService.OnProgressChanged`
5. **View:** UI обновляется автоматически через binding

---

## 🗂️ Ключевые компоненты

### GameSessionService

**Файл:** `Services/Game/GameSessionService.cs` (~1000 строк)

Оркестратор всего процесса запуска игры. Координирует:
- Получение версий
- Скачивание и патчинг
- Применение модов и скинов
- Запуск процесса

### ClientPatcher

**Файл:** `Services/Game/ClientPatcher.cs`

⚠️ **Критический компонент** — бинарное патчирование исполняемого файла игры.

### LocalizationService

**Файл:** `Services/Core/LocalizationService.cs`

Реактивная система локализации с поддержкой hot-reload языка.

---

## 📐 Диаграммы

### Зависимости сервисов

```mermaid
graph LR
    subgraph Core
        Config[ConfigService]
        Loc[LocalizationService]
        Log[Logger]
        Theme[ThemeService]
        Progress[ProgressNotificationService]
    end
    
    subgraph Game
        GS[GameSessionService]
        VS[VersionService]
        IS[InstanceService]
        DS[DownloadService]
        LS[LaunchService]
        BS[ButlerService]
        MS[ModService]
    end
    
    subgraph User
        PS[ProfileService]
        PMS[ProfileManagementService]
        SS[SkinService]
    end
    
    GS --> Config
    GS --> VS
    GS --> IS
    GS --> DS
    GS --> LS
    GS --> BS
    GS --> MS
    GS --> SS
    GS --> Progress
    
    PMS --> PS
    PMS --> Config
```

---

## 📚 Дополнительные ресурсы

- [MigrationGuide.md](MigrationGuide.md) — Миграция с Photino
- [ServicesReference.md](ServicesReference.md) — Справочник сервисов
- [MVVMPatterns.md](../Development/MVVMPatterns.md) — Паттерны MVVM
- [ProjectStructure.md](ProjectStructure.md) — Структура проекта
