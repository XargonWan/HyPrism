# Configuration

HyPrism stores its configuration in `config.json` inside the data directory.

## Settings

Access settings through the **Settings** page (gear icon in sidebar).

### General

| Setting | Description | Default |
|---------|-------------|---------|
| Language | UI language (12 available) | System language or en-US |
| Close after launch | Close launcher when game starts | false |
| Launch on startup | Auto-start with OS | false |
| Minimize to tray | Minimize to system tray | false |

### Appearance

| Setting | Description | Default |
|---------|-------------|---------|
| Accent color | Theme accent color | Purple (#7C5CFC) |
| Animations | Enable UI animations | true |
| Transparency | Glass-morphism effects | true |
| Background mode | Dashboard background style | default |

### Game

| Setting | Description | Default |
|---------|-------------|---------|
| Resolution | Game window resolution | 1920x1080 |
| RAM allocation | Memory for game (MB) | 4096 |
| Sound | Game sound enabled | true |
| GPU preference | Graphics adapter selection | auto |

#### GPU Preference Options

| Value | Description |
|-------|-------------|
| auto | Let the system choose the best GPU |
| dedicated | Force dedicated graphics (NVIDIA/AMD) |
| integrated | Force integrated graphics (Intel/AMD) |

### Advanced

| Setting | Description | Default |
|---------|-------------|---------|
| Developer mode | Show developer tools | false |
| Verbose logging | Extended log output | false |
| Pre-release | Receive pre-release updates | false |
| Launcher branch | Release or pre-release channel | release |
| Data directory | Custom data storage path | Platform default |

## Instance Management

Instead of a single game installation, HyPrism uses **instances** — isolated game installations in separate folders.

### Instance Structure

Each instance is stored in `Instances/{GUID}/` where `{GUID}` is a unique identifier:

```
Instances/
├── a1b2c3d4-e5f6-7890-abcd-ef1234567890/
│   ├── game/           # Game files
│   ├── mods/           # Installed mods
│   └── instance.json   # Instance metadata
└── ...
```

### Managing Instances

- **Create** — Download a new game installation
- **Switch** — Select which instance to launch
- **Delete** — Remove an instance (confirmation required)
- **View details** — See version, patch status, installed mods

## Profiles

HyPrism supports multiple player profiles. Switch between profiles via the sidebar profile selector.

### Profile Data

Each profile stores:
- **Nickname** — Display name in-game
- **UUID** — Unique player identifier
- **Avatar** — Profile picture (optional)
- **Skin backup** — Saved skin data

### Skin Backup

Profiles can back up your Hytale skin. Backups are stored in:

```
Profiles/
├── {ProfileUUID}/
│   ├── profile.json    # Profile metadata
│   └── skin.png        # Backed up skin
└── ...
```

Use the profile menu to:
- **Backup skin** — Save current skin to profile
- **Restore skin** — Apply backed up skin to account

## Configuration File

**Location:**
- Windows: `%APPDATA%/HyPrism/config.json`
- Linux: `~/.local/share/HyPrism/config.json`
- macOS: `~/Library/Application Support/HyPrism/config.json`

The config file is JSON and can be edited manually, but it's recommended to use the Settings page.

### Data Directory

HyPrism uses a fixed launcher data directory based on your platform default.

- The path is shown in **Settings** → **Data**
- Launcher data directory relocation is not supported
- The launcher provides an **Open** button to open the containing folder
