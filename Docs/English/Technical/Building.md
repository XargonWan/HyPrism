# Building HyPrism

Detailed instructions for compiling HyPrism from source code.

---

## 📋 Requirements

### Required Dependencies

| Component | Requirement |
|-----------|-------------|
| **.NET SDK** | 10.0 (Preview) |
| **Git** | Any modern version |
| **IDE** | Visual Studio 2022, Rider, or VS Code |

### Operating Systems

| OS | Minimum Version |
|----|-----------------|
| Windows | 10 (1809+) or 11 |
| Linux | Ubuntu 22.04+, Fedora 38+, Arch |
| macOS | 12 Monterey+ |

---

## 🔧 Build Instructions

### Cloning

```bash
git clone https://github.com/yyyumeniku/HyPrism.git
cd HyPrism
```

### Debug Build

```bash
# Restore NuGet packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Release Build

```bash
dotnet build -c Release
```

---

## 📦 Publishing (Standalone)

### Windows (x64)

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `bin/Release/net10.0/win-x64/publish/HyPrism.exe`

### Linux (x64)

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Output: `bin/Release/net10.0/linux-x64/publish/HyPrism`

### macOS (Intel)

```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

### macOS (Apple Silicon)

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

---

## 🐧 Linux: Additional Dependencies

Avalonia uses SkiaSharp, which requires native libraries.

### Ubuntu/Debian

```bash
sudo apt install libfontconfig1 libice6 libsm6 libx11-6 libxext6 libxrender1
```

### Fedora

```bash
sudo dnf install fontconfig libX11 libXext libXrender libSM libICE
```

### Arch Linux

```bash
sudo pacman -S fontconfig libx11 libxext libxrender libsm libice
```

### .NET Version Issues

If .NET 10 Preview causes issues:

```bash
export DOTNET_ROLL_FORWARD=Major
dotnet run
```

---

## 📦 Flatpak Build

Flatpak manifests are in `Packaging/flatpak/`.

### Requirements

```bash
# Ubuntu/Debian
sudo apt install flatpak-builder

# Fedora
sudo dnf install flatpak-builder
```

### Build and Install

```bash
flatpak-builder --user --install build-dir Packaging/flatpak/dev.hyprism.HyPrism.json
```

### Run

```bash
flatpak run dev.hyprism.HyPrism
```

---

## 🍎 macOS Build

### App Bundle

To create an `.app` bundle:

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

Then create the structure:

```
HyPrism.app/
├── Contents/
│   ├── Info.plist          # From Packaging/macos/
│   ├── MacOS/
│   │   └── HyPrism         # Executable
│   └── Resources/
│       └── Icon.icns
```

### DMG

Use `create-dmg` or `hdiutil` to create `.dmg`.

---

## 🛠️ Project Configuration

### HyPrism.csproj

Key parameters:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>13</LangVersion>
    <AvaloniaVersion>11.3.11</AvaloniaVersion>
</PropertyGroup>
```

### Dependencies

| Package | Version |
|---------|---------|
| Avalonia | 11.3.11 |
| Avalonia.Desktop | 11.3.11 |
| ReactiveUI | 11.3.9 |

| SkiaSharp | 3.116.1 |
| Serilog | 4.3.0 |

---

## 🔧 Build Scripts

### Linux (`Scripts/build-linux.sh`)

```bash
#!/bin/bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### Docker (`Scripts/Dockerfile.build`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -r linux-x64 --self-contained
```

---

## ⚠️ Known Issues

### Linux: libSkiaSharp.so

If "Unable to load libSkiaSharp.so" error appears:

```bash
# Install library manually or use
export LD_LIBRARY_PATH=/path/to/skia:$LD_LIBRARY_PATH
```

### macOS: Gatekeeper

On first launch on macOS:

```bash
xattr -d com.apple.quarantine /path/to/HyPrism.app
```

---

## 📚 Additional Resources

- [Installation.md](../User/Installation.md) — User installation
- [ProjectStructure.md](ProjectStructure.md) — Project structure
- [Architecture.md](Architecture.md) — Architecture

## Troubleshooting

*   **SkiaSharp Errors (Linux):** Ensure `libSkiaSharp.so` can be found. You may need to install `SkiaSharp.NativeAssets.Linux` NuGet package or install `libskia` system-wide.
*   **"Preview" Runtime needed:** If the `.csproj` specifies `net10.0` and you only have `net8.0`, you must install the .NET 10 Preview SDK from Microsoft.
