# Сборка HyPrism

Подробная инструкция по компиляции HyPrism из исходного кода.

---

## 📋 Требования

### Обязательные зависимости

| Компонент | Требование |
|-----------|------------|
| **.NET SDK** | 10.0 (Preview) |
| **Git** | Любая современная версия |
| **IDE** | Visual Studio 2022, Rider, или VS Code |

### Операционные системы

| ОС | Минимальная версия |
|----|-------------------|
| Windows | 10 (1809+) или 11 |
| Linux | Ubuntu 22.04+, Fedora 38+, Arch |
| macOS | 12 Monterey+ |

---

## 🔧 Инструкция по сборке

### Клонирование

```bash
git clone https://github.com/yyyumeniku/HyPrism.git
cd HyPrism
```

### Debug сборка

```bash
# Восстановление NuGet пакетов
dotnet restore

# Сборка
dotnet build

# Запуск
dotnet run
```

### Release сборка

```bash
dotnet build -c Release
```

---

## 📦 Публикация (Standalone)

### Windows (x64)

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Результат: `bin/Release/net10.0/win-x64/publish/HyPrism.exe`

### Linux (x64)

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Результат: `bin/Release/net10.0/linux-x64/publish/HyPrism`

### macOS (Intel)

```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

### macOS (Apple Silicon)

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

---

## 🐧 Linux: дополнительные зависимости

Avalonia использует SkiaSharp, который требует нативных библиотек.

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

### Проблемы с версией .NET

Если .NET 10 Preview вызывает проблемы:

```bash
export DOTNET_ROLL_FORWARD=Major
dotnet run
```

---

## 📦 Flatpak сборка

Манифесты Flatpak находятся в `Packaging/flatpak/`.

### Требования

```bash
# Ubuntu/Debian
sudo apt install flatpak-builder

# Fedora
sudo dnf install flatpak-builder
```

### Сборка и установка

```bash
flatpak-builder --user --install build-dir Packaging/flatpak/dev.hyprism.HyPrism.json
```

### Запуск

```bash
flatpak run dev.hyprism.HyPrism
```

---

## 🍎 macOS сборка

### App Bundle

Для создания `.app` bundle используйте:

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

Затем создайте структуру:

```
HyPrism.app/
├── Contents/
│   ├── Info.plist          # Из Packaging/macos/
│   ├── MacOS/
│   │   └── HyPrism         # Исполняемый файл
│   └── Resources/
│       └── Icon.icns
```

### DMG

Используйте `create-dmg` или `hdiutil` для создания `.dmg`.

---

## 🛠️ Конфигурация проекта

### HyPrism.csproj

Ключевые параметры:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>13</LangVersion>
    <AvaloniaVersion>11.3.11</AvaloniaVersion>
</PropertyGroup>
```

### Зависимости

| Пакет | Версия |
|-------|--------|
| Avalonia | 11.3.11 |
| Avalonia.Desktop | 11.3.11 |
| ReactiveUI | 11.3.9 |

| SkiaSharp | 3.116.1 |
| Serilog | 4.3.0 |

---

## 🔧 Скрипты сборки

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

## ⚠️ Известные проблемы

### Linux: libSkiaSharp.so

Если появляется ошибка "Unable to load libSkiaSharp.so":

```bash
# Установите библиотеку вручную или используйте
export LD_LIBRARY_PATH=/path/to/skia:$LD_LIBRARY_PATH
```

### macOS: Gatekeeper

При первом запуске на macOS:

```bash
xattr -d com.apple.quarantine /path/to/HyPrism.app
```

---

## 📚 Дополнительные ресурсы

- [Installation.md](../User/Installation.md) — Установка для пользователей
- [ProjectStructure.md](ProjectStructure.md) — Структура проекта
- [Architecture.md](Architecture.md) — Архитектура

## Troubleshooting

*   **SkiaSharp Errors (Linux):** Ensure `libSkiaSharp.so` can be found. You may need to install `SkiaSharp.NativeAssets.Linux` NuGet package or install `libskia` system-wide.
*   **"Preview" Runtime needed:** If the `.csproj` specifies `net10.0` and you only have `net8.0`, you must install the .NET 10 Preview SDK from Microsoft.
