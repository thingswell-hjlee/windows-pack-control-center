# Control Center Gateway

ASP.NET Core 8 backend for the Windows Pack Control Center. This gateway serves as the central hub for receiving events from AIBox devices via MQTT, normalizing them, storing them in SQLite, and providing a REST API and real-time SignalR hub for the frontend dashboard.

## Architecture

- **MQTT Receiver** - Connects to MQTT brokers (one per registered AIBox device) and subscribes to event/status topics
- **Event Normalizer** - Transforms raw MQTT payloads into a standardized event schema and stores them in SQLite
- **Device Status Service** - Monitors device heartbeats and updates online/offline status
- **SQLite Storage** - Persists devices, cameras, and normalized events
- **REST API** - CRUD endpoints for devices, cameras, events, and dashboard summary
- **SignalR Hub** - Pushes real-time events to connected dashboard clients
- **Static File Server** - Serves the React frontend from `wwwroot/`

## Building

```bash
cd gateway/src
dotnet build
```

## Running

```bash
cd gateway/src
dotnet run
```

The server will start on port 8088 by default.

## API Endpoints

### Devices
- `GET /api/devices` - List all devices
- `GET /api/devices/{id}` - Get device by ID
- `POST /api/devices` - Create device
- `PUT /api/devices/{id}` - Update device
- `DELETE /api/devices/{id}` - Delete device

### Cameras
- `GET /api/cameras` - List all cameras
- `GET /api/cameras/{id}` - Get camera by ID
- `POST /api/cameras` - Create camera
- `PUT /api/cameras/{id}` - Update camera
- `DELETE /api/cameras/{id}` - Delete camera

### Events
- `GET /api/events` - List events (supports filtering by device_id, event_type, severity, ack_status, start_date, end_date, page, page_size)
- `GET /api/events/{id}` - Get event by ID
- `PUT /api/events/{id}/ack` - Acknowledge event
- `PUT /api/events/{id}/memo` - Add action memo to event
- `GET /api/events/export/csv` - Export events as CSV

### Dashboard
- `GET /api/dashboard/summary` - Get dashboard summary stats

### SignalR
- Hub URL: `/hubs/events`
- Methods: `NewEvent`, `DeviceStatusChanged`, `CameraStatusChanged`

## Configuration

Configuration is in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - SQLite database path (default: `controlcenter.db`)
- `Mqtt:DefaultPort` - Default MQTT port (1883)
- `Mqtt:HeartbeatTimeoutSeconds` - Time before marking a device as offline (60s)

## Database

The application uses SQLite with Entity Framework Core. The database is automatically created on first run with tables:
- `Devices` - AIBox device registrations
- `Cameras` - Network camera configurations
- `Events` - Normalized events from AIBox devices
