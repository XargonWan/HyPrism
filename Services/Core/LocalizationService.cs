using System.Reactive.Linq;
using System.Text.Json;
using System.Reflection;
using ReactiveUI;

namespace HyPrism.Services.Core;

/// <summary>
/// Provides localization and internationalization services for the application.
/// Manages translation loading, caching, and reactive language switching.
/// </summary>
/// <remarks>
/// Translations are loaded from embedded JSON resources in Assets/Locales.
/// The service supports runtime language changes with reactive UI updates.
/// </remarks>
public class LocalizationService : ReactiveObject, ILocalizationService
{
    // Static accessor for XAML markup extensions only (cannot use DI).
    // Set during app initialization from the DI container.
    internal static LocalizationService? Current { get; set; }

    // Thread-safe translations access
    private volatile Dictionary<string, string> _translations = new();
    private string _currentLanguage = "en-US";
    
    // Cache for available languages to avoid scanning assembly every time
    private static Dictionary<string, string>? _cachedAvailableLanguages;

    // Cache for loaded translations: Key=LanguageCode, Value=Dictionary of translations
    private readonly Dictionary<string, Dictionary<string, string>> _languageCache = new();
    private readonly object _cacheLock = new();

    /// <summary>
    /// Gets available languages by scanning embedded resources.
    /// Returns Dictionary where Key = language code (e.g. "ru-RU"), Value = native name (e.g. "Русский")
    /// </summary>
    public static Dictionary<string, string> GetAvailableLanguages()
    {
        if (_cachedAvailableLanguages != null)
            return _cachedAvailableLanguages;

        var result = new Dictionary<string, string>();
        var assembly = Assembly.GetExecutingAssembly();
        // Standard namespace + folder structure "HyPrism.Assets.Locales." 
        // But let's filter generically to be safe
        var suffix = ".json";

        var resourceNames = assembly.GetManifestResourceNames();
        
        foreach (var resourceName in resourceNames)
        {
            if (resourceName.Contains(".Locales.") && resourceName.EndsWith(suffix))
            {
                // Extract code: HyPrism.Assets.Locales.en-US.json -> en-US
                // We assume the segment between Locales. and .json is the code
                // But we must handle cases where prefix is different if namespace changed
                
                var parts = resourceName.Split('.');
                // Expected: [..., "Locales", "en-US", "json"]
                // Code is parts[parts.Length - 2]
                
                if (parts.Length >= 2)
                {
                    var langCode = parts[parts.Length - 2];
                    
                    try
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using var reader = new StreamReader(stream);
                            var json = reader.ReadToEnd();
                            using var doc = JsonDocument.Parse(json);
                            
                            // Read _langName
                            if (doc.RootElement.TryGetProperty("_langName", out var langNameElement))
                            {
                                var langName = langNameElement.GetString();
                                result[langCode] = !string.IsNullOrEmpty(langName) ? langName : langCode;
                            }
                            else
                            {
                                result[langCode] = langCode;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Localization", $"Failed to parse locale {langCode}: {ex.Message}");
                    }
                }
            }
        }
        
        // Ensure en-US is always present as fallback
        if (!result.ContainsKey("en-US"))
        {
            result["en-US"] = "English";
        }

        _cachedAvailableLanguages = result;
        return result;
    }
    
    /// <summary>
    /// Gets or sets the current language code (e.g., "en-US", "ru-RU").
    /// Setting this property loads the corresponding translations and notifies UI for updates.
    /// </summary>
    /// <value>The BCP 47 language tag of the current language.</value>
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
             // Force refresh if needed, or rely on cache. 
             // Ideally we check keys of cached dict.
             var available = GetAvailableLanguages();
             if (!available.ContainsKey(value))
             {
                 Logger.Warning("Localization", $"Invalid language code: {value}, keeping: {_currentLanguage}");
                 return;
             }
             
            // Only load if actually changed
            if (_currentLanguage != value)
            {
                // LOAD FIRST: Update the translation dictionary before notifying UI
                LoadLanguage(value);
                
                // NOTIFY SECOND: Now that the dictionary is ready, tell UI to refresh
                this.RaiseAndSetIfChanged(ref _currentLanguage, value);
            }
        }
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationService"/> class.
    /// Loads only the default English locale on startup for memory efficiency.
    /// </summary>
    public LocalizationService()
    {
        // Load only the default language on startup - other languages are loaded on demand
        // This saves ~500KB of RAM by not preloading all 12 locales
        LoadLanguage("en-US");
    }
    
    /// <summary>
    /// Loads all available languages into memory cache
    /// </summary>
    public void PreloadAllLanguages()
    {
        var languages = GetAvailableLanguages();
        foreach (var lang in languages.Keys)
        {
            lock (_cacheLock)
            {
                if (!_languageCache.ContainsKey(lang))
                {
                    LoadLanguageInternal(lang);
                }
            }
        }
        Logger.Success("Localization", "All translations successfully loaded");
    }

    /// <summary>
    /// Creates an observable that tracks a specific translation key.
    /// This is the key method for reactive bindings!
    /// </summary>
    public IObservable<string> GetObservable(string key)
    {
        return this.WhenAnyValue(x => x.CurrentLanguage)
            .Select(_ => Translate(key));
    }
    
    /// <summary>
    /// Loads translations for the specified language code.
    /// Uses cached translations if available, otherwise loads from embedded resources.
    /// </summary>
    /// <param name="languageCode">The BCP 47 language tag to load (e.g., "ru-RU").</param>
    private void LoadLanguage(string languageCode)
    {
        // Check cache first (thread-safe)
        lock (_cacheLock)
        {
            if (_languageCache.TryGetValue(languageCode, out var cachedTranslations))
            {
                _translations = cachedTranslations;
                Logger.Info("Localization", $"Loaded language '{languageCode}' from memory cache");
                return;
            }
        }

        var loaded = LoadLanguageInternal(languageCode);
        if (loaded != null)
        {
            _translations = loaded;
        }
        else
        {
             // Fallback to English if load failed
            if (languageCode != "en-US")
            {
                Logger.Warning("Localization", $"Falling back to English from {languageCode}");
                LoadLanguage("en-US");
            }
        }
    }

    /// <summary>
    /// Internal method to load translations from embedded resources.
    /// Also caches the loaded translations for future use.
    /// </summary>
    /// <param name="languageCode">The language code to load.</param>
    /// <returns>The loaded translations dictionary, or <c>null</c> if loading failed.</returns>
    private Dictionary<string, string>? LoadLanguageInternal(string languageCode)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"HyPrism.Assets.Locales.{languageCode}.json";
        
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                Logger.Warning("Localization", $"Language file not found: {resourceName}");
                return null;
            }
            
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            // Parse as JsonDocument to support nested keys
            using var doc = JsonDocument.Parse(json);
            
            // Flatten for compatibility with old-style key lookups
            var translations = new Dictionary<string, string>();
            FlattenJson(doc.RootElement, "", translations);
            
            // Add to cache
            lock (_cacheLock) 
            {
                _languageCache[languageCode] = translations;
            }
            
            return translations;
        }
        catch (Exception ex)
        {
            Logger.Error("Localization", $"Failed to load language file '{languageCode}': {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Recursively flattens a nested JSON element into a flat dictionary with dot-separated keys.
    /// </summary>
    /// <param name="element">The JSON element to flatten.</param>
    /// <param name="prefix">The current key prefix for nested objects.</param>
    /// <param name="result">The dictionary to populate with flattened key-value pairs.</param>
    private void FlattenJson(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                FlattenJson(property.Value, key, result);
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            result[prefix] = element.GetString() ?? "";
        }
    }
    
    /// <summary>
    /// Translates a key to the localized string in the current language.
    /// Falls back to English if the key is not found in the current language.
    /// </summary>
    /// <param name="key">The translation key (e.g., "dashboard.play").</param>
    /// <param name="args">Optional format arguments for placeholder replacement ({0}, {1}, etc.).</param>
    /// <returns>The localized string, or the key itself if no translation is found.</returns>
    public string Translate(string key, params object[] args)
    {
        string? translation = null;
        
        // Try current language
        if (_translations.TryGetValue(key, out var val))
        {
            translation = val;
        }
        // Fallback to English if missing
        else if (_currentLanguage != "en-US")
        {
            lock (_cacheLock)
            {
                if (_languageCache.TryGetValue("en-US", out var enDict) && enDict.TryGetValue(key, out var enVal))
                {
                    translation = enVal;
                }
            }
        }

        if (translation != null)
        {
            // Simple placeholder replacement {0}, {1}, etc.
            if (args.Length > 0)
            {
                try
                {
                    return string.Format(translation, args);
                }
                catch
                {
                    return translation;
                }
            }
            return translation;
        }
        
        // Return key if translation not found
        return key;
    }
    
    /// <summary>
    /// Gets the localized string for the specified key.
    /// Note: ReactiveObject doesn't auto-notify for indexers; use <see cref="GetObservable"/> for reactive bindings.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    public string this[string key] => Translate(key);
}
