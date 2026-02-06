# Руководство по UI компонентам

> Полное руководство по созданию, стилизации и использованию UI компонентов в HyPrism.

---

## Содержание

- [Структура UI](#-структура-ui)
- [Создание нового компонента](#-создание-нового-компонента)
- [Компонент с ViewModel](#-компонент-с-viewmodel)
- [Стандартные компоненты](#-стандартные-компоненты)
- [Data Binding](#-data-binding)
- [Конвертеры](#-конвертеры)
- [Анимации компонентов](#-анимации-компонентов)
- [Behaviors](#-behaviors)
- [Best Practices](#-best-practices)
- [Чеклист при создании компонента](#-чеклист-при-создании-компонента)

---

## 📁 Структура UI

```
UI/
├── App.axaml              # Глобальные ресурсы и стили
├── App.axaml.cs           # Инициализация приложения
├── MainWindow/            # Главное окно
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   └── MainViewModel.cs
├── Components/            # Переиспользуемые компоненты
│   ├── Buttons/
│   │   ├── CloseButton/
│   │   ├── IconButton/
│   │   └── PrimaryButton/
│   ├── Cards/
│   │   ├── NewsCard/
│   │   └── NoticeCard/
│   ├── Common/
│   ├── Dashboard/
│   ├── Inputs/
│   ├── Layouts/
│   └── Navigation/
├── Views/                 # Полноэкранные представления
│   ├── DashboardView/
│   ├── SettingsView/
│   ├── ProfileEditorView/
│   └── ...
├── Styles/                # Глобальные стили
├── Converters/            # Value Converters
└── Helpers/               # Вспомогательные классы
```

---

## 🎨 Создание нового компонента

### Шаг 1: Создайте папку

Новые компоненты размещаются в `UI/Components/<Category>/<ComponentName>/`:

```bash
mkdir -p UI/Components/Buttons/MyButton
```

### Шаг 2: Создайте XAML файл

**`UI/Components/Buttons/MyButton/MyButton.axaml`:**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:svg="clr-namespace:Avalonia.Svg.Skia;assembly=Avalonia.Svg.Skia"
             x:Class="HyPrism.UI.Components.Buttons.MyButton.MyButton"
             x:DataType="local:MyButtonViewModel">
    
    <Button Classes="Primary" 
            Command="{Binding ClickCommand}"
            HorizontalAlignment="Stretch">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <!-- SVG иконка -->
            <svg:Svg Path="/Assets/Icons/star.svg" 
                     Width="16" Height="16"/>
            
            <!-- Текст кнопки -->
            <TextBlock Text="{Binding ButtonText}" 
                       VerticalAlignment="Center"/>
        </StackPanel>
    </Button>
    
</UserControl>
```

### Шаг 3: Создайте Code-Behind

**`UI/Components/Buttons/MyButton/MyButton.axaml.cs`:**

```csharp
using Avalonia.Controls;

namespace HyPrism.UI.Components.Buttons.MyButton;

public partial class MyButton : UserControl
{
    public MyButton()
    {
        InitializeComponent();
    }
}
```

> ⚠️ **Важно:** Code-behind должен быть минимальным! Вся логика в ViewModel.

### Шаг 4: Создайте ViewModel (опционально)

Если компоненту нужна собственная логика:

**`UI/Components/Buttons/MyButton/MyButtonViewModel.cs`:**

```csharp
using ReactiveUI;
using System.Reactive;

namespace HyPrism.UI.Components.Buttons.MyButton;

public class MyButtonViewModel : ReactiveObject
{
    private string _buttonText = "Click me";
    public string ButtonText
    {
        get => _buttonText;
        set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }
    
    public ReactiveCommand<Unit, Unit> ClickCommand { get; }
    
    public MyButtonViewModel()
    {
        ClickCommand = ReactiveCommand.Create(() =>
        {
            // Логика клика
        });
    }
}
```

---

## 🎯 Паттерны компонентов

### Паттерн 1: Простой компонент (без ViewModel)

Для статических компонентов используйте Avalonia Properties:

```csharp
public partial class IconButton : UserControl
{
    public static readonly StyledProperty<string> IconPathProperty =
        AvaloniaProperty.Register<IconButton, string>(nameof(IconPath));
    
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand?>(nameof(Command));
    
    public string IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }
    
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    
    public IconButton()
    {
        InitializeComponent();
    }
}
```

```xml
<!-- Использование -->
<local:IconButton IconPath="/Assets/Icons/settings.svg" 
                  Command="{Binding OpenSettingsCommand}"/>
```

### Паттерн 2: Компонент с собственным ViewModel

Для компонентов со сложной логикой:

```csharp
public partial class NewsCard : UserControl
{
    public NewsCard()
    {
        InitializeComponent();
        DataContext = new NewsCardViewModel();
    }
}
```

### Паттерн 3: Компонент с внешним DataContext

Когда данные приходят из родительского ViewModel:

```xml
<!-- В родительском View -->
<ItemsControl ItemsSource="{Binding NewsItems}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <cards:NewsCard DataContext="{Binding}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🎨 Стилизация

### Глобальные ресурсы

Определены в `App.axaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <!-- Цвета -->
        <SolidColorBrush x:Key="SystemAccentBrush" Color="#FFA845"/>
        <SolidColorBrush x:Key="PrimaryTextBrush" Color="#FFFFFF"/>
        <SolidColorBrush x:Key="SecondaryTextBrush" Color="#B0B0B0"/>
        
        <!-- Стили -->
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="/UI/Styles/BaseControlStyles.axaml"/>
            <ResourceInclude Source="/UI/Styles/CommonAnimations.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Использование стилей

```xml
<!-- Через Classes -->
<Button Classes="Primary">Primary Button</Button>
<Button Classes="Secondary">Secondary Button</Button>

<!-- Через StaticResource -->
<TextBlock Foreground="{StaticResource SystemAccentBrush}"/>
```

### Локальные стили компонента

```xml
<UserControl.Styles>
    <Style Selector="Button.MyCustomClass">
        <Setter Property="Background" Value="#333"/>
        <Setter Property="CornerRadius" Value="8"/>
    </Style>
    
    <!-- Состояние при наведении -->
    <Style Selector="Button.MyCustomClass:pointerover">
        <Setter Property="Background" Value="#444"/>
    </Style>
</UserControl.Styles>
```

---

## 🖼️ Работа с иконками

### SVG иконки (рекомендуется)

```xml
<svg:Svg Path="/Assets/Icons/play.svg" 
         Width="24" 
         Height="24"/>
```

**Преимущества:**
- Масштабируемость без потери качества
- Меньший размер файла
- Возможность стилизации через CSS

### Bitmap изображения

Используйте **только** для пользовательского контента (аватары, скриншоты):

```xml
<Image Source="{Binding AvatarPath}" 
       Width="64" 
       Height="64">
    <Image.Clip>
        <EllipseGeometry Rect="0,0,64,64"/>
    </Image.Clip>
</Image>
```

### AsyncImageLoader

Для загрузки изображений из сети:

```xml
<Image asyncImageLoader:ImageLoader.Source="{Binding RemoteImageUrl}"/>
```

---

## 📦 Value Converters

### Создание конвертера

**`UI/Converters/BoolToVisibilityConverter.cs`:**

```csharp
using Avalonia.Data.Converters;
using System.Globalization;

namespace HyPrism.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### Регистрация в App.axaml

```xml
<Application.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
</Application.Resources>
```

### Использование

```xml
<Button IsVisible="{Binding IsLoggedIn, Converter={StaticResource BoolToVisibility}}"/>
```

---

## 🔄 Анимации

### Определение анимации

**`UI/Styles/CommonAnimations.axaml`:**

```xml
<Styles>
    <Style Selector="Panel.FadeIn">
        <Style.Animations>
            <Animation Duration="0:0:0.3" FillMode="Forward">
                <KeyFrame Cue="0%">
                    <Setter Property="Opacity" Value="0"/>
                </KeyFrame>
                <KeyFrame Cue="100%">
                    <Setter Property="Opacity" Value="1"/>
                </KeyFrame>
            </Animation>
        </Style.Animations>
    </Style>
</Styles>
```

### Применение

```xml
<Panel Classes="FadeIn" IsVisible="{Binding IsVisible}">
    <!-- Контент -->
</Panel>
```

---

## 📐 Layout компоненты

### OverlayContainer

Для модальных окон и оверлеев:

```xml
<Grid>
    <!-- Основной контент -->
    <ContentControl Content="{Binding MainContent}"/>
    
    <!-- Оверлей -->
    <Panel IsVisible="{Binding IsOverlayVisible}"
           Background="#80000000">
        <Border Background="#2A2A2A"
                CornerRadius="16"
                Padding="24"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">
            <ContentControl Content="{Binding OverlayContent}"/>
        </Border>
    </Panel>
</Grid>
```

---

## ✅ Чеклист при создании компонента

- [ ] Компонент в правильной категории (`Buttons/`, `Cards/`, `Layouts/`)
- [ ] XAML и Code-behind в одной папке
- [ ] Минимальный Code-behind (только конструктор)
- [ ] Вся логика в ViewModel
- [ ] Используются `StaticResource` для цветов
- [ ] SVG для иконок (не Bitmap)
- [ ] DataContext правильно привязан
- [ ] Добавлены необходимые стили

---

## 🚫 Антипаттерны

### ❌ Логика в Code-Behind

```csharp
// НЕПРАВИЛЬНО!
private void Button_Click(object sender, RoutedEventArgs e)
{
    IsLoading = true;
    await LoadDataAsync();
    IsLoading = false;
}
```

### ✅ Логика в ViewModel

```csharp
// ПРАВИЛЬНО
public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

public MyViewModel()
{
    LoadDataCommand = ReactiveCommand.CreateFromTask(async () =>
    {
        IsLoading = true;
        await _dataService.LoadAsync();
        IsLoading = false;
    });
}
```

### ❌ Hardcoded цвета

```xml
<!-- НЕПРАВИЛЬНО -->
<Button Background="#FFA845"/>
```

### ✅ Использование ресурсов

```xml
<!-- ПРАВИЛЬНО -->
<Button Background="{StaticResource SystemAccentBrush}"/>
```

### ❌ Bitmap для иконок

```xml
<!-- НЕПРАВИЛЬНО -->
<Image Source="/Assets/Icons/play.png"/>
```

### ✅ SVG для иконок

```xml
<!-- ПРАВИЛЬНО -->
<svg:Svg Path="/Assets/Icons/play.svg"/>
```

---

## 📚 Дополнительные ресурсы

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [StylingGuide.md](StylingGuide.md) — Глубже о стилях
- [MVVMPatterns.md](MVVMPatterns.md) — Паттерны MVVM
