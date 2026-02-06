# Стандарты кодирования

Для обеспечения качества, поддерживаемости и консистентности кода все контрибьюторы должны следовать этим стандартам.

---

## Содержание

- [Общие рекомендации C#](#-общие-рекомендации-c)
- [Архитектура и дизайн](#-архитектура-и-дизайн)
- [Асинхронное программирование](#-асинхронное-программирование)
- [Обработка ошибок](#-обработка-ошибок)
- [Работа с файлами](#-работа-с-файлами)
- [XAML и UI](#-xaml-и-ui)
- [Тестирование](#-тестирование)
- [Документация](#-документация)
- [Git и контроль версий](#-git-и-контроль-версий)

---

## 💻 Общие рекомендации C#

### Версия языка

- **Target Framework:** .NET 10 (Preview)
- **C# Version:** 13 (latest)
- Используйте новейшие возможности языка

### Форматирование

- **Отступы:** 4 пробела (не табы)
- **Скобки:** Allman style (скобки на новой строке)
- **Рекомендуется:** использовать `.editorconfig`

```csharp
// ✅ Правильно (Allman style)
public void DoSomething()
{
    if (condition)
    {
        // ...
    }
}
```

### Конвенции именования

| Тип | Стиль | Пример |
|-----|-------|--------|
| Классы, методы, свойства | PascalCase | `GameSessionService`, `LoadAsync()` |
| Локальные переменные | camelCase | `userName`, `isValid` |
| Приватные поля | _camelCase | `_configService`, `_isLoading` |
| Интерфейсы | IPrefix | `IService`, `IDisposable` |
| Константы | PascalCase | `MaxRetryCount` |
| Enum значения | PascalCase | `ConnectionState.Connected` |

---

## 🏗️ Архитектура и дизайн

### MVVM (Model-View-ViewModel)

#### Строгое разделение

ViewModels **не должны** ссылаться на Avalonia Controls:

```csharp
// ❌ НЕПРАВИЛЬНО!
public class MyViewModel
{
    public Button PlayButton { get; set; }
    public Window MainWindow { get; set; }
}

// ✅ ПРАВИЛЬНО
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

#### Свойства

Используйте `RaiseAndSetIfChanged` из ReactiveUI:

```csharp
// ❌ НЕПРАВИЛЬНО (ручная реализация INotifyPropertyChanged)
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

// ✅ ПРАВИЛЬНО (ReactiveUI)
private string _name;
public string Name
{
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
}
```

#### Команды

Используйте `ReactiveCommand` из ReactiveUI:

```csharp
// ✅ Синхронная команда
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public MyViewModel(IConfigService configService)
{
    SaveCommand = ReactiveCommand.Create(() => configService.Save());
}

// ✅ Асинхронная команда
public ReactiveCommand<Unit, Unit> LoadCommand { get; }

public MyViewModel(IService service)
{
    LoadCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        Data = await service.LoadAsync();
    });
}

// ✅ С условием CanExecute
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

### Асинхронность

#### Правила

1. **Суффикс `Async`** для всех асинхронных методов
2. **`async`/`await`** для I/O операций
3. **Избегайте `Task.Run`** для I/O (только для CPU-intensive)
4. **Обрабатывайте исключения** в `async void` методах

```csharp
// ✅ ПРАВИЛЬНО
public async Task<Config> LoadConfigAsync()
{
    var json = await File.ReadAllTextAsync(path);
    return JsonConvert.DeserializeObject<Config>(json);
}

// ❌ НЕПРАВИЛЬНО (нет суффикса)
public async Task<Config> LoadConfig() { }

// ❌ НЕПРАВИЛЬНО (Task.Run для I/O)
public async Task<Config> LoadConfigAsync()
{
    return await Task.Run(() => File.ReadAllText(path));
}
```

#### Обработка исключений в async void

```csharp
// ✅ ПРАВИЛЬНО
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

**Nullable Reference Types** включены (`<Nullable>enable</Nullable>`).

#### Правила

1. **Избегайте `!`** (null-forgiving operator) — используйте только когда абсолютно уверены
2. **Проверяйте null** через pattern matching
3. **Используйте `?`** для nullable типов

```csharp
// ❌ НЕПРАВИЛЬНО
var name = user!.Name; // Опасно!

// ✅ ПРАВИЛЬНО
if (user is not null)
{
    var name = user.Name;
}

// ✅ ПРАВИЛЬНО (null-коалесценция)
var name = user?.Name ?? "Unknown";
```

---

## 📐 Слои приложения

### UI Layer

- **Только** логика отображения
- **Минимальный** code-behind
- Сложные вычисления → сервисы

```csharp
// ❌ НЕПРАВИЛЬНО (логика в code-behind)
private async void Button_Click(object sender, RoutedEventArgs e)
{
    var data = await FetchFromApi();
    ProcessData(data);
    UpdateUI(data);
}

// ✅ ПРАВИЛЬНО (логика в ViewModel)
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

- **Stateless** singleton сервисы
- **Single Responsibility** — один сервис = одна задача
- State → модели или session managers

```csharp
// ✅ Пример хорошего сервиса
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

### Правила

1. **Не создавайте сервисы вручную** — получайте через DI
2. **Используйте конструкторную инъекцию**
3. **Регистрируйте в `Bootstrapper.cs`**

```csharp
// ❌ НЕПРАВИЛЬНО
public MyViewModel()
{
    _service = new MyService();
}

// ✅ ПРАВИЛЬНО
public MyViewModel(MyService service)
{
    _service = service;
}
```

### Регистрация

```csharp
// Bootstrapper.cs
services.AddSingleton<ConfigService>();
services.AddSingleton<GameSessionService>();
services.AddTransient<SettingsViewModel>();
```

---

## 📝 Комментарии и документация

### XML документация

Публичные API должны иметь XML документацию:

```csharp
/// <summary>
/// Загружает конфигурацию из файла.
/// </summary>
/// <param name="path">Путь к файлу конфигурации.</param>
/// <returns>Загруженная конфигурация или null.</returns>
/// <exception cref="FileNotFoundException">Файл не найден.</exception>
public Config? LoadConfig(string path)
```

### Комментарии в коде

Объясняйте **почему**, а не **что**:

```csharp
// ❌ НЕПРАВИЛЬНО (описывает что)
// Увеличиваем счётчик на 1
counter++;

// ✅ ПРАВИЛЬНО (объясняет почему)
// Компенсируем off-by-one в API ответе
counter++;
```

---

## 🎨 UI специфичные правила

### Стили

```xml
<!-- ❌ НЕПРАВИЛЬНО (hardcoded цвета) -->
<Button Background="#FFA845"/>

<!-- ✅ ПРАВИЛЬНО (через ресурсы) -->
<Button Background="{DynamicResource SystemAccentBrush}"/>
```

### Иконки

```xml
<!-- ❌ НЕПРАВИЛЬНО (Bitmap) -->
<Image Source="/Assets/Icons/play.png"/>

<!-- ✅ ПРАВИЛЬНО (SVG) -->
<svg:Svg Path="/Assets/Icons/play.svg"/>
```

### Data Binding

```xml
<!-- ✅ С compile-time проверкой -->
<UserControl x:DataType="vm:MyViewModel">
    <TextBlock Text="{Binding Name}"/>
</UserControl>
```

---

## ⚠️ Критические компоненты

### ClientPatcher

**Файл:** `Services/Game/ClientPatcher.cs`

> ⚠️ **ВНИМАНИЕ:** Изменяйте только с полным пониманием последствий!
> Этот компонент влияет на целостность игры.

---

## ✅ Чеклист перед коммитом

- [ ] Код компилируется без warnings
- [ ] `<Nullable>enable</Nullable>` — нет `!` без причины
- [ ] Async методы имеют суффикс `Async`
- [ ] ViewModels не ссылаются на UI Controls
- [ ] Сервисы получаются через DI
- [ ] Публичные методы задокументированы
- [ ] Нет hardcoded цветов в XAML
- [ ] SVG для иконок (не Bitmap)

---

## 📚 Дополнительные ресурсы

- [MVVMPatterns.md](MVVMPatterns.md) — Паттерны MVVM
- [UIComponentGuide.md](UIComponentGuide.md) — Создание компонентов
- [StylingGuide.md](StylingGuide.md) — Стилизация
