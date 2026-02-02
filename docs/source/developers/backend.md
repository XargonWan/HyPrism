# Backend (C#)

Location: `Backend/` and `Program.cs` contain the main app entrypoints and services.

Important services:

- `ButlerService` — handles downloads, patching orchestration and progress reporting.
- `ClientPatcher` — performs safe binary patching and backups.
- `DiscordService` — optional Discord RPC presence and announcement integration.
- `AuthService`, `AppService` — miscellaneous app services for authentication and state management.

IPC / Bridge

- The backend exposes a set of bridge calls in `Program.cs` (e.g. `GetProfiles`, `PatchClient`, `GetDiscordAnnouncement`, `SetShowDiscordAnnouncements`). These are consumed by the frontend via the `bridge` layer.

Testing and debugging

- Use `dotnet run` with `DOTNET_ROLL_FORWARD=Major` for quick tests when runtime versions differ.
- Inspect logs emitted by `Logger.cs` for diagnostics.
