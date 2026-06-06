# System Design Document

## 1. System Architecture

The Control Center follows a monolithic architecture where a single .NET 8 process handles all backend concerns and serves the frontend as static files.

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                  Gateway Process (.NET 8)                │
                    │                                                         │
┌──────────┐  MQTT │  ┌─────────────────┐    ┌─────────────────────┐        │
│  AIBox   │──────────>│ MqttReceiver    │───>│ EventNormalizer     │        │
│  Device  │       │  │ Service          │    │ Service             │        │
└──────────┘       │  └─────────────────┘    └──────────┬──────────┘        │
                   │                                     │                    │
                   │                          ┌──────────v──────────┐        │
                   │                          │   SQLite Database    │        │
                   │                          │  (controlcenter.db)  │        │
                   │                          └──────────┬──────────┘        │
                   │                                     │                    │
                   │  ┌─────────────────┐    ┌──────────v──────────┐        │
                   │  │  Static Files   │    │    REST API          │        │
                   │  │  (wwwroot/)     │    │  /api/devices        │        │
                   │  └────────┬────────┘    │  /api/cameras        │        │
                   │           │             │  /api/events         │        │
                   │           │             │  /api/dashboard      │        │
                   │           │             └──────────┬──────────┘        │
                   │           │                        │                    │
                   │           │             ┌──────────v──────────┐        │
                   │           │             │   SignalR Hub        │        │
                   │           │             │  /hubs/events        │        │
                   │           │             └──────────┬──────────┘        │
                   └───────────┼────────────────────────┼────────────────────┘
                               │                        │
                          HTTP :8088              WebSocket :8088
                               │                        │
                   ┌───────────v────────────────────────v────────────────────┐
                   │              React Dashboard (Browser)                   │
                   │  - Device Management                                    │
                   │  - Camera Management                                    │
                   │  - Real-time Event Feed                                 │
                   │  - Event Acknowledgment                                 │
                   │  - CSV Export                                            │
                   └─────────────────────────────────────────────────────────┘
```

## 2. Component Descriptions

### 2.1 MqttReceiverService (Background Service)

- Manages one MQTT client per registered device
- Subscribes to `aibox/+/event/#` and `aibox/+/status/#` topics
- Parses incoming JSON payloads
- Enqueues raw messages for the EventNormalizerService
- Handles reconnection on disconnect with configurable delay

### 2.2 EventNormalizerService (Background Service)

- Consumes raw MQTT messages from an in-memory channel
- Transforms varied AIBox event formats into the NormalizedEvent schema
- Assigns severity based on event type
- Persists events to SQLite via EF Core
- Broadcasts new events to SignalR hub

### 2.3 DeviceStatusService (Background Service)

- Periodically checks device heartbeat timestamps
- Marks devices as offline if no heartbeat received within HeartbeatTimeoutSeconds (default: 60)
- Notifies dashboard clients of status changes via SignalR

### 2.4 REST API (Minimal API Endpoints)

- `DevicesEndpoints` - CRUD for AIBox devices
- `CamerasEndpoints` - CRUD for cameras
- `EventsEndpoints` - Event listing with filters, acknowledgment, memo, CSV export
- `DashboardEndpoints` - Summary statistics

### 2.5 SignalR Hub (EventHub)

- Real-time event broadcasting to connected clients
- Methods:
  - `NewEvent` - Pushes new normalized events
  - `DeviceStatusChanged` - Notifies device online/offline transitions
  - `CameraStatusChanged` - Notifies camera status updates

### 2.6 React Dashboard (SPA)

- Single-page application served from `wwwroot/`
- Pages: Dashboard, Devices, Cameras, Events, Reports
- Uses @tanstack/react-query for server state management
- Uses @microsoft/signalr for real-time event subscriptions
- Tailwind CSS for responsive layout

## 3. Data Flow

### 3.1 Event Ingestion Flow

```
1. AIBox publishes event JSON to MQTT topic (e.g., aibox/device001/event/intrusion)
2. MqttReceiverService receives the message
3. Raw payload is written to an in-memory Channel<T>
4. EventNormalizerService reads from the channel
5. Payload is parsed and mapped to NormalizedEvent entity
6. Event is saved to SQLite via EF Core
7. Event is broadcast via SignalR to all connected dashboard clients
8. Dashboard updates the event feed in real-time
```

### 3.2 Device Status Flow

```
1. AIBox publishes heartbeat to aibox/{device_id}/status/heartbeat
2. MqttReceiverService updates the device's last-seen timestamp
3. DeviceStatusService periodically checks all device timestamps
4. If a device exceeds HeartbeatTimeoutSeconds, it is marked offline
5. Status change is broadcast via SignalR
6. Dashboard updates device status indicator
```

## 4. Database Schema

### 4.1 Devices Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| device_id | TEXT | PRIMARY KEY | Unique device identifier |
| device_name | TEXT | NOT NULL | Human-readable name |
| site_id | TEXT | NULLABLE | Site identifier |
| site_name | TEXT | NULLABLE | Site name |
| location | TEXT | NULLABLE | Physical location |
| ip_address | TEXT | NULLABLE | Device IP address |
| mqtt_host | TEXT | NULLABLE | MQTT broker hostname |
| mqtt_port | INTEGER | DEFAULT 1883 | MQTT broker port |
| mqtt_username | TEXT | NULLABLE | MQTT authentication |
| mqtt_password | TEXT | NULLABLE | MQTT authentication |
| event_topic | TEXT | NULLABLE | Custom event topic pattern |
| status_topic | TEXT | NULLABLE | Custom status topic pattern |
| enabled | INTEGER | DEFAULT 1 | Device enabled flag |
| description | TEXT | NULLABLE | Notes |
| created_at | TEXT | NOT NULL | Creation timestamp |
| updated_at | TEXT | NOT NULL | Last update timestamp |
| status | TEXT | DEFAULT 'unknown' | online/offline/unknown |

### 4.2 Cameras Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| camera_id | TEXT | PRIMARY KEY | Unique camera identifier |
| camera_name | TEXT | NOT NULL | Human-readable name |
| site_id | TEXT | NULLABLE | Site identifier |
| site_name | TEXT | NULLABLE | Site name |
| device_id | TEXT | FK -> Devices | Associated AIBox device |
| channel_no | INTEGER | NOT NULL | Camera channel number |
| location | TEXT | NULLABLE | Installation location |
| ip_address | TEXT | NULLABLE | Camera IP address |
| rtsp_url | TEXT | NULLABLE | RTSP stream URL |
| onvif_enabled | INTEGER | DEFAULT 0 | ONVIF support flag |
| onvif_host | TEXT | NULLABLE | ONVIF host address |
| onvif_username | TEXT | NULLABLE | ONVIF credentials |
| onvif_password | TEXT | NULLABLE | ONVIF credentials |
| status | TEXT | DEFAULT 'unknown' | online/offline/unknown |
| enabled | INTEGER | DEFAULT 1 | Camera enabled flag |
| description | TEXT | NULLABLE | Notes |
| created_at | TEXT | NOT NULL | Creation timestamp |
| updated_at | TEXT | NOT NULL | Last update timestamp |

### 4.3 Events Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| event_id | TEXT | PRIMARY KEY | UUID for the event |
| schema_version | TEXT | DEFAULT '1.0' | Event schema version |
| tenant_id | TEXT | DEFAULT 'default' | Multi-tenant support |
| site_id | TEXT | NULLABLE | Site identifier |
| device_id | TEXT | NULLABLE | Source device |
| camera_id | INTEGER | NOT NULL | Source camera channel |
| event_type | TEXT | NOT NULL | Event classification |
| severity | TEXT | DEFAULT 'LOW' | LOW/MEDIUM/HIGH/CRITICAL |
| timestamp | TEXT | NOT NULL | Event timestamp (ISO 8601) |
| ts_ms | INTEGER | NOT NULL | Millisecond timestamp |
| track_id | INTEGER | NOT NULL | Object tracking ID |
| bbox | TEXT | NULLABLE | Bounding box JSON [x,y,w,h] |
| confidence | REAL | NOT NULL | Detection confidence 0-1 |
| roi_id | INTEGER | NOT NULL | Region of interest ID |
| snapshot_url | TEXT | NULLABLE | Event snapshot path |
| clip_url | TEXT | NULLABLE | Event video clip path |
| ack_status | TEXT | DEFAULT 'unconfirmed' | unconfirmed/confirmed |
| ack_user | TEXT | NULLABLE | User who acknowledged |
| ack_time | TEXT | NULLABLE | Acknowledgment timestamp |
| action_memo | TEXT | NULLABLE | Operator notes |
| sync_status | TEXT | DEFAULT 'pending' | Cloud sync status |
| raw_payload | TEXT | NULLABLE | Original MQTT payload |

### 4.4 Indexes

- `Events.device_id` - Fast lookup by device
- `Events.timestamp` - Time-range queries
- `Events.event_type` - Filter by type
- `Events.severity` - Filter by severity
- `Events.ack_status` - Unconfirmed event queries
- `Cameras.device_id` - Cameras per device lookup
- `Cameras.status` - Status queries
- `Devices.status` - Online/offline queries

## 5. API Design

All endpoints use JSON request/response bodies and follow REST conventions.

### Base URL: `http://localhost:8088`

### Endpoint Summary

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/devices | List all devices |
| GET | /api/devices/{id} | Get device detail |
| POST | /api/devices | Create device |
| PUT | /api/devices/{id} | Update device |
| DELETE | /api/devices/{id} | Delete device |
| GET | /api/cameras | List all cameras |
| GET | /api/cameras/{id} | Get camera detail |
| POST | /api/cameras | Create camera |
| PUT | /api/cameras/{id} | Update camera |
| DELETE | /api/cameras/{id} | Delete camera |
| GET | /api/events | List events (filtered, paginated) |
| GET | /api/events/{id} | Get event detail |
| PUT | /api/events/{id}/ack | Acknowledge event |
| PUT | /api/events/{id}/memo | Add action memo |
| GET | /api/events/export/csv | Export events as CSV |
| GET | /api/dashboard/summary | Dashboard statistics |

## 6. Deployment Model

The application is deployed as a single self-contained Windows executable:

```
dist/
├── ControlCenter.Gateway.exe    # Self-contained .NET 8 executable
├── wwwroot/                     # Pre-built React frontend
├── appsettings.json             # Configuration (editable post-deploy)
└── controlcenter.db             # SQLite database (created on first run)
```

No external dependencies are required on the target machine. The self-contained publish includes the .NET runtime.
