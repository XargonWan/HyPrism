# Installation

Supported platforms:

- Linux (including Flatpak)
- macOS
- Windows

## Download

Downloads are available from the GitHub releases: https://github.com/yyyumeniku/HyPrism/releases

## Flatpak (recommended for older Linux systems)

1. Install Flatpak and Flathub
2. Install HyPrism using the provided Flatpak bundle (see `packaging/flatpak` in the repo)

## Run from source (advanced users)

1. Build frontend: `cd frontend && npm ci && npm run build`
2. Build backend: `dotnet build` or use `scripts/run.sh` which automates building and will install frontend deps if missing.

See developer docs for more build tips and troubleshooting.
