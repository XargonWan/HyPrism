# Localization System

HyPrism includes a flexible localization system with runtime language switching without restart.

---

## 📁 File Structure

### Current System (`Assets/Locales/`)

Localizations are stored in JSON files following IETF standards:

```
Assets/Locales/
├── en-US.json      # English (United States)
├── ru-RU.json      # Русский
├── de-DE.json      # Deutsch
├── es-ES.json      # Español
├── fr-FR.json      # Français
├── ja-JP.json      # 日本語
├── ko-KR.json      # 한국어
├── pt-BR.json      # Português (Brasil)
├── tr-TR.json      # Türkçe
├── uk-UA.json      # Українська
├── zh-CN.json      # 简体中文
└── be-BY.json      # Беларуская
```

### ⚠️ Legacy System (`assets/game-lang/`)

> **Deprecated!** This folder contains old `.lang` files for compatibility with previous versions. **Do not use** for new translations.

---

## 📄 File Format

### JSON Structure

```json
{
  "_langName": "English",
  "_langCode": "en-US",
  "button": {
    "play": "Play",
    "settings": "Settings",
    "mods": "Mods",
    "profile": "Profile"
  },
  "dashboard": {
    "welcome": "Welcome, {0}!",
    "version": "Version: {0}",
    "status": {
      "ready": "Ready to play",
      "downloading": "Downloading...",
      "updating": "Updating..."
    }
  },
  "settings": {
    "language": "Language",
    "theme": "Theme",
    "discord": "Discord RPC"
  },
  "errors": {
    "network": "Network error",
    "notFound": "File not found"
  }
}
```

### Metadata

| Key | Description |
|-----|-------------|
| `_langName` | Language name (for UI display) |
| `_langCode` | IETF code (must match filename) |

### Nesting

Keys can be nested for better organization:

```json
{
  "dashboard": {
    "status": {
      "ready": "Ready"
    }
  }
}
```

Access: `dashboard.status.ready`

### Placeholders

Use `{0}`, `{1}`, etc. for value substitution:

```json
{
  "welcome": "Welcome, {0}!"
}
```

```csharp
var text = String.Format(localization.Translate("welcome"), username);
```

---

## ⚙️ LocalizationService

**File:** `Services/Core/LocalizationService.cs`

### API

```csharp
public class LocalizationService : ReactiveObject
{
    // Singleton
    public static LocalizationService Instance { get; }
    
    // Current language (reactive property)
    public string CurrentLanguage { get; set; }
    
    // Get translation
    public string Translate(string key);
    
    // Reactive retrieval (updates on language change)
    public IObservable<string> GetObservable(string key);
    
    // List of available languages
    public static Dictionary<string, string> GetAvailableLanguages();
}
```

### Usage in ViewModel

```csharp
public class MyViewModel : ReactiveObject
{
    private string _playButtonText = "";
    public string PlayButtonText
    {
        get => _playButtonText;
        set => this.RaiseAndSetIfChanged(ref _playButtonText, value);
    }
    
    public MyViewModel()
    {
        // Reactive subscription — updates on language change
        LocalizationService.Instance.GetObservable("button.play")
            .Subscribe(text => PlayButtonText = text);
    }
}
```

### Usage in XAML

```xml
<!-- Via converter -->
<TextBlock Text="{Binding Source={x:Static services:LocalizationService.Instance},
                          Path=CurrentLanguage,
                          Converter={StaticResource TranslateConverter},
                          ConverterParameter=button.play}"/>

<!-- Via ViewModel -->
<TextBlock Text="{Binding PlayButtonText}"/>
```

---

## 🌍 Adding a New Language

### Step 1: Create File

Copy `en-US.json` and rename:

```bash
cp Assets/Locales/en-US.json Assets/Locales/pl-PL.json
```

### Step 2: Update Metadata

```json
{
  "_langName": "Polski",
  "_langCode": "pl-PL",
  ...
}
```

### Step 3: Translate Strings

```json
{
  "button": {
    "play": "Graj",
    "settings": "Ustawienia"
  }
}
```

### Step 4: Done!

The application will automatically detect the new file on next launch and add it to the language list.

---

## 📐 Translation Guidelines

### String Length

Try to keep length close to English original to avoid UI issues.

| Original | ✅ Good | ❌ Bad |
|----------|---------|--------|
| "Play" | "Graj" | "Rozpocznij sesję gry" |
| "Settings" | "Ustawienia" | "Parametry i konfiguracja aplikacji" |

### Placeholders

Don't forget to preserve `{0}`, `{1}`, etc.:

```json
// ✅ Correct
"Version: {0}"

// ❌ Wrong (lost placeholder)
"Version"
```

### Testing

After translating, check all screens in the application for:
- Truncated text
- Incorrect translations
- Missing placeholders

---

## 🔧 Fallback System

If a key is missing in the current language, the system automatically uses `en-US.json`.

```csharp
// Lookup order:
// 1. ru-RU.json -> dashboard.title
// 2. en-US.json -> dashboard.title (fallback)
// 3. Returns key "dashboard.title" if not found
```

---

## 📊 Update Script

**File:** `Scripts/update_locales.py`

Used to synchronize keys between languages:

```bash
python Scripts/update_locales.py
```

The script:
1. Scans all `.json` files
2. Finds missing keys
3. Adds them with empty values or fallback to English

---

## 📚 Additional Resources

- [Features.md](../General/Features.md) — Localization feature description
- [Configuration.md](../User/Configuration.md) — Language configuration
