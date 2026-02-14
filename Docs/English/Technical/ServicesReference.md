# Services Reference

All services are registered as singletons in `Bootstrapper.cs` and injected via constructor.

## Core Services (`Services/Core/`)

### IpcService
- **File:** `Services/Core/IpcService.cs`
- **Purpose:** Central IPC channel registry — single source of truth for all React ↔ .NET communication
- **Key method:** `RegisterAll()` — registers all IPC handlers
- **Annotations:** Contains `@type` and `@ipc` doc comments used by code generator
- **Domains:** config, game, news, profile, settings, i18n, window, browser, mods, console
- **Instance saves handlers:** supports listing saves, opening save folders, and deleting save folders via IPC (`hyprism:instance:saves`, `hyprism:instance:openSaveFolder`, `hyprism:instance:deleteSave`)

### ConfigService
- **File:** `Services/Core/ConfigService.cs`
- **Type:** Singleton
- **Purpose:** Application configuration (persisted to JSON)
- **Config paths:**
  - Windows: `%APPDATA%/HyPrism/config.json`
  - Linux: `~/.config/HyPrism/config.json`
  - macOS: `~/Library/Application Support/HyPrism/config.json`

### Logger
- **File:** `Services/Core/Logger.cs`
- **Type:** Static class
- **Purpose:** Structured logging (Serilog backend + colored console + in-memory buffer)
- **Methods:** `Info()`, `Success()`, `Warning()`, `Error()`, `Debug()`, `Progress()`
- **Log files:** `{appDir}/Logs/{timestamp}.log`

### LocalizationService
- **File:** `Services/Core/LocalizationService.cs`
- **Type:** Singleton (Instance pattern)
- **Purpose:** Runtime language switching with nested key support
- **Locale files:** `Assets/Locales/{code}.json`

### BrowserService
- **File:** `Services/Core/BrowserService.cs`
- **Purpose:** Opens URLs in the system default browser

### DiscordService
- **File:** `Services/Core/DiscordService.cs`
- **Purpose:** Discord Rich Presence integration

### GitHubService
- **File:** `Services/Core/GitHubService.cs`
- **Purpose:** Release checking and self-update functionality

## Game Services (`Services/Game/`)

### GameSessionService
- **Purpose:** Manages game lifecycle — download, install, patch, launch
- **States:** preparing → download → install → patching → launching → running → stopped
- **Auth launch behavior:** In authenticated mode, launch identity/name is derived from token claims when available to avoid server-side username mismatch shutdowns.
- **Custom auth mode:** Non-official profiles can launch in online authenticated mode with client + server patching for custom session domains.
- **Stop control:** Game stop is available through IPC (`hyprism:game:stop`) and can be triggered from Dashboard and Instances actions.

### ClientPatcher ⚠️
- **File:** `Services/Game/ClientPatcher.cs`
- **CRITICAL:** Binary manipulation for game integrity
- **Rule:** NEVER modify without explicit instruction

### ModService
- **Purpose:** Mod listing, searching, and management (CurseForge integration)
- **Instance mods source:** Reads from `UserData/Mods` and falls back to file-system discovery (`.jar`, `.zip`, `.disabled`) when manifest entries are missing

## User Services (`Services/User/`)

### ProfileService
- **Purpose:** Player profile CRUD operations
- **Features:** Multiple profiles, avatar management, profile switching
