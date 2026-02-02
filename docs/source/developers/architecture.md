# Architecture Overview

HyPrism is structured into two main components:

- **Backend (C# / .NET 8)**: core logic, launch/patcher/back-end services, IPC API exposed to the frontend.
- **Frontend (TypeScript / React / Vite)**: UI, user interaction, and bridge code that calls into the backend via an internal API.

Other components:

- **Packaging**: scripts and flatpak manifests under `packaging/flatpak` and `scripts/Dockerfile.build`.
- **Bot / Announcements**: a small Discord bot script in `scripts/run-discord-bot.sh` that posts announcements to a configured channel.

Design notes:

- The backend exposes a compact IPC/bridge API that the frontend triggers for actions such as profile management, patching, onboarding and fetching announcements.
- The `ClientPatcher` is designed to safely detect and patch the game binary, creating backups and flag files to avoid double-patching.
- The `ButlerService` and download/patch subsystem handle large downloads with progress feedback and retries.
