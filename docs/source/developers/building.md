# Building HyPrism

## Prerequisites

- .NET 8 SDK
- Node.js (for frontend)
- npm, pnpm or yarn depending on lockfile
- Optional: Docker for packaging builds (`scripts/Dockerfile.build`)

## Quick build

1. Build frontend: `cd frontend && npm ci && npm run build`
2. Build backend: `dotnet build`
3. Run launcher: `DOTNET_ROLL_FORWARD=Major dotnet bin/Debug/net8.0/HyPrism.dll`

## Linux-specific notes

See `docs/BUILD-LINUX.md` (existing file) for additional troubleshooting and CI tips.

## Running the Discord bot locally

- Create a `.env` file with `DISCORD_BOT_TOKEN` and `DISCORD_CHANNEL_ID`.
- Run `scripts/run-discord-bot.sh` to start the announcement bot.
