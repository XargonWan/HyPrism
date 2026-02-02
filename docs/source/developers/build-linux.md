# Building on Linux (Developer Guide)

This page summarizes common Linux build steps. The project already contains an extended guide at `docs/BUILD-LINUX.md`. See that file for detailed, distro-specific instructions and troubleshooting.

Quick steps:

- Build frontend: `cd frontend && npm ci && npm run build`
- Build backend: `dotnet build`
- Run: `DOTNET_ROLL_FORWARD=Major dotnet bin/Debug/net8.0/HyPrism.dll`

For the full Linux guide, open `docs/BUILD-LINUX.md` in the repo root.
