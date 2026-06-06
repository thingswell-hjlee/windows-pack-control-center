# Requirements Specification

## 1. Overview

The Windows Pack Control Center is a local web application that provides a unified dashboard for managing AIBox devices, network cameras, and AI-generated events. It runs as a single self-contained Windows service, accessible via web browser at http://localhost:8088.

## 2. Goals

- Provide a centralized view of all AIBox devices and their status
- Receive and normalize events from AIBox devices via MQTT
- Display real-time events with severity indicators
- Allow operators to acknowledge events and add action memos
- Export event data as CSV reports
- Support multiple AIBox devices and cameras per device
- Run entirely on a local Windows machine without cloud dependencies

## 3. Functional Requirements

### 3.1 Device Management

| ID | Requirement |
|----|-------------|
| FR-001 | System shall allow registering AIBox devices with MQTT connection details |
| FR-002 | System shall display online/offline status for each device |
| FR-003 | System shall support enabling/disabling individual devices |
| FR-004 | System shall allow editing device configuration (name, location, MQTT settings) |
| FR-005 | System shall allow deleting devices |

### 3.2 Camera Management

| ID | Requirement |
|----|-------------|
| FR-010 | System shall allow registering cameras associated with a device |
| FR-011 | System shall store RTSP URL, ONVIF settings, and channel number |
| FR-012 | System shall display camera status (online/offline) |
| FR-013 | System shall allow editing and deleting cameras |

### 3.3 MQTT Communication

| ID | Requirement |
|----|-------------|
| FR-020 | System shall connect to each registered device's MQTT broker |
| FR-021 | System shall subscribe to `aibox/+/event/#` topics for events |
| FR-022 | System shall subscribe to `aibox/+/status/#` topics for status updates |
| FR-023 | System shall reconnect automatically on connection loss |
| FR-024 | System shall detect device offline status via heartbeat timeout (60s default) |

### 3.4 Event Processing

| ID | Requirement |
|----|-------------|
| FR-030 | System shall normalize incoming MQTT payloads into a standard event schema |
| FR-031 | Normalized events shall include: event_type, severity, timestamp, device_id, camera_id, confidence, bbox |
| FR-032 | System shall persist events in SQLite database |
| FR-033 | System shall broadcast new events to connected dashboard clients via SignalR |
| FR-034 | System shall support event types: intrusion, loitering, fire, smoke, fall_detection, line_crossing, object_detection |
| FR-035 | System shall support severity levels: LOW, MEDIUM, HIGH, CRITICAL |

### 3.5 Event Management

| ID | Requirement |
|----|-------------|
| FR-040 | System shall display events in a filterable list |
| FR-041 | System shall support filtering by: device, event type, severity, acknowledgment status, date range |
| FR-042 | System shall allow acknowledging events (mark as confirmed) |
| FR-043 | System shall allow adding action memos to events |
| FR-044 | System shall support paginated event listing |

### 3.6 Dashboard

| ID | Requirement |
|----|-------------|
| FR-050 | System shall display summary statistics: total devices, online devices, total events today, unconfirmed events |
| FR-051 | System shall update statistics in real-time via SignalR |
| FR-052 | System shall provide a responsive web interface accessible from desktop browsers |

### 3.7 Reporting

| ID | Requirement |
|----|-------------|
| FR-060 | System shall support exporting events as CSV |
| FR-061 | Export shall respect current filter criteria |

## 4. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-001 | System shall run on Windows 10 or later |
| NFR-002 | System shall be deployable as a self-contained executable (no .NET runtime required on target) |
| NFR-003 | System shall use SQLite for zero-configuration database |
| NFR-004 | System shall start within 5 seconds |
| NFR-005 | System shall handle at least 10 concurrent MQTT connections |
| NFR-006 | System shall support at least 1000 events per minute throughput |
| NFR-007 | System shall provide real-time updates with less than 1 second latency |
| NFR-008 | System shall log all operations with Serilog (console + rolling file) |
| NFR-009 | Web dashboard shall work on Chrome, Edge, and Firefox |
| NFR-010 | System shall listen on port 8088 by default |

## 5. Constraints

- Single-machine deployment only (not distributed)
- Local network access only (no cloud connectivity required)
- Korean language UI for end users
- Windows x64 target platform
- No authentication required (local trusted network)

## 6. Assumptions

- AIBox devices expose MQTT brokers that the gateway can connect to
- Network cameras are pre-configured and accessible on the local network
- Operators access the dashboard from the same machine or local network
- Events are published in JSON format to MQTT topics following the `aibox/{device_id}/event/{event_type}` pattern
