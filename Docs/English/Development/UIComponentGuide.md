# UI Component Guide

> Complete guide on creating, styling, and using UI components in HyPrism.

---

## Table of Contents

- [UI Structure](#-ui-structure)
- [Creating a New Component](#-creating-a-new-component)
- [Component Patterns](#-component-patterns)
- [Styling](#-styling)
- [Working with Icons](#-working-with-icons)
- [Value Converters](#-value-converters)
- [Animations](#-animations)
- [Layout Components](#-layout-components)
- [Component Checklist](#-component-checklist)
- [Anti-Patterns](#-anti-patterns)

---

## 📁 UI Structure

```
UI/
├── App.axaml              # Global resources and styles
├── App.axaml.cs           # Application initialization
├── MainWindow/            # Main window
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   └── MainViewModel.cs
├── Components/            # Reusable components
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
├── Views/                 # Full-screen views
│   ├── DashboardView/
│   ├── SettingsView/
│   ├── ProfileEditorView/
│   └── ...
├── Styles/                # Global styles
├── Converters/            # Value Converters
└── Helpers/               # Helper classes
```

---

## 🎨 Creating a New Component

### Step 1: Create Folder

New components are placed in `UI/Components/<Category>/<ComponentName>/`:

```bash
mkdir -p UI/Components/Buttons/MyButton
```

### Step 2: Create XAML File

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
            <!-- SVG icon -->
            <svg:Svg Path="/Assets/Icons/star.svg" 
                     Width="16" Height="16"/>
            
            <!-- Button text -->
            <TextBlock Text="{Binding ButtonText}" 
                       VerticalAlignment="Center"/>
        </StackPanel>
    </Button>
    
</UserControl>
```

### Step 3: Create Code-Behind

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

> ⚠️ **Important:** Code-behind should be minimal! All logic goes in ViewModel.

### Step 4: Create ViewModel (optional)

If the component needs its own logic:

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
            // Click logic
        });
    }
}
```

---

## 🎯 Component Patterns

### Pattern 1: Simple Component (without ViewModel)

For static components use Avalonia Properties:

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
<!-- Usage -->
<local:IconButton IconPath="/Assets/Icons/settings.svg" 
                  Command="{Binding OpenSettingsCommand}"/>
```

### Pattern 2: Component with Own ViewModel

For components with complex logic:

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

### Pattern 3: Component with External DataContext

When data comes from parent ViewModel:

```xml
<!-- In parent View -->
<ItemsControl ItemsSource="{Binding NewsItems}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <cards:NewsCard DataContext="{Binding}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🎨 Styling

### Global Resources

Defined in `App.axaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <!-- Colors -->
        <SolidColorBrush x:Key="SystemAccentBrush" Color="#FFA845"/>
        <SolidColorBrush x:Key="PrimaryTextBrush" Color="#FFFFFF"/>
        <SolidColorBrush x:Key="SecondaryTextBrush" Color="#B0B0B0"/>
        
        <!-- Styles -->
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="/UI/Styles/BaseControlStyles.axaml"/>
            <ResourceInclude Source="/UI/Styles/CommonAnimations.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Using Styles

```xml
<!-- Via Classes -->
<Button Classes="Primary">Primary Button</Button>
<Button Classes="Secondary">Secondary Button</Button>

<!-- Via StaticResource -->
<TextBlock Foreground="{StaticResource SystemAccentBrush}"/>
```

### Local Component Styles

```xml
<UserControl.Styles>
    <Style Selector="Button.MyCustomClass">
        <Setter Property="Background" Value="#333"/>
        <Setter Property="CornerRadius" Value="8"/>
    </Style>
    
    <!-- Hover state -->
    <Style Selector="Button.MyCustomClass:pointerover">
        <Setter Property="Background" Value="#444"/>
    </Style>
</UserControl.Styles>
```

---

## 🖼️ Working with Icons

### SVG Icons (recommended)

```xml
<svg:Svg Path="/Assets/Icons/play.svg" 
         Width="24" 
         Height="24"/>
```

**Advantages:**
- Scalability without quality loss
- Smaller file size
- CSS styling capability

### Bitmap Images

Use **only** for user content (avatars, screenshots):

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

For loading images from network:

```xml
<Image asyncImageLoader:ImageLoader.Source="{Binding RemoteImageUrl}"/>
```

---

## 📦 Value Converters

### Creating a Converter

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

### Registration in App.axaml

```xml
<Application.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
</Application.Resources>
```

### Usage

```xml
<Button IsVisible="{Binding IsLoggedIn, Converter={StaticResource BoolToVisibility}}"/>
```

---

## 🔄 Animations

### Animation Definition

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

### Application

```xml
<Panel Classes="FadeIn" IsVisible="{Binding IsVisible}">
    <!-- Content -->
</Panel>
```

---

## 📐 Layout Components

### OverlayContainer

For modal windows and overlays:

```xml
<Grid>
    <!-- Main content -->
    <ContentControl Content="{Binding MainContent}"/>
    
    <!-- Overlay -->
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

## ✅ Component Checklist

- [ ] Component is in correct category (`Buttons/`, `Cards/`, `Layouts/`)
- [ ] XAML and Code-behind are in the same folder
- [ ] Minimal Code-behind (only constructor)
- [ ] All logic is in ViewModel
- [ ] Uses `StaticResource` for colors
- [ ] SVG for icons (not Bitmap)
- [ ] DataContext is correctly bound
- [ ] Necessary styles are added

---

## 🚫 Anti-Patterns

### ❌ Logic in Code-Behind

```csharp
// WRONG!
private void Button_Click(object sender, RoutedEventArgs e)
{
    IsLoading = true;
    await LoadDataAsync();
    IsLoading = false;
}
```

### ✅ Logic in ViewModel

```csharp
// CORRECT
public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

public MyViewModel()
{
    LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
}

private async Task LoadDataAsync()
{
    IsLoading = true;
    await _dataService.LoadAsync();
    IsLoading = false;
}
```

### ❌ Hardcoded Colors

```xml
<!-- WRONG -->
<Button Background="#FFA845"/>
```

### ✅ Using Resources

```xml
<!-- CORRECT -->
<Button Background="{StaticResource SystemAccentBrush}"/>
```

### ❌ Bitmap for Icons

```xml
<!-- WRONG -->
<Image Source="/Assets/Icons/play.png"/>
```

### ✅ SVG for Icons

```xml
<!-- CORRECT -->
<svg:Svg Path="/Assets/Icons/play.svg"/>
```

---

## 📚 Additional Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [StylingGuide.md](StylingGuide.md) — Deep dive into styles
- [MVVMPatterns.md](MVVMPatterns.md) — MVVM Patterns
