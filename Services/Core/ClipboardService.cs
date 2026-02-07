using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace HyPrism.Services.Core;

/// <summary>
/// Clipboard service implementation using Avalonia's clipboard API.
/// This isolates Avalonia UI dependency from ViewModels, maintaining MVVM separation.
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <inheritdoc/>
    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            return await clipboard.TryGetTextAsync();
        }
        return null;
    }

    /// <summary>
    /// Gets the Avalonia clipboard instance from the main window.
    /// </summary>
    /// <returns>The clipboard instance, or <c>null</c> if not available.</returns>
    private static Avalonia.Input.Platform.IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }
        return null;
    }
}
