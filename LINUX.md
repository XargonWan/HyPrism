# Linux Installation Guide

HyPrism on Linux is available as Flatpak, AppImage, and standalone binary.

## Recommended: Flatpak

Flatpak bundles all dependencies for maximum compatibility.

### Prerequisites

Install Flatpak if not already installed:

```bash
# Ubuntu/Debian
sudo apt install flatpak

# Fedora (already included)
# Arch Linux
sudo pacman -S flatpak
```

### Install HyPrism

1. Download `HyPrism.flatpak` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)

2. **Install the GNOME 44 runtime** (required - contains webkit2gtk-4.0):
   ```bash
   flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
   flatpak install flathub org.gnome.Platform//44
   ```

3. Install HyPrism:
   ```bash
   flatpak install HyPrism.flatpak
   ```

4. Run:
   ```bash
   flatpak run dev.hyprism.HyPrism
   ```

> **Important:** HyPrism requires GNOME Platform 44 because newer GNOME versions (46+) removed webkit2gtk-4.0 which the app depends on.

## Alternative: AppImage

### Prerequisites

Install WebKitGTK 4.0:

```bash
# Ubuntu/Debian
sudo apt install libwebkit2gtk-4.0-37

# Fedora
sudo dnf install webkit2gtk4.0

# Arch Linux / Manjaro
# The webkit2gtk package provides the 4.1 API only. Install webkit2gtk-4.0 from AUR:
yay -S webkit2gtk-4.0
# Or use paru: paru -S webkit2gtk-4.0
```

> **Note for Arch users:** Flatpak is recommended because `webkit2gtk-4.0` is only available from AUR.

### Install & Run

1. Download `HyPrism-x86_64.AppImage` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)
2. Make it executable: `chmod +x HyPrism-x86_64.AppImage`
3. Run: `./HyPrism-x86_64.AppImage`

## Alternative: Binary (tar.gz)

If AppImage doesn't work, use the standalone binary:

1. Download `HyPrism-linux-x86_64.tar.gz` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)
2. Extract: `tar -xzf HyPrism-linux-x86_64.tar.gz`
3. Run: `./HyPrism`

## Troubleshooting

### "libwebkit2gtk-4.0.so.37: cannot open shared object file"

Your system is missing WebKitGTK 4.0. This library was deprecated in GNOME 46+.

**For Flatpak users:** Make sure you have the GNOME 44 runtime installed:
```bash
flatpak install flathub org.gnome.Platform//44
```

**For AppImage/binary users:** Install webkit2gtk-4.0 for your distribution (see AppImage section above).

### "libxml2.so.2: cannot open shared object file"

This is a dependency issue. Use the Flatpak version which bundles all dependencies:
```bash
flatpak install flathub org.gnome.Platform//44
flatpak install HyPrism.flatpak
```

### Arch Linux / GNOME 46+ Issues

Arch Linux and distributions with GNOME 46+ no longer ship webkit2gtk-4.0. Options:

1. **Use Flatpak** (recommended):
   ```bash
   flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
   flatpak install flathub org.gnome.Platform//44
   flatpak install HyPrism.flatpak
   flatpak run dev.hyprism.HyPrism
   ```

2. **Install webkit2gtk-4.0 from AUR**:
   ```bash
   yay -S webkit2gtk-4.0
   # Then use AppImage or binary
   ```

### AppImage won't launch

Try Flatpak instead, or extract and run:
```bash
./HyPrism-x86_64.AppImage --appimage-extract
./squashfs-root/AppRun
```

### Game launches but crashes

1. Update to the latest HyPrism release
2. Ensure you have the latest graphics drivers
3. Try using Flatpak for an isolated environment

## SteamOS / Steam Deck

Flatpak is recommended for Steam Deck:

```bash
flatpak install HyPrism.flatpak
flatpak run dev.hyprism.HyPrism
```

Or use AppImage in Desktop Mode:
```bash
chmod +x HyPrism-x86_64.AppImage
./HyPrism-x86_64.AppImage
```

## Building from Source

See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions.

## Support

Report issues at [GitHub Issues](https://github.com/yyyumeniku/HyPrism/issues)
