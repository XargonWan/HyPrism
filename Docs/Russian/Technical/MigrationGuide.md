# Руководство по миграции: Photino → Avalonia UI

> **PR #299:** [Merge from Photino to AvaloniaUI](https://github.com/yyyumeniku/HyPrism/pull/299)  
> **Статус:** В процессе (Draft)  
> **Изменений:** +33,151 / -155,144 строк в 897 файлах

---

## Содержание

- [Обзор миграции](#-обзор-миграции)
- [Архитектурные изменения](#-архитектурные-изменения)
- [Изменения структуры файлов](#-изменения-структуры-файлов)
- [Рефакторинг AppService](#-рефакторинг-appservice)
- [Dependency Injection](#-dependency-injection)
- [Изменения UI](#-изменения-ui)
- [Критические изменения для пользователей](#-критические-изменения-для-пользователей)
- [Для разработчиков: что нужно знать](#-для-разработчиков-что-нужно-знать)
- [Статус миграции (PR #299)](#-статус-миграции-pr-299)

---

## 📋 Обзор миграции

### Что было заменено

| Компонент | До (Photino) | После (Avalonia) |
|-----------|--------------|------------------|
| **UI Framework** | Photino (WebKit) | Avalonia UI 11.3 |
| **Фронтенд** | HTML/CSS/TypeScript | XAML/C# |
| **Архитектура** | SPA + IPC мост | Нативный MVVM |
| **Рендеринг** | WebKit Engine | SkiaSharp |
| **Стейт-менеджмент** | JavaScript + Bridge | ReactiveUI |

### Почему была произведена миграция

1. **Проблемы с WebKit на Linux** — Issue #183 и множество подобных проблем с рендерингом на разных дистрибутивах
2. **Архитектурная сложность** — IPC-мост между C# и JavaScript создавал ненужную сложность
3. **Производительность** — WebKit потреблял больше памяти и CPU
4. **Поддержка** — Photino имеет ограниченное сообщество и менее активную разработку
5. **Унификация кодовой базы** — Весь код теперь на C#, что упрощает разработку

---

## 🏗️ Архитектурные изменения

### До миграции (Photino)

```
┌─────────────────────────────────────────────────────┐
│                    Photino Window                    │
├─────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────┐    │
│  │           WebKit Browser Engine              │    │
│  │  ┌───────────────────────────────────────┐  │    │
│  │  │     HTML/CSS/TypeScript Frontend      │  │    │
│  │  │  (React-like components, SPA routing) │  │    │
│  │  └───────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────┘    │
│                        ↕ IPC Bridge                  │
│  ┌─────────────────────────────────────────────┐    │
│  │              C# Backend (AppService)         │    │
│  │    (Monolithic god-object with all logic)   │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

**Проблемы старой архитектуры:**
- `AppService.cs` был "God Object" с 3000+ строк кода
- IPC-вызовы добавляли латентность
- Сложная отладка (два runtime: .NET + JavaScript)
- Проблемы с WebKit на некоторых Linux дистрибутивах

### После миграции (Avalonia)

```
┌─────────────────────────────────────────────────────┐
│                   Avalonia Window                    │
├─────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────┐    │
│  │              Views (XAML)                    │    │
│  │         Pure declarative UI markup           │    │
│  └───────────────────┬─────────────────────────┘    │
│                      ↕ Data Binding                  │
│  ┌─────────────────────────────────────────────┐    │
│  │            ViewModels (ReactiveUI)           │    │
│  │      ObservableProperties + Commands         │    │
│  └───────────────────┬─────────────────────────┘    │
│                      ↓ DI                           │
│  ┌─────────────────────────────────────────────┐    │
│  │         Services (Single Responsibility)     │    │
│  │   GameSessionService, ConfigService, etc.   │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

**Преимущества новой архитектуры:**
- Чистый MVVM паттерн
- Сервисы с единственной ответственностью
- Dependency Injection через `Bootstrapper.cs`
- Нативная производительность
- Единый язык программирования (C#)

---

## 📁 Изменения в структуре файлов

### Удалённые директории

```diff
- frontend/                  # Весь TypeScript/React код
- frontend/node_modules/     
- frontend/src/
- frontend/dist/
- wwwroot/                   # Статические ресурсы для WebKit
- Backend/                   # Переименовано в Services/
```

### Новые директории

```diff
+ Services/
+   Core/                    # Базовые сервисы (Config, Logger, etc.)
+   Game/                    # Игровые сервисы (Launch, Download, etc.)
+   User/                    # Пользовательские сервисы (Profile, Skin)
+ UI/
+   Components/              # Переиспользуемые UI компоненты
+   Views/                   # Полноэкранные представления
+   MainWindow/              # Главное окно и его ViewModel
+   Styles/                  # XAML стили
+   Converters/              # Value Converters
+ Assets/                    # Капитализирован (Images, Icons, Locales)
```

### Переименования

| Старое | Новое | Причина |
|--------|-------|---------|
| `Backend/AppService.cs` | Разделён на 20+ сервисов | Single Responsibility |
| `assets/` | `Assets/` | Соответствие .NET конвенциям |
| `scripts/` | `Scripts/` | Консистентность |
| `packaging/` | `Packaging/` | Консистентность |

---

## 🔄 Рефакторинг AppService

Монолитный `AppService.cs` (~3000 строк) был разбит на специализированные сервисы:

### Core Services (`Services/Core/`)

| Сервис | Ответственность |
|--------|-----------------|
| `ConfigService` | Чтение/запись конфигурации |
| `LocalizationService` | Система локализации |
| `Logger` | Централизованное логирование |
| `ThemeService` | Управление темами и акцентными цветами |
| `BrowserService` | Открытие внешних ссылок |
| `ProgressNotificationService` | Уведомления о прогрессе |
| `FileService` | Файловые операции |
| `GitHubService` | Работа с GitHub API |
| `NewsService` | Загрузка новостей |
| `DiscordService` | Discord Rich Presence |

### Game Services (`Services/Game/`)

| Сервис | Ответственность |
|--------|-----------------|
| `GameSessionService` | Оркестрация запуска игры |
| `LaunchService` | Конструирование процесса запуска |
| `DownloadService` | Загрузка файлов |
| `VersionService` | Управление версиями игры |
| `InstanceService` | Управление инстансами |
| `ModService` | Менеджмент модов |
| `ClientPatcher` | Бинарное патчирование |
| `ButlerService` | Интеграция с itch.io Butler |
| `AssetService` | Управление ассетами |

### User Services (`Services/User/`)

| Сервис | Ответственность |
|--------|-----------------|
| `ProfileService` | Данные профилей |
| `ProfileManagementService` | Высокоуровневые операции с профилями |
| `SkinService` | Пользовательские скины |
| `UserIdentityService` | Идентификация пользователя |

---

## 💉 Dependency Injection

Новая система DI через `Bootstrapper.cs`:

```csharp
public static class Bootstrapper
{
    public static IServiceProvider Initialize()
    {
        var services = new ServiceCollection();
        
        // Инфраструктура
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

### Получение сервисов

```csharp
// В App.axaml.cs
var mainVm = Services!.GetRequiredService<MainViewModel>();

// В ViewModel (через конструктор)
public DashboardViewModel(GameSessionService gameSession, ...) 
{
    _gameSessionService = gameSession;
}
```

---

## 🎨 Изменения в UI

### Сравнение подходов

| Аспект | Photino (Before) | Avalonia (After) |
|--------|------------------|-------------------|
| **Разметка** | HTML + CSS | XAML |
| **Стили** | CSS файлы | XAML Styles + Resources |
| **Компоненты** | TypeScript классы | UserControl + ViewModel |
| **Binding** | Ручной через IPC | Нативный Data Binding |
| **Анимации** | CSS animations | Avalonia Animations |
| **Иконки** | `<img src="...">` | `<svg:Svg Path="..."/>` |

### Пример: Кнопка

**До (HTML/CSS):**
```html
<button class="primary-button" onclick="bridge.launch()">
  <img src="icons/play.svg" />
  <span>Play</span>
</button>
```

**После (XAML):**
```xml
<Button Command="{Binding LaunchCommand}" Classes="Primary">
  <StackPanel Orientation="Horizontal">
    <svg:Svg Path="/Assets/Icons/play.svg" Width="16" Height="16"/>
    <TextBlock Text="{Binding PlayButtonText}"/>
  </StackPanel>
</Button>
```

---

## ⚠️ Breaking Changes для пользователей

### Конфигурация

- **Путь конфигурации не изменился**: `%APPDATA%/HyPrism` (Windows), `~/.config/HyPrism` (Linux)
- **Формат `config.json`**: Добавлены новые поля, старые сохранены для совместимости
- **Локали**: Миграция с `.lang` на `.json` (папка `Assets/Locales/`)

### Визуальные изменения

- Новый дизайн интерфейса (Avalonia-native look)
- Улучшенные анимации и переходы
- Поддержка системных тем (светлая/тёмная)

---

## 🔧 Для разработчиков: Что нужно знать

### 1. Больше нет JavaScript

Весь фронтенд код удалён. Если вы ранее работали с `frontend/`, теперь всё в `UI/`.

### 2. MVVM обязателен

ViewModels не должны ссылаться на Avalonia Controls напрямую:

```csharp
// ❌ НЕПРАВИЛЬНО
public class MyViewModel
{
    public Button MyButton { get; set; } // НЕ ДЕЛАЙТЕ ТАК!
}

// ✅ ПРАВИЛЬНО
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

### 3. Используйте ReactiveUI паттерны

```csharp
// Объявление свойства с RaiseAndSetIfChanged
private string _name;
public string Name
{
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
}
```

### 4. Сервисы через DI

Не создавайте сервисы напрямую. Получайте их через конструктор или `App.Current.Services`:

```csharp
// ❌ НЕПРАВИЛЬНО
var config = new ConfigService(appDir);

// ✅ ПРАВИЛЬНО
public MyViewModel(ConfigService configService) 
{
    _configService = configService;
}
```

---

## 📊 Статус миграции (PR #299)

### Завершено ✅
- [x] Рефакторинг `AppService.cs` на отдельные сервисы
- [x] Удаление WebKit (Photino) зависимостей
- [x] Главная страница
- [x] Оверлеи
- [x] Меню модов (только UI)
- [x] Блок новостей
- [x] Логотипы брендов
- [x] Локализация
- [x] Темы/Цвета
- [x] Обновление скриптов сборки

### В процессе 🔄
- [ ] Экран первого запуска
- [ ] Стилизация
- [ ] Меню настроек
- [ ] Меню профиля
- [ ] Discord RPC
- [ ] События клиента

### Требуется тестирование 🧪
- [ ] UI артефакты
- [ ] Фоновые сервисы
- [ ] Конфигурационные сервисы
- [ ] Системные зависимости
- [ ] Кросс-платформенная работа
- [ ] Полный цикл приложения

---

## 📚 Дополнительные ресурсы

- [Architecture.md](Architecture.md) — Новая архитектура
- [UIComponentGuide.md](../Development/UIComponentGuide.md) — Создание компонентов
- [ServicesReference.md](ServicesReference.md) — Справочник сервисов
