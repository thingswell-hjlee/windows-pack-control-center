# Requirements Document

## Introduction

The Windows Local Control Center is a local web-based monitoring and management system for AIBox devices and network cameras. The system runs as a self-contained application on a Windows PC, providing an integrated dashboard accessible via web browser at http://localhost:8088. The architecture consists of a Local Gateway (backend), a Local Web Dashboard (frontend), SQLite database for persistence, and an MQTT Receiver for real-time event ingestion from AIBox edge devices. This system operates entirely on the local network without cloud dependencies, with provisions for future AWS cloud expansion.

## Glossary

- **Local_Gateway**: The .NET 8 ASP.NET Core backend application that receives MQTT messages, normalizes events, stores data in SQLite, and provides REST API and SignalR endpoints
- **Web_Dashboard**: The React/TypeScript/Vite single-page application served by the Local Gateway that provides the operator user interface
- **AIBox**: An edge computing device that runs AI inference models, connects to network cameras, detects events, and publishes results via MQTT
- **MQTT_Receiver**: The background service component within the Local Gateway that connects to MQTT brokers and receives messages from AIBox devices
- **Event_Normalizer**: The background service component that transforms raw MQTT payloads into the standard Event Schema
- **Device_Status_Service**: The background service that monitors AIBox heartbeat timestamps and determines online/offline status
- **SignalR_Hub**: The WebSocket-based real-time messaging component that pushes events and status changes to connected Web Dashboard clients
- **REST_API**: The HTTP API layer within the Local Gateway that provides CRUD operations and data queries
- **Standard_Event_Schema**: The normalized event data structure used for consistent storage and display of events from varying AIBox payload formats
- **Operator**: A human user who monitors the Web Dashboard, acknowledges events, and records action memos
- **Camera_Status_Checker**: The component that determines network camera online/offline status using multiple methods (AIBox status payload, RTSP test, Ping test, ONVIF test)
- **Sync_Status**: A field stored on each event indicating readiness for future cloud synchronization (pending, queued, synced, failed)

## Requirements

### Requirement 1: Local Gateway Startup and Accessibility

**User Story:** As an operator, I want the Local Gateway to start and serve the Web Dashboard on a known port, so that I can access the monitoring system from a browser.

#### Acceptance Criteria

1. WHEN the Local Gateway process starts, THE Local_Gateway SHALL listen for HTTP connections on port 8088
2. WHEN a browser navigates to http://localhost:8088, THE Local_Gateway SHALL serve the Web Dashboard static files
3. WHEN the Local Gateway starts, THE Local_Gateway SHALL create the SQLite database file if the database does not already exist
4. WHEN the Local Gateway starts, THE Local_Gateway SHALL apply all pending database schema migrations automatically
5. THE Local_Gateway SHALL start and become ready to accept HTTP requests within 5 seconds on standard hardware
6. IF the configured port 8088 is already in use, THEN THE Local_Gateway SHALL log an error message indicating the port conflict and terminate gracefully

### Requirement 2: AIBox Device Registration

**User Story:** As an operator, I want to register AIBox devices with their connection details, so that the system can connect to them and receive events.

#### Acceptance Criteria

1. WHEN an operator submits a device registration form, THE REST_API SHALL create a new AIBox device record with the following fields: device_id, device_name, site_id, site_name, location, ip_address, mqtt_host, mqtt_port, mqtt_username, mqtt_password, event_topic, status_topic, enabled, and description
2. THE REST_API SHALL require device_id and device_name fields for device creation
3. THE REST_API SHALL set mqtt_port to 1883 when no port value is provided
4. THE REST_API SHALL record created_at and updated_at timestamps on device creation
5. IF a device registration is submitted with a device_id that already exists, THEN THE REST_API SHALL return a conflict error response with a descriptive message
6. WHEN a device record is created with enabled set to true, THE MQTT_Receiver SHALL initiate a connection to the device MQTT broker within 5 seconds

### Requirement 3: AIBox Device Configuration Management

**User Story:** As an operator, I want to view, edit, and delete AIBox device configurations, so that I can maintain accurate device information.

#### Acceptance Criteria

1. WHEN an operator requests the device list, THE REST_API SHALL return all registered AIBox devices with their current status
2. WHEN an operator requests a specific device by device_id, THE REST_API SHALL return the full device record including all registration fields and current status
3. WHEN an operator submits updated device fields, THE REST_API SHALL update the device record and set the updated_at timestamp
4. WHEN an operator updates MQTT connection fields on an enabled device, THE MQTT_Receiver SHALL disconnect from the previous broker and reconnect using the new settings
5. WHEN an operator deletes a device, THE REST_API SHALL remove the device record and all associated camera records from the database
6. WHEN a device is deleted, THE MQTT_Receiver SHALL disconnect from that device MQTT broker and unsubscribe from all topics

### Requirement 4: AIBox Status Monitoring

**User Story:** As an operator, I want to see the real-time online/offline status of each AIBox device, so that I can identify connectivity issues immediately.

#### Acceptance Criteria

1. THE Device_Status_Service SHALL maintain a status value for each AIBox device using one of: online, offline, warning, unknown, or disabled
2. WHEN the MQTT_Receiver receives a status message from an AIBox device, THE Device_Status_Service SHALL update that device status to online and record the received timestamp
3. WHEN a device has not sent any MQTT message within 60 seconds, THE Device_Status_Service SHALL change that device status to offline
4. WHEN an operator disables a device, THE Device_Status_Service SHALL set that device status to disabled
5. WHEN a device status changes, THE SignalR_Hub SHALL broadcast the status change to all connected Web Dashboard clients within 1 second
6. THE Web_Dashboard SHALL display the following status items for each AIBox device: AIBox ID, AIBox Name, Site, Location, IP Address, Online Status, Last Received Time, Last Event Time, Connected Cameras count, Today Events count, High-or-above Events count, App Version, Model Version, CPU Usage, GPU Usage, Temperature, and Disk Status

### Requirement 5: Network Camera Registration

**User Story:** As an operator, I want to register network cameras and associate them with AIBox devices, so that events can be attributed to specific camera locations.

#### Acceptance Criteria

1. WHEN an operator submits a camera registration form, THE REST_API SHALL create a new camera record with the following fields: camera_id, camera_name, site_id, site_name, device_id, channel_no, location, ip_address, rtsp_url, onvif_enabled, onvif_host, onvif_username, onvif_password, status, enabled, and description
2. THE REST_API SHALL require camera_id, camera_name, and device_id fields for camera creation
3. THE REST_API SHALL validate that the specified device_id exists before creating the camera record
4. THE REST_API SHALL record created_at and updated_at timestamps on camera creation
5. IF a camera registration is submitted with a camera_id that already exists, THEN THE REST_API SHALL return a conflict error response with a descriptive message

### Requirement 6: Network Camera Configuration Management

**User Story:** As an operator, I want to view, edit, and delete camera configurations, so that I can maintain accurate camera information.

#### Acceptance Criteria

1. WHEN an operator requests the camera list, THE REST_API SHALL return all registered cameras with their current status and associated device information
2. WHEN an operator requests cameras filtered by device_id, THE REST_API SHALL return only cameras associated with the specified device
3. WHEN an operator submits updated camera fields, THE REST_API SHALL update the camera record and set the updated_at timestamp
4. WHEN an operator deletes a camera, THE REST_API SHALL remove the camera record from the database

### Requirement 7: Network Camera Status Monitoring

**User Story:** As an operator, I want to see the online/offline status of each network camera, so that I can detect camera connectivity issues.

#### Acceptance Criteria

1. THE Camera_Status_Checker SHALL maintain a status value for each camera using one of: online, offline, warning, unknown, or disabled
2. WHEN the MQTT_Receiver receives an AIBox status payload containing camera_status information, THE Camera_Status_Checker SHALL update the corresponding camera status accordingly
3. WHERE RTSP URL is configured for a camera, THE Camera_Status_Checker SHALL verify camera connectivity by testing the RTSP connection
4. WHERE ONVIF is enabled for a camera, THE Camera_Status_Checker SHALL verify camera connectivity by testing the ONVIF connection
5. WHEN a camera status changes, THE SignalR_Hub SHALL broadcast the camera status change to all connected Web Dashboard clients
6. WHEN an operator disables a camera, THE Camera_Status_Checker SHALL set that camera status to disabled

### Requirement 8: MQTT Broker Connection Management

**User Story:** As an operator, I want the system to maintain reliable MQTT connections to AIBox devices, so that events are received without manual intervention.

#### Acceptance Criteria

1. WHEN an enabled device is registered, THE MQTT_Receiver SHALL connect to the device MQTT broker using the configured mqtt_host, mqtt_port, mqtt_username, and mqtt_password
2. THE MQTT_Receiver SHALL subscribe to the event topic pattern aibox/+/event/# for each connected device
3. THE MQTT_Receiver SHALL subscribe to the status topic pattern aibox/+/status/# for each connected device
4. IF the MQTT connection to a device broker is lost, THEN THE MQTT_Receiver SHALL attempt reconnection automatically using a configurable reconnect interval
5. THE MQTT_Receiver SHALL support configurable MQTT settings: Host, Port, Username, Password, Client ID, Auto Connect flag, Topic Prefix, Keep Alive interval, and Reconnect Interval
6. IF the MQTT_Receiver receives a message on an unknown or unexpected topic, THEN THE MQTT_Receiver SHALL log the topic and payload without crashing or raising an unhandled exception
7. THE MQTT_Receiver SHALL support the future topic pattern thingswell/{tenant_id}/{site_id}/{device_id}/event/{event_type} without modification to core subscription logic

### Requirement 9: Event Ingestion and Normalization

**User Story:** As an operator, I want all AIBox events normalized into a consistent format, so that I can view and filter events uniformly regardless of the source device.

#### Acceptance Criteria

1. WHEN the MQTT_Receiver receives an event message, THE Event_Normalizer SHALL transform the raw payload into the Standard Event Schema containing: schema_version, tenant_id, site_id, site_name, device_id, device_name, camera_id, camera_name, event_id, event_type, severity, timestamp, ts_ms, track_id, bbox, confidence, roi_id, roi_name, snapshot_url, clip_url, model_version, edge_app_version, ack_status, ack_user, ack_time, action_memo, sync_status, sync_retry_count, last_sync_time, cloud_event_id, sync_error_message, and raw_payload
2. THE Event_Normalizer SHALL generate event_id using the pattern: {device_id}-{ts_ms}-{event_type}-{camera_id}-{track_id} when track_id is present
3. WHEN track_id is not present in the payload, THE Event_Normalizer SHALL generate event_id using the pattern: {device_id}-{ts_ms}-{event_type}-{camera_id}
4. WHEN the raw payload contains bbox as an array [x, y, w, h], THE Event_Normalizer SHALL convert the bbox to an object with properties x, y, w, and h
5. THE Event_Normalizer SHALL store the original raw_payload alongside the normalized event
6. THE Event_Normalizer SHALL record a received_at timestamp indicating when the gateway received the MQTT message
7. IF a normalized event has an event_id that already exists in the database, THEN THE Event_Normalizer SHALL discard the duplicate event without storing it

### Requirement 10: Event Type and Severity Classification

**User Story:** As an operator, I want events classified by type and severity, so that I can prioritize critical events for immediate response.

#### Acceptance Criteria

1. THE Event_Normalizer SHALL support the following event types with assigned severity: FALL_DETECTED (HIGH), ROI_INTRUSION (HIGH), WORK_ZONE_ENTRY (MEDIUM), HAZARD_PROXIMITY (HIGH), WORK_NO_HELMET (MEDIUM), WORK_NO_VEST (MEDIUM), WORK_NO_MASK (MEDIUM), DEVICE_OFFLINE (HIGH), CAMERA_OFFLINE (MEDIUM), SYSTEM_WARNING (MEDIUM), and UNKNOWN_EVENT (LOW)
2. WHEN an event message contains an event_type not in the supported list, THE Event_Normalizer SHALL classify the event as UNKNOWN_EVENT with LOW severity
3. THE Event_Normalizer SHALL set ack_status to unconfirmed for all newly ingested events
4. THE Event_Normalizer SHALL set sync_status to pending for all newly ingested events

### Requirement 11: Event Persistence

**User Story:** As an operator, I want all events stored persistently, so that I can review historical events and generate reports.

#### Acceptance Criteria

1. WHEN the Event_Normalizer produces a normalized event, THE Local_Gateway SHALL persist the event in the SQLite database
2. THE Local_Gateway SHALL maintain database indexes on device_id, timestamp, event_type, severity, and ack_status columns for efficient query performance
3. THE Local_Gateway SHALL store events with full Standard Event Schema fields to support future AWS cloud synchronization

### Requirement 12: Real-Time Event Display

**User Story:** As an operator, I want to see new events appear on the dashboard immediately as they occur, so that I can respond to incidents in real time.

#### Acceptance Criteria

1. WHEN a new event is persisted to the database, THE SignalR_Hub SHALL broadcast the event to all connected Web Dashboard clients
2. WHEN the Web Dashboard receives a new event via SignalR, THE Web_Dashboard SHALL display the event in the event feed without requiring a page refresh
3. THE Local_Gateway SHALL deliver real-time event notifications to connected clients with latency of less than 1 second from MQTT receipt to dashboard display
4. THE Web_Dashboard SHALL display events with severity-based visual indicators distinguishing HIGH, MEDIUM, and LOW severity levels

### Requirement 13: Event Filtering and Listing

**User Story:** As an operator, I want to filter and browse events by various criteria, so that I can find specific events quickly.

#### Acceptance Criteria

1. WHEN an operator requests the event list, THE REST_API SHALL return events in reverse chronological order with pagination support
2. THE REST_API SHALL support filtering events by: device_id, camera_id, event_type, severity level, ack_status, and date range (start and end timestamps)
3. WHEN an operator applies multiple filters simultaneously, THE REST_API SHALL return events matching all specified filter criteria
4. THE REST_API SHALL return paginated results with configurable page size and page number parameters

### Requirement 14: Event Detail View

**User Story:** As an operator, I want to view full details of a specific event, so that I can assess the situation and determine the appropriate response.

#### Acceptance Criteria

1. WHEN an operator selects an event from the list, THE Web_Dashboard SHALL display the complete event details including: event_type, severity, timestamp, device_name, camera_name, confidence, bbox coordinates, snapshot_url, clip_url, ack_status, ack_user, ack_time, action_memo, and raw_payload
2. WHERE a snapshot_url is present in the event, THE Web_Dashboard SHALL display the event snapshot image

### Requirement 15: Event Acknowledgment

**User Story:** As an operator, I want to acknowledge events, so that the team knows which events have been reviewed.

#### Acceptance Criteria

1. WHEN an operator submits an acknowledgment for an event, THE REST_API SHALL update the event ack_status to confirmed
2. WHEN an event is acknowledged, THE REST_API SHALL record the ack_user identifier and ack_time timestamp
3. WHEN an event acknowledgment is saved, THE SignalR_Hub SHALL broadcast the updated ack_status to all connected Web Dashboard clients
4. IF an operator attempts to acknowledge an event that is already confirmed, THEN THE REST_API SHALL return the current event state without modification

### Requirement 16: Action Memo Recording

**User Story:** As an operator, I want to attach action memos to events, so that response actions are documented for review and audit.

#### Acceptance Criteria

1. WHEN an operator submits an action memo for an event, THE REST_API SHALL store the memo text in the action_memo field of the event record
2. THE REST_API SHALL allow updating the action_memo field on events regardless of the current ack_status
3. WHEN an action memo is saved, THE SignalR_Hub SHALL broadcast the updated event to all connected Web Dashboard clients

### Requirement 17: Event Export

**User Story:** As an operator, I want to export events as CSV or Excel files, so that I can generate reports and share data with management.

#### Acceptance Criteria

1. WHEN an operator requests a CSV export, THE REST_API SHALL generate a CSV file containing all events matching the current filter criteria
2. WHEN an operator requests an Excel export, THE REST_API SHALL generate an Excel file containing all events matching the current filter criteria
3. THE REST_API SHALL include the following columns in the export: event_id, event_type, severity, timestamp, device_id, device_name, camera_id, camera_name, confidence, ack_status, ack_user, ack_time, and action_memo
4. THE REST_API SHALL set appropriate Content-Disposition headers for file download on export responses

### Requirement 18: Dashboard Summary Statistics

**User Story:** As an operator, I want a summary dashboard showing key metrics at a glance, so that I can quickly assess the overall system health.

#### Acceptance Criteria

1. WHEN an operator opens the Web Dashboard, THE Web_Dashboard SHALL display summary statistics including: total registered devices, online device count, offline device count, total cameras, online camera count, total events today, unconfirmed events count, and high-severity events today count
2. WHEN a new event is received or a device status changes, THE Web_Dashboard SHALL update the summary statistics in real time via SignalR without page refresh
3. THE REST_API SHALL provide a dashboard summary endpoint that returns all statistics in a single response

### Requirement 19: Logging and Diagnostics

**User Story:** As a system administrator, I want comprehensive logging, so that I can diagnose issues and monitor system health.

#### Acceptance Criteria

1. THE Local_Gateway SHALL log all operational events using Serilog with output to both console and rolling daily log files
2. THE Local_Gateway SHALL log MQTT connection events including: connect, disconnect, reconnect attempts, and subscription confirmations
3. THE Local_Gateway SHALL log event processing activities including: event received count, normalization successes, normalization failures, and duplicate event rejections
4. IF an unhandled exception occurs in any background service, THEN THE Local_Gateway SHALL log the exception details and continue operation without process termination

### Requirement 20: Future Cloud Synchronization Readiness

**User Story:** As a system architect, I want the local system to store synchronization metadata, so that future AWS cloud integration can be implemented without schema changes.

#### Acceptance Criteria

1. THE Local_Gateway SHALL store sync_status on each event record with values: pending, queued, synced, or failed
2. THE Local_Gateway SHALL store sync_retry_count, last_sync_time, cloud_event_id, and sync_error_message fields on each event record
3. THE Local_Gateway SHALL use the Standard Event Schema that is compatible with the future AWS Central Control System common event format
4. THE Local_Gateway SHALL use the Device and Camera registration schemas that are compatible with the future AWS Central Control System common device schema

### Requirement 21: Platform and Deployment

**User Story:** As an IT administrator, I want a simple deployment process, so that I can install and run the system without complex configuration.

#### Acceptance Criteria

1. THE Local_Gateway SHALL run on Windows 10 or later (x64 architecture)
2. THE Local_Gateway SHALL be deployable as a self-contained executable that does not require a separate .NET runtime installation on the target machine
3. THE Local_Gateway SHALL use SQLite for data storage requiring zero database server configuration
4. THE Local_Gateway SHALL support at least 10 concurrent MQTT broker connections
5. THE Local_Gateway SHALL handle a throughput of at least 1000 events per minute without message loss
6. THE Web_Dashboard SHALL function correctly on Chrome, Edge, and Firefox browsers
7. THE Local_Gateway SHALL listen on port 8088 by default with the port configurable via application settings
