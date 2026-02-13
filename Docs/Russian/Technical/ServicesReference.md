# Справочник сервисов

Все сервисы регистрируются как синглтоны в `Bootstrapper.cs` и внедряются через конструктор.

## Основные сервисы (`Services/Core/`)

### IpcService
- **Файл:** `Services/Core/IpcService.cs`
- **Назначение:** Центральный реестр IPC-каналов — единый источник истины для всего взаимодействия React ↔ .NET
- **Ключевой метод:** `RegisterAll()` — регистрирует все IPC-обработчики
- **Аннотации:** Содержит XML-комментарии `@type` и `@ipc`, используемые генератором кода
- **Домены:** config, game, news, profile, settings, i18n, window, browser, mods, console

### ConfigService
- **Файл:** `Services/Core/ConfigService.cs`
- **Тип:** Singleton
- **Назначение:** Конфигурация приложения (сохраняется в JSON)
- **Пути конфигурации:**
  - Windows: `%APPDATA%/HyPrism/config.json`
  - Linux: `~/.config/HyPrism/config.json`
  - macOS: `~/Library/Application Support/HyPrism/config.json`

### Logger
- **Файл:** `Services/Core/Logger.cs`
- **Тип:** Статический класс
- **Назначение:** Структурированное логирование (бэкенд Serilog + цветной вывод в консоль + буфер в памяти)
- **Методы:** `Info()`, `Success()`, `Warning()`, `Error()`, `Debug()`, `Progress()`
- **Файлы логов:** `{appDir}/Logs/{timestamp}.log`

### LocalizationService
- **Файл:** `Services/Core/LocalizationService.cs`
- **Тип:** Singleton (паттерн Instance)
- **Назначение:** Переключение языков в реальном времени с поддержкой вложенных ключей
- **Файлы локалей:** `Assets/Locales/{code}.json`

### BrowserService
- **Файл:** `Services/Core/BrowserService.cs`
- **Назначение:** Открытие URL в системном браузере по умолчанию

### DiscordService
- **Файл:** `Services/Core/DiscordService.cs`
- **Назначение:** Интеграция с Discord Rich Presence

### GitHubService
- **Файл:** `Services/Core/GitHubService.cs`
- **Назначение:** Проверка релизов и функция самообновления

## Игровые сервисы (`Services/Game/`)

### GameSessionService
- **Назначение:** Управление жизненным циклом игры — загрузка, установка, патчинг, запуск
- **Состояния:** preparing → download → install → patching → launching → running → stopped

### ClientPatcher ⚠️
- **Файл:** `Services/Game/ClientPatcher.cs`
- **КРИТИЧЕСКИЙ:** Бинарные манипуляции для целостности игры
- **Правило:** НИКОГДА не изменяйте без явных инструкций

### ModService
- **Назначение:** Просмотр, поиск и управление модами (интеграция с CurseForge)

## Пользовательские сервисы (`Services/User/`)

### ProfileService
- **Назначение:** CRUD-операции с профилями игроков
- **Возможности:** Несколько профилей, управление аватарами, переключение профилей
