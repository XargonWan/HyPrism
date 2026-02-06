# Contribution Guide

We welcome community contributions! Whether it's bug fixes, documentation improvements, or new features — your help is valuable.

---

## 🛠️ Development Requirements

| Tool | Requirement |
|------|-------------|
| **IDE** | Visual Studio 2022, Rider, or VS Code |
| **SDK** | .NET 10.0 (Preview) |
| **Git** | Basic knowledge of git operations |
| **OS** | Windows 10+, Linux (Ubuntu 22.04+), macOS 12+ |

---

## 📋 Workflow

### 1. Fork the Repository

Click "Fork" on GitHub to create your copy.

### 2. Clone

```bash
git clone https://github.com/<YOUR_USERNAME>/HyPrism.git
cd HyPrism
```

### 3. Branching Strategy

> ⚠️ Never commit directly to `master`/`main`!

**Naming convention:** `type/short-description`

| Type | Example |
|------|---------|
| New feature | `feat/mod-manager-ui` |
| Fix | `fix/crash-on-startup` |
| Documentation | `docs/update-readme` |
| Refactoring | `refactor/split-appservice` |
| Styling | `style/button-animations` |

```bash
git checkout -b feat/my-new-feature
```

### 4. Development

1. Make changes
2. Ensure code compiles: `dotnet build`
3. Check for errors: `dotnet run`
4. Follow [CodingStandards.md](CodingStandards.md)

### 5. Commit

```bash
git add .
git commit -m "feat: add mod manager"
```

**Commit format:**
- `feat:` — new feature
- `fix:` — bug fix
- `docs:` — documentation
- `refactor:` — refactoring
- `style:` — formatting/styles
- `chore:` — other changes

Reference Issues: `fix: auth error (#123)`

### 6. Push and Pull Request

```bash
git push origin feat/my-new-feature
```

Open a PR to the official repository and fill out the template.

---

## 📐 Code Rules

### MVVM

- ViewModels **must not** reference Avalonia Controls
- Use `RaiseAndSetIfChanged` and `ReactiveCommand`
- Get services via DI

### Styles

- Use `StaticResource`/`DynamicResource`
- SVG for icons (not Bitmap)
- Follow [StylingGuide.md](StylingGuide.md)

### Tests

- Cover critical services with tests
- Use mocks for dependencies

---

## 🐛 Reporting Bugs

If you find a bug but can't fix it:

1. Go to the **Issues** tab
2. Check if such issue already exists
3. Create a new Issue with:
   - Logs (`Logs/*.log`)
   - Screenshots (for visual bugs)
   - Steps to reproduce
   - OS and .NET version

---

## 📁 Structure for New Components

When creating a new UI component:

```
UI/Components/<Category>/<ComponentName>/
├── <ComponentName>.axaml
├── <ComponentName>.axaml.cs
└── <ComponentName>ViewModel.cs  (optional)
```

See [UIComponentGuide.md](UIComponentGuide.md).

---

## 🔍 Code Review

During PR review we check:

- [ ] Code compiles without warnings
- [ ] Coding standards are followed
- [ ] MVVM pattern is respected
- [ ] No hardcoded values
- [ ] Documentation is updated (if needed)

---

## 📚 Useful Links

- [CodingStandards.md](CodingStandards.md) — Code Standards
- [MVVMPatterns.md](MVVMPatterns.md) — MVVM Patterns
- [UIComponentGuide.md](UIComponentGuide.md) — Creating Components
- [Architecture.md](../Technical/Architecture.md) — Architecture
