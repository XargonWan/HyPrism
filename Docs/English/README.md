# HyPrism Documentation

> **📍 Documentation Version:** 3.0 (February 2026)  
> **🔄 Last Update:** After Photino → Avalonia UI migration

Welcome to the official documentation of **HyPrism** — a cross-platform Hytale launcher built with .NET 10 and Avalonia UI.

---

## 📚 Documentation Navigation

### 🌐 [General Information](General/Introduction.md)
Project mission, technology stack, and key features.
- [Introduction](General/Introduction.md) — Project introduction and philosophy
- [Features](General/Features.md) — Complete feature list

### 👤 [User Guide](User/Installation.md)
Instructions for end users.
- [Installation](User/Installation.md) — Installation on Windows, Linux, and macOS
- [Configuration](User/Configuration.md) — Settings and configuration files

### 🏗️ [Technical Documentation](Technical/Architecture.md)
Deep dive into architecture and internals.
- [Architecture](Technical/Architecture.md) — MVVM pattern, Service Layer, Data Flow
- [Project Structure](Technical/ProjectStructure.md) — Files and directories structure
- [Building](Technical/Building.md) — Build instructions
- [Localization](Technical/Localization.md) — Localization system (JSON format)
- **[Services Reference](Technical/ServicesReference.md)** — Services reference guide *(NEW)*

### 🎨 [UI Development](Development/UIComponentGuide.md) *(NEW)*
Guide for creating and modifying UI components.
- [UI Component Guide](Development/UIComponentGuide.md) — Creating new components
- [Styling Guide](Development/StylingGuide.md) — Working with styles and themes *(NEW)*
- [MVVM Patterns](Development/MVVMPatterns.md) — MVVM patterns in the project *(NEW)*

### 🔧 [Development](Development/Contributing.md)
Guides for contributors and developers.
- [Contributing](Development/Contributing.md) — Contribution process
- [Coding Standards](Development/CodingStandards.md) — Code standards and best practices

### 🔄 [Migration](Technical/MigrationGuide.md) *(NEW)*
Complete migration guide from Photino to Avalonia UI.
- [Migration Guide](Technical/MigrationGuide.md) — What changed and why
- [Breaking Changes](Technical/BreakingChanges.md) — Critical changes *(NEW)*

---

## 🚀 Quick Start for Developers

```bash
# Clone repository
git clone https://github.com/yyyumeniku/HyPrism.git
cd HyPrism

# Build project
dotnet build

# Run
dotnet run
```

**Requirements:**
- .NET 10 SDK (Preview)
- On Linux: `libSkiaSharp.so` or `DOTNET_ROLL_FORWARD=Major`

---

## 📋 Key Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 (Preview) | Platform |
| Avalonia UI | 11.3.11 | UI Framework |
| ReactiveUI | 11.3.9 | Reactive MVVM |

| SkiaSharp | 3.116.1 | Graphics Rendering |
| Serilog | 4.3.0 | Logging |

---

## ❓ Need Help?

- 📖 Start with [Introduction](General/Introduction.md)
- 🐛 Report issues on [GitHub Issues](https://github.com/yyyumeniku/HyPrism/issues)
- 💬 Join the discussion on [Discord](https://discord.gg/hyprism)
