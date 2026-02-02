using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HyPrism.UI.Components.Buttons;

public class IconButton : Button
{
    protected override Type StyleKeyOverride => typeof(IconButton);

    public static readonly StyledProperty<IBrush?> ButtonBackgroundProperty =
        AvaloniaProperty.Register<IconButton, IBrush?>(nameof(ButtonBackground));

    public IBrush? ButtonBackground
    {
        get => GetValue(ButtonBackgroundProperty);
        set => SetValue(ButtonBackgroundProperty, value);
    }
    
    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<IconButton, string?>(nameof(IconPath));

    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconSize), 24.0);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<string> HoverCssProperty =
        AvaloniaProperty.Register<IconButton, string>(nameof(HoverCss), "* { stroke: #FFA845; fill: none; }");

    public string HoverCss
    {
        get => GetValue(HoverCssProperty);
        private set => SetValue(HoverCssProperty, value);
    }

    public IconButton()
    {
        // Initialize with accent color as soon as possible
        UpdateHoverCss();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ForegroundProperty)
        {
            UpdateHoverCss();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Re-update in case resources weren't available during constructor
        UpdateHoverCss();
    }

    private void UpdateHoverCss()
    {
        // Try to get SystemAccentBrush from resources
        if (Application.Current?.TryGetResource("SystemAccentBrush", null, out var resource) == true 
            && resource is ISolidColorBrush accentBrush)
        {
            var c = accentBrush.Color;
            var hexColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            HoverCss = $"* {{ stroke: {hexColor}; fill: none; }}";
        }
        else
        {
            // Fallback to HytaleOrange if SystemAccentBrush is not available
            HoverCss = "* { stroke: #FFA845; fill: none; }";
        }
    }
}
