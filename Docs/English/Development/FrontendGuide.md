# Frontend Guide

## Overview

The frontend is a React 19 SPA built with Vite 7 and TypeScript 5.9. It runs inside an Electron BrowserWindow loaded from `file://wwwroot/index.html`.

## Stack

| Library | Purpose |
|---------|---------|
| React 19 | UI framework |
| TypeScript 5.9 | Type safety |
| Vite 7 | Build tool |
| TailwindCSS v4 | Utility-first CSS |
| GSAP 3 + @gsap/react | Animations |
| Lucide React | Icons |
| React Router DOM | Client-side routing |

## Pages

| Page | File | Route | Description |
|------|------|-------|-------------|
| Dashboard | `pages/Dashboard.tsx` | `/` | Game launch, progress, status |
| News | `pages/News.tsx` | `/news` | Hytale news feed |
| Settings | `pages/Settings.tsx` | `/settings` | App settings |
| Mod Manager | `pages/ModManager.tsx` | `/mods` | Browse and manage mods |

## Components

| Component | File | Description |
|-----------|------|-------------|
| TitleBar | `components/TitleBar.tsx` | Frameless window title bar with controls |
| Sidebar | `components/Sidebar.tsx` | Navigation sidebar with icons |
| GlassCard | `components/GlassCard.tsx` | Glass-morphism card wrapper |

## Creating a Component

```tsx
import { ipc } from '../lib/ipc';
import type { Profile } from '../lib/ipc';

interface Props {
  profileId: string;
}

export function ProfileCard({ profileId }: Props) {
  const [profile, setProfile] = useState<Profile | null>(null);

  useEffect(() => {
    ipc.profile.get().then(setProfile);
  }, [profileId]);

  if (!profile) return <div>Loading...</div>;
  return (
    <div className="p-4 rounded-xl" style={{ backgroundColor: 'var(--bg-light)' }}>
      {profile.name}
    </div>
  );
}
```

## Theming

All theme colors are CSS custom properties defined in `Frontend/src/index.css`:

```css
:root {
  --bg-darkest: #0D0D10;    /* App background */
  --bg-dark: #14141A;        /* Sidebar, title bar */
  --bg-medium: #1C1C26;      /* Cards */
  --bg-light: #252533;       /* Elevated surfaces */
  --bg-lighter: #2E2E40;     /* Borders, scrollbar */
  --text-primary: #F0F0F5;   /* Main text */
  --text-secondary: #A0A0B8; /* Secondary text */
  --text-muted: #6B6B80;     /* Muted / disabled */
  --accent: #7C5CFC;         /* Primary accent (purple) */
  --accent-hover: #6A4AE8;   /* Accent hover */
  --success: #4ADE80;
  --warning: #FBBF24;
  --error: #F87171;
}
```

**Usage:**
- Tailwind classes for layout/spacing: `className="flex items-center gap-2 p-4"`
- CSS vars for theme colors: `style={{ color: 'var(--accent)' }}`
- Never hardcode hex colors — always use CSS variables

## Animations (GSAP)

Page transitions and micro-interactions use GSAP:

```tsx
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';

export function MyPage() {
  const containerRef = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from(containerRef.current, {
      opacity: 0, y: 20, duration: 0.5, ease: 'power2.out'
    });
  }, []);

  return <div ref={containerRef}>...</div>;
}
```

## IPC Usage

All IPC is accessed through the auto-generated `ipc` object:

```typescript
import { ipc } from '../lib/ipc';

// Invoke (request/reply)
const settings = await ipc.settings.get();

// Send (fire-and-forget)
ipc.windowCtl.minimize();

// Event subscription
ipc.game.onProgress((data) => setProgress(data.progress));

// Open URL
ipc.browser.open('https://example.com');
```

## Instance State Resilience

- The app auto-selects a fallback instance when the backend returns a list but no explicit selection.
- The fallback prefers an installed instance, then first available instance.
- When game-running state is true but running branch/version is temporarily unknown, instance controls treat the currently selected instance as the active one to avoid a permanently disabled Play button.

## Context Providers

Game state is managed via `GameContext`:

```tsx
const { isPlaying, launch, cancel } = useGame();
```

Add new contexts in `Frontend/src/contexts/` for other domain state.

## Icons

All icons come from **Lucide React** — no custom SVG files:

```tsx
import { Settings, Download, Play } from 'lucide-react';

<Settings size={18} style={{ color: 'var(--text-secondary)' }} />
```
