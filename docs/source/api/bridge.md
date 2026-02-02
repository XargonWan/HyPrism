# Backend Bridge (API)

HyPrism provides a simple RPC-style bridge between the frontend (React) and the backend (.NET). The frontend uses `frontend/src/api/bridge.ts` utilities:

- `callBackend(method, ...args)` — invoke a backend method and return a Promise with the result
- `EventsOn(eventName, callback)` / `EventsOnce` / `EventsOff` — subscribe to backend events

## Example

```ts
import { callBackend } from '../api/bridge';

const profiles = await callBackend('GetProfiles');
```

## Common methods

Below are key methods exposed in `Program.cs`. This is not exhaustive; check `Program.cs` when adding new methods.

- Profile management: `GetProfiles`, `CreateProfile`, `DuplicateProfile`, `SwitchProfile`, `DeleteProfile`, `UpdateProfile`, `SaveCurrentAsProfile`, `OpenCurrentProfileFolder`
- Patching / Game: `PatchClient` (via the app services), `DownloadAndLaunch`, `DownloadOnly`, `LaunchOnly`, `CancelDownload`, `IsGameRunning`, `ExitGame`, `DeleteGame`
- Mods / Mod Manager: `SearchMods`, `GetModFiles`, `GetModCategories`, `InstallModFileToInstance`, `UninstallInstanceMod`, `CheckInstanceModUpdates`, `InstallLocalModFile`, `InstallModFromBase64`, `ExportModsToFolder`, `ImportModList`, `ExportModList`
- Versions / Updates: `GetVersionList`, `SetSelectedVersion`, `CheckLatestNeedsUpdate`, `ForceUpdateLatest`, `DuplicateLatest`, `GetPendingUpdateInfo`
- Settings/UI: `GetShowDiscordAnnouncements`, `SetShowDiscordAnnouncements`, `GetHasCompletedOnboarding`, `SetHasCompletedOnboarding`, `DismissAnnouncement`, `GetRandomUsername`
- Utility: `BrowserOpenURL`, `OpenFolder`, `BrowseFolder`, `GetNews`, `GetDiscordAnnouncement`, `ReactToAnnouncement`

## Adding a new method

1. Add a case in `Program.cs` mapping an RPC method name to an `app` method.
2. Add a TypeScript wrapper (optional) in `frontend/src/api/bridge.ts` for convenience.
3. Add tests and update documentation.

> Tip: Keep method names stable and document their arguments/return values in this file for maintainability.
