# Frontend (TypeScript / React)

Location: `frontend/`

Key points:

- Built with Vite and React (TypeScript). The entry is `frontend/src/main.tsx` and components are under `frontend/src/components`.
- Bridge: `frontend/src/api/bridge.ts` is the frontend layer that communicates with the backend IPC.
- Build: `cd frontend && npm ci && npm run build` produces `frontend/dist/` which is served by the backend at runtime.

Development

- Use `npm run dev` in `frontend/` to run the dev server.
- The main UI components include `ModManager`, `SettingsModal`, `Onboarding`, and others listed in `frontend/src/components`.
