namespace HyPrism.Services.Core;

/// <summary>
/// Provides file and folder dialog operations abstracted from the UI layer.
/// Enables ViewModels to request file/folder selection without direct UI dependencies.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Opens a folder picker dialog to allow the user to select a directory.
    /// </summary>
    /// <param name="initialPath">Optional initial directory path to start the dialog in.</param>
    /// <returns>The selected folder path, or <c>null</c> if the user cancelled the dialog.</returns>
    Task<string?> BrowseFolderAsync(string? initialPath = null);
    
    /// <summary>
    /// Opens a file picker dialog configured for selecting mod files (.jar, .zip).
    /// </summary>
    /// <returns>An array of selected file paths. Returns empty array if cancelled.</returns>
    Task<string[]> BrowseModFilesAsync();
}
