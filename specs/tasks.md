# Implementation Tasks

## Status Legend

- [x] Completed
- [ ] Pending

---

## Phase 1: Gateway Backend

- [x] Project scaffolding (.NET 8 Minimal API)
- [x] Entity models (Device, Camera, NormalizedEvent)
- [x] EF Core SQLite DbContext with migrations
- [x] MqttReceiverService - MQTT client management
- [x] EventNormalizerService - Event processing pipeline
- [x] DeviceStatusService - Heartbeat monitoring
- [x] REST API - Devices endpoints (CRUD)
- [x] REST API - Cameras endpoints (CRUD)
- [x] REST API - Events endpoints (list, filter, ack, memo, export)
- [x] REST API - Dashboard summary endpoint
- [x] SignalR EventHub - Real-time event broadcasting
- [x] Static file serving + SPA fallback
- [x] Serilog configuration (console + file)
- [x] CORS configuration for development

## Phase 2: React Frontend

- [x] Vite + React + TypeScript project setup
- [x] Tailwind CSS configuration
- [x] API client (axios) setup
- [x] SignalR client integration
- [x] React Router configuration
- [x] Layout component (sidebar navigation)
- [x] Dashboard page (summary stats, recent events)
- [x] Devices page (list, create, edit, delete)
- [x] Cameras page (list, create, edit, delete)
- [x] Events page (filterable list, acknowledge, memo)
- [x] Reports page (date range filter, CSV export)
- [x] Real-time event updates via SignalR
- [x] Responsive design for desktop

## Phase 3: Documentation and Build

- [x] Top-level README.md
- [x] specs/requirements.md
- [x] specs/design.md
- [x] specs/tasks.md
- [x] docs/INSTALL.ko.md (Korean installation guide)
- [x] docs/USER_GUIDE.ko.md (Korean user guide)
- [x] docs/TROUBLESHOOTING.ko.md (Korean troubleshooting)
- [x] installer/README.md
- [x] build.ps1 (Windows build script)
- [x] build.sh (Linux/CI build script)
- [x] gateway/README.md updates
- [x] web/README.md updates

## Phase 4: Future Enhancements (Not in current scope)

- [ ] Windows Service wrapper (sc.exe registration)
- [ ] NSIS/WiX installer package
- [ ] Auto-start on Windows boot
- [ ] Database backup and restore
- [ ] User authentication (if multi-user required)
- [ ] Cloud sync integration
- [ ] Multi-language UI support
- [ ] Alert notification (email/SMS)
- [ ] PTZ camera control via ONVIF
- [ ] Event video clip recording
