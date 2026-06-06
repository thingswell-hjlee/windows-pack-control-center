# Windows Pack Control Center

A local web-based control center for managing AIBox devices, network cameras, and AI-generated events. Built with .NET 8 and React, designed for Windows deployment.

## Architecture

```
+-------------------+        MQTT         +---------------------+
|   AIBox Devices   | ------------------> |   Gateway (.NET 8)  |
+-------------------+                     |                     |
                                          |  - MQTT Receiver    |
                                          |  - Event Normalizer |
                                          |  - SQLite DB        |
                                          |  - REST API         |
                                          |  - SignalR Hub      |
                                          |  - Static Files     |
                                          +----------+----------+
                                                     |
                                            HTTP :8088 / WebSocket
                                                     |
                                          +----------v----------+
                                          |  React Dashboard    |
                                          |  (SPA in wwwroot/)  |
                                          +---------------------+
```

**Data Flow:**
AIBox Device --> MQTT --> Gateway (MqttReceiverService) --> EventNormalizerService --> SQLite --> REST API / SignalR --> React Dashboard

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 8, ASP.NET Core Minimal API |
| MQTT Client | MQTTnet v4 |
| Database | SQLite via EF Core |
| Real-time | SignalR |
| Logging | Serilog |
| Frontend | React 19, TypeScript, Vite |
| Styling | Tailwind CSS v4 |
| State/Fetch | @tanstack/react-query, axios |
| Icons | lucide-react |

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for frontend development)
- Windows 10 or later (for deployment target)

### Development Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd windows-pack-control-center
   ```

2. **Build the frontend:**
   ```bash
   cd web
   npm install
   npm run build
   ```

3. **Run the gateway:**
   ```bash
   cd gateway/src
   dotnet run
   ```

4. **Access the dashboard:** Open http://localhost:8088

### Frontend Development (with hot reload)

```bash
# Terminal 1 - Run the gateway
cd gateway/src
dotnet run

# Terminal 2 - Run Vite dev server
cd web
npm run dev
```

The Vite dev server runs on http://localhost:5173 with API proxy to the gateway.

## Build for Production

### Windows (PowerShell)

```powershell
./build.ps1
```

### Linux / CI

```bash
chmod +x build.sh
./build.sh
```

Both scripts produce a self-contained deployment in the `dist/` folder. The output includes:
- The .NET gateway compiled as a self-contained win-x64 application
- The React frontend pre-built and embedded in the gateway's static files

## Folder Structure

```
windows-pack-control-center/
├── README.md                  # This file
├── build.ps1                  # Windows build script
├── build.sh                   # Linux/CI build script
├── gateway/                   # .NET 8 Gateway backend
│   ├── README.md              # Gateway documentation
│   └── src/
│       ├── Program.cs         # Application entry point
│       ├── Models/            # Entity models (Device, Camera, NormalizedEvent)
│       ├── Data/              # EF Core DbContext
│       ├── Services/          # Background services (MQTT, Normalizer, Status)
│       ├── Hubs/              # SignalR hubs
│       ├── Endpoints/         # REST API endpoint definitions
│       ├── wwwroot/           # Built frontend assets (generated)
│       └── appsettings.json   # Application configuration
├── web/                       # React frontend
│   ├── README.md              # Frontend documentation
│   ├── src/
│   │   ├── pages/             # Page components
│   │   ├── components/        # Reusable UI components
│   │   ├── hooks/             # Custom React hooks
│   │   └── lib/               # Utilities (API client, SignalR)
│   └── vite.config.ts         # Vite build configuration
├── specs/                     # Technical specifications
│   ├── requirements.md        # Requirements document
│   ├── design.md              # Architecture and design
│   └── tasks.md               # Implementation tasks
├── docs/                      # User documentation (Korean)
│   ├── INSTALL.ko.md          # Installation guide
│   ├── USER_GUIDE.ko.md       # User guide
│   └── TROUBLESHOOTING.ko.md  # Troubleshooting guide
├── installer/                 # Deployment/installer docs
│   └── README.md              # Installer instructions
└── .gitignore
```

## Configuration

The gateway is configured via `gateway/src/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| ConnectionStrings:DefaultConnection | Data Source=controlcenter.db | SQLite database path |
| Mqtt:DefaultPort | 1883 | Default MQTT broker port |
| Mqtt:HeartbeatTimeoutSeconds | 60 | Seconds before marking device offline |
| Mqtt:ReconnectDelaySeconds | 5 | MQTT reconnection interval |

## License

Proprietary - Internal use only.
