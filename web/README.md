# Control Center Web Dashboard

React + TypeScript frontend for the Windows Control Center local web dashboard.

## Tech Stack

- React 19 with TypeScript
- Vite (build tool)
- Tailwind CSS v4 (styling)
- React Router (client-side routing)
- TanStack React Query (data fetching and caching)
- Axios (HTTP client)
- @microsoft/signalr (real-time events)
- Lucide React (icons)
- Day.js (date formatting)

## Pages

- **Dashboard** - Summary cards, recent events, device status overview
- **AIBox Devices** - Device list with CRUD operations
- **Cameras** - Camera list grouped by device with CRUD operations
- **Events** - Filterable event timeline with real-time updates via SignalR
- **Event Detail** - Full event details, acknowledge, action memo
- **Reports** - Date range filter and CSV export

## Development

```bash
npm install
npm run dev
```

The dev server proxies `/api` and `/hubs` requests to `http://localhost:8088` (the gateway).

## Production Build

```bash
npm run build
```

Output goes to `../gateway/src/wwwroot/` so the .NET gateway serves the frontend as static files.

## Architecture

```
src/
  components/   - Reusable UI components (StatusBadge, SeverityBadge, Modal, etc.)
  hooks/        - Custom React hooks (useSignalR)
  pages/        - Page-level components
  services/     - API client (axios) and SignalR connection management
  types/        - TypeScript interfaces matching backend models
```

## SignalR Real-Time Events

The app connects to the gateway's SignalR hub at `/hubs/events` and listens for:

- `NewEvent` - New normalized event received
- `DeviceStatusChanged` - Device online/offline/warning status update
- `CameraStatusChanged` - Camera status update

Connection status is displayed in the sidebar footer.
