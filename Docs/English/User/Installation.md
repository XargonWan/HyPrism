# Installation

## Requirements

- **Windows 10/11** (x64), **Linux** (x64), or **macOS** (x64/arm64)
- Internet connection for first launch (authentication & game download)

## Download

Download the latest release from the [GitHub Releases](https://github.com/yyyumeniku/HyPrism/releases) page.

### Windows

1. Download `HyPrism-win-x64.zip`
2. Extract to any folder
3. Run `HyPrism.exe`

### Linux

HyPrism uses native Linux dialog tools for folder/file selection. Install at least one of:
- `zenity`
- `kdialog`
- `yad`
- `qarma`

#### AppImage
1. Download `HyPrism-linux-x64.AppImage`
2. Make executable: `chmod +x HyPrism-linux-x64.AppImage`
3. Run: `./HyPrism-linux-x64.AppImage`

#### Flatpak
```bash
flatpak install dev.hyprism.HyPrism
flatpak run dev.hyprism.HyPrism
```

### macOS

1. Download `HyPrism-osx-x64.zip` (or `osx-arm64` for Apple Silicon)
2. Extract and move `HyPrism.app` to Applications
3. Launch from Applications

## Data Directory

HyPrism stores its data (config, instances, profiles, logs) in:

| OS | Path |
|----|------|
| Windows | `%APPDATA%/HyPrism/` |
| Linux | `~/.local/share/HyPrism/` |
| macOS | `~/Library/Application Support/HyPrism/` |

### Directory Structure

```
HyPrism/
├── config.json         # Launcher configuration
├── Instances/          # Game installations (GUID-based folders)
│   └── {GUID}/         # Individual instance
├── Profiles/           # Player profiles and skin backups
├── Logs/               # Application logs
└── Cache/              # Temporary files
```

## First Launch (Onboarding)

On first launch, HyPrism guides you through setup with an onboarding wizard:

1. **Splash Screen** — Welcome to HyPrism
2. **Language Selection** — Choose your preferred language (12 available)
3. **Hytale Authentication** — Log in with your Hytale account
4. **Profile Setup** — Create your first player profile (nickname, avatar)
5. **Initial Settings** — Configure GPU preference and other options

After onboarding:
- The launcher creates the data directory structure
- Your profile and settings are saved
- You can download and install the game from the Dashboard
