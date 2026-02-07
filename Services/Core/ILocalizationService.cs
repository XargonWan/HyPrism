namespace HyPrism.Services.Core;

/// <summary>
/// Provides reactive localization with observable translations for dynamic language switching.
/// Supports loading translations from embedded JSON resources and format string interpolation.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets or sets the current UI language code (e.g., "en-US", "ru-RU").
    /// Setting this property triggers language reload and notifies all subscribers.
    /// </summary>
    string CurrentLanguage { get; set; }
    
    /// <summary>
    /// Gets a translated string for the specified key using the current language.
    /// </summary>
    /// <param name="key">The translation key (e.g., "dashboard.play").</param>
    /// <returns>The translated string, or the key itself if translation not found.</returns>
    string this[string key] { get; }
    
    /// <summary>
    /// Gets an observable that emits translated values whenever the language changes.
    /// Useful for binding UI elements that should update automatically on language switch.
    /// </summary>
    /// <param name="key">The translation key to observe.</param>
    /// <returns>An observable stream of translated strings.</returns>
    IObservable<string> GetObservable(string key);
    
    /// <summary>
    /// Translates a key with optional format arguments for string interpolation.
    /// </summary>
    /// <param name="key">The translation key containing format placeholders.</param>
    /// <param name="args">Arguments to substitute into format placeholders.</param>
    /// <returns>The formatted translated string.</returns>
    string Translate(string key, params object[] args);
    
    /// <summary>
    /// Preloads all available language translations into memory for faster switching.
    /// Call during application startup for better UX.
    /// </summary>
    void PreloadAllLanguages();
}
