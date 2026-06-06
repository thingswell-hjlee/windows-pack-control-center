# Implementation Plan: Windows Local Control Center

## Overview

This implementation plan enhances the existing .NET 8 backend (gateway/) and React/TypeScript frontend (web/) to meet the full Windows Local Control Center specification. The existing code provides basic scaffolding with MQTT connectivity, EF Core/SQLite persistence, SignalR, and REST API endpoints. These tasks incrementally add missing schema fields, enhanced services, new features (Camera Status, Export, Property Tests), and frontend improvements to achieve a production-ready local monitoring system.

**Backend:** C# / .NET 8 (ASP.NET Core Minimal API)
**Frontend:** TypeScript / React 18 / Vite
**Testing:** xUnit + FsCheck (property-based) + FluentAssertions

## Tasks

- [ ] 1. Enhance NormalizedEvent model and database schema
  - [ ] 1.1 Add missing fields to the NormalizedEvent model
    - Add `site_name`, `device_name`, `camera_name`, `roi_name`, `model_version`, `edge_app_version`, `sync_retry_count`, `last_sync_time`, `cloud_event_id`, `sync_error_message`, `received_at` properties to `NormalizedEvent.cs`
    - Change `CameraId` from `int` to `string` type to match the Standard Event Schema
    - Set default values: `sync_retry_count = 0`, `received_at = DateTime.UtcNow`
    - _Requirements: 9.1, 20.1, 20.2, 20.3_

  - [ ] 1.2 Add missing database indexes and update DbContext configuration
    - Add index on `ts_ms` column for sorting
    - Add index on `sync_status` for future cloud sync batch queries
    - Ensure `OnDelete(DeleteBehavior.Cascade)` for Device→Camera relationship per Requirement 3.5
    - _Requirements: 11.2, 11.3, 20.1_

  - [ ] 1.3 Update TypeScript types to match the full Standard Event Schema
    - Add `site_name`, `device_name`, `camera_name`, `roi_name`, `model_version`, `edge_app_version`, `sync_retry_count`, `last_sync_time`, `cloud_event_id`, `sync_error_message`, `received_at` fields to `NormalizedEvent` interface in `web/src/types/index.ts`
    - Add `offline_devices`, `high_severity_today` to `DashboardSummary` interface
    - Add `total_pages` to `PaginatedResponse` interface
    - _Requirements: 9.1, 18.1_

- [ ] 2. Enhance MQTT Receiver Service
  - [ ] 2.1 Add configurable MQTT settings from appsettings.json
    - Create `MqttSettings` configuration class with properties: DefaultPort, ReconnectDelaySeconds, HeartbeatTimeoutSeconds, ChannelCapacity, KeepAliveSeconds
    - Bind from `IConfiguration` section "Mqtt" in `Program.cs`
    - Inject `IOptions<MqttSettings>` into MqttReceiverService replacing hardcoded values
    - _Requirements: 8.5, 21.7_

  - [ ] 2.2 Enhance reconnection logic with configurable interval
    - Replace hardcoded 5-second reconnect delay with `MqttSettings.ReconnectDelaySeconds`
    - Add `KeepAlive` setting to MQTT client options builder
    - Log reconnection attempts with structured logging (attempt count, device ID, broker host)
    - _Requirements: 8.4, 19.2_

  - [ ] 2.3 Add graceful handling of unknown topics and logging enhancements
    - Add structured logging for connect, disconnect, reconnect, and subscription confirmations
    - Log unknown/unexpected topics at Warning level without throwing exceptions
    - Support future topic pattern `thingswell/{tenant_id}/{site_id}/{device_id}/event/{event_type}` via configurable topic fields
    - _Requirements: 8.6, 8.7, 19.2_

- [ ] 3. Enhance Event Normalizer Service
  - [ ] 3.1 Implement full event normalization with all schema fields
    - Add `received_at = DateTime.UtcNow` assignment on message receipt
    - Enrich events with `device_name` and `camera_name` from database lookups
    - Extract `site_name`, `roi_name`, `model_version`, `edge_app_version` from payload
    - Set `sync_retry_count = 0`, `last_sync_time = null`, `cloud_event_id = null`, `sync_error_message = null`
    - _Requirements: 9.1, 9.6, 10.3, 10.4_

  - [ ] 3.2 Implement correct event_id generation rules (with and without track_id)
    - When `track_id` is present and non-zero: generate `{device_id}-{ts_ms}-{event_type}-{camera_id}-{track_id}`
    - When `track_id` is absent or zero: generate `{device_id}-{ts_ms}-{event_type}-{camera_id}`
    - _Requirements: 9.2, 9.3_

  - [ ] 3.3 Implement bbox array-to-object conversion
    - Detect when `bbox` is a JSON array `[x, y, w, h]` and convert to JSON object `{"x":x,"y":y,"w":w,"h":h}`
    - Pass through already-object bbox values unchanged
    - Handle null/missing bbox gracefully
    - _Requirements: 9.4_

  - [ ] 3.4 Add structured logging for event processing metrics
    - Log event received count, normalization successes, normalization failures, duplicate rejections
    - Log at Information level for successful processing, Warning for failures
    - _Requirements: 19.3_

  - [ ]* 3.5 Write property test for event ID generation (Property 1)
    - **Property 1: Event ID Determinism**
    - Verify that normalizing the same payload multiple times always produces the same event_id
    - Test both with-track_id and without-track_id paths
    - **Validates: Requirements 9.2, 9.3**

  - [ ]* 3.6 Write property test for event normalization completeness (Property 2)
    - **Property 2: Event Normalization Completeness**
    - For any valid MQTT JSON payload, all required fields (event_id, schema_version, tenant_id, device_id, event_type, severity, timestamp, ts_ms, ack_status, sync_status, received_at) are non-null and non-empty
    - **Validates: Requirements 9.1, 9.6, 10.3, 10.4**

  - [ ]* 3.7 Write property test for severity classification (Property 3)
    - **Property 3: Severity Classification Totality**
    - For any event_type string, the classification always produces exactly one of HIGH, MEDIUM, or LOW
    - Known types map to defined severity; unknown types → UNKNOWN_EVENT with LOW
    - **Validates: Requirements 10.1, 10.2**

  - [ ]* 3.8 Write property test for bbox conversion (Property 4)
    - **Property 4: Bbox Array-to-Object Round Trip**
    - For any 4-element numeric array [x, y, w, h], conversion produces `{"x":x,"y":y,"w":w,"h":h}` and parsing back yields the original values
    - **Validates: Requirements 9.4**

  - [ ]* 3.9 Write property test for raw payload preservation (Property 9)
    - **Property 9: Raw Payload Preservation**
    - For any valid JSON string payload, the stored raw_payload is character-for-character identical to the original MQTT message payload
    - **Validates: Requirements 9.5**

- [ ] 4. Checkpoint - Ensure all core backend services compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Enhance Device Status Service
  - [ ] 5.1 Add configurable heartbeat timeout and check interval
    - Read `HeartbeatTimeoutSeconds` (default 60) and check interval (default 15s) from `MqttSettings`
    - Replace hardcoded `TimeSpan.FromSeconds(60)` and `TimeSpan.FromSeconds(15)` with configuration values
    - _Requirements: 4.1, 4.2, 4.3_

  - [ ] 5.2 Handle device disabled status transitions
    - When a device `Enabled` field is set to false via API update, set its status to "disabled"
    - Skip disabled devices during heartbeat timeout checks
    - _Requirements: 4.4_

  - [ ]* 5.3 Write property test for device heartbeat monotonicity (Property 6)
    - **Property 6: Device Status Heartbeat Monotonicity**
    - A device is "online" iff a status message was received within heartbeat timeout; otherwise "offline"
    - **Validates: Requirements 4.1, 4.2, 4.3**

- [ ] 6. Implement Camera Status Service
  - [ ] 6.1 Create CameraStatusService with waterfall status checking
    - Create `Services/CameraStatusService.cs` as a `BackgroundService`
    - Implement the waterfall check priority: AIBox payload → RTSP TCP probe → ICMP Ping → ONVIF query
    - Use `SemaphoreSlim` (max 5) for concurrent camera checking with `Task.WhenAll`
    - Check interval: 30 seconds, timeout per method: 3 seconds
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 6.2 Add CameraStatusService configuration and registration
    - Create `CameraStatusSettings` class: CheckIntervalSeconds (30), TimeoutSeconds (3), MaxConcurrentChecks (5)
    - Bind from `IConfiguration` section "CameraStatus"
    - Register as hosted service in `Program.cs`
    - Broadcast `CameraStatusChanged` via SignalR when status transitions
    - _Requirements: 7.5, 7.6_

- [ ] 7. Enhance REST API - Devices and Cameras
  - [ ] 7.1 Add validation and conflict detection to Device endpoints
    - Validate `device_id` and `device_name` are required on POST (return 400 if missing)
    - Check for duplicate `device_id` on POST and return 409 Conflict with descriptive message
    - Set `mqtt_port` to 1883 when not provided
    - On DELETE, cascade delete all associated cameras
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 3.5_

  - [ ] 7.2 Add validation and conflict detection to Camera endpoints
    - Validate `camera_id`, `camera_name`, and `device_id` are required on POST (return 400 if missing)
    - Validate that the specified `device_id` exists before creating camera (return 400 if not found)
    - Check for duplicate `camera_id` on POST and return 409 Conflict
    - Add query parameter `?device_id={id}` filter support to GET /api/cameras
    - _Requirements: 5.1, 5.2, 5.3, 5.5, 6.2_

  - [ ] 7.3 Trigger MQTT reconnection on device MQTT field updates
    - When MQTT connection fields (mqtt_host, mqtt_port, mqtt_username, mqtt_password) are updated on an enabled device, disconnect existing MQTT client and reconnect with new settings
    - Add a public method `ReconnectDevice(string deviceId)` to MqttReceiverService
    - On device enable: initiate MQTT connection; on disable: disconnect MQTT client
    - _Requirements: 2.6, 3.4, 3.6_

- [ ] 8. Enhance REST API - Events and Dashboard
  - [ ] 8.1 Add camera_id filter to events endpoint and enhance acknowledgment idempotence
    - Add `camera_id` query parameter filter to GET /api/events
    - On PUT /api/events/{id}/ack: if event is already "confirmed", return current state without modification
    - Broadcast `EventAcknowledged` via SignalR after successful acknowledgment
    - Broadcast `EventMemoUpdated` via SignalR after memo update
    - _Requirements: 13.2, 15.4, 15.3, 16.3_

  - [ ] 8.2 Implement Excel export endpoint
    - Add GET `/api/events/export/excel` endpoint using a lightweight Excel library (e.g., ClosedXML or MiniExcel)
    - Include columns: event_id, event_type, severity, timestamp, device_id, device_name, camera_id, camera_name, confidence, ack_status, ack_user, ack_time, action_memo
    - Set `Content-Disposition: attachment; filename="events_export.xlsx"` header
    - Apply same filter parameters as the CSV export
    - Add NuGet package reference for Excel generation
    - _Requirements: 17.2, 17.3, 17.4_

  - [ ] 8.3 Enhance dashboard summary endpoint with missing statistics
    - Add `offline_devices` count (total_devices - online_devices)
    - Add `high_severity_today` count (events with severity=HIGH and ts_ms >= today start)
    - _Requirements: 18.1, 18.3_

  - [ ]* 8.4 Write property test for event pagination completeness (Property 7)
    - **Property 7: Event Pagination Completeness**
    - For any N events and page_size > 0, iterating all pages returns exactly N events with no duplicates
    - **Validates: Requirements 13.1, 13.4**

  - [ ]* 8.5 Write property test for filter conjunction correctness (Property 8)
    - **Property 8: Filter Conjunction Correctness**
    - For any combination of filters, every event in the result satisfies ALL criteria and no matching event is excluded
    - **Validates: Requirements 13.2, 13.3**

  - [ ]* 8.6 Write property test for acknowledgment idempotence (Property 10)
    - **Property 10: Acknowledgment Idempotence**
    - For an already-confirmed event, re-acknowledgment leaves the event completely unchanged
    - **Validates: Requirements 15.4**

- [ ] 9. Enhance SignalR Hub with all message types
  - [ ] 9.1 Add EventAcknowledged and EventMemoUpdated hub methods
    - Add `SendEventAcknowledged` method broadcasting `{ eventId, ackStatus, ackUser, ackTime }`
    - Add `SendEventMemoUpdated` method broadcasting `{ eventId, actionMemo }`
    - Ensure all hub messages match the contract defined in the design document
    - _Requirements: 15.3, 16.3, 12.1_

- [ ] 10. Checkpoint - Ensure all backend enhancements compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Enhance Web Dashboard - SignalR real-time integration
  - [ ] 11.1 Add EventAcknowledged and EventMemoUpdated handlers to SignalR service
    - Add `onEventAcknowledged` / `offEventAcknowledged` functions in `signalr.ts`
    - Add `onEventMemoUpdated` / `offEventMemoUpdated` functions in `signalr.ts`
    - Extend `useSignalR` hook to accept handlers for `EventAcknowledged` and `EventMemoUpdated`
    - _Requirements: 12.2, 15.3, 16.3_

  - [ ] 11.2 Implement real-time dashboard statistics updates via SignalR
    - On `NewEvent` signal: invalidate dashboard-summary and recent-events queries
    - On `DeviceStatusChanged` signal: invalidate dashboard-summary and devices queries
    - On `CameraStatusChanged` signal: invalidate dashboard-summary queries
    - _Requirements: 18.2_

- [ ] 12. Enhance Web Dashboard - Events page improvements
  - [ ] 12.1 Add comprehensive event filtering UI
    - Add filter controls: device_id dropdown, camera_id dropdown, event_type multi-select, severity multi-select, ack_status dropdown, date range picker (start_date/end_date)
    - Wire filter state to API query parameters
    - Show active filter count badge
    - _Requirements: 13.1, 13.2, 13.3_

  - [ ] 12.2 Enhance event list with severity badges and real-time updates
    - Display severity-based visual indicators (HIGH=red, MEDIUM=orange, LOW=gray) using `SeverityBadge` component
    - Show new events arriving via SignalR at the top of the list without page refresh
    - Add pagination controls with page size selector
    - _Requirements: 12.2, 12.4, 13.4_

  - [ ] 12.3 Implement event acknowledgment and action memo UI
    - Add "Acknowledge" button on event detail view (calls PUT /api/events/{id}/ack)
    - Add action memo text input/textarea with save button (calls PUT /api/events/{id}/memo)
    - Show ack_user and ack_time when event is confirmed
    - Disable acknowledge button if already confirmed
    - _Requirements: 14.1, 15.1, 15.2, 16.1, 16.2_

  - [ ] 12.4 Implement CSV and Excel export buttons
    - Add "Export CSV" button that downloads via `/api/events/export/csv` with current filters
    - Add "Export Excel" button that downloads via `/api/events/export/excel` with current filters
    - _Requirements: 17.1, 17.2_

- [ ] 13. Enhance Web Dashboard - Device and Camera management pages
  - [ ] 13.1 Enhance Devices page with full CRUD forms and status display
    - Add create device modal/form with all registration fields (device_id, device_name, site_id, site_name, location, ip_address, mqtt_host, mqtt_port, mqtt_username, mqtt_password, event_topic, status_topic, enabled, description)
    - Add edit device modal/form
    - Add delete device confirmation dialog
    - Display device list with status indicators: online/offline/disabled badge, last received time, connected cameras count
    - Real-time status updates via SignalR `DeviceStatusChanged`
    - _Requirements: 2.1, 3.1, 3.2, 3.3, 3.5, 4.5, 4.6_

  - [ ] 13.2 Enhance Cameras page with full CRUD forms and status display
    - Add create camera modal/form with all registration fields
    - Add edit camera modal/form
    - Add delete camera confirmation dialog
    - Display camera list with status indicators and associated device name
    - Filter cameras by device
    - Real-time status updates via SignalR `CameraStatusChanged`
    - _Requirements: 5.1, 6.1, 6.2, 6.3, 6.4, 7.5_

- [ ] 14. Enhance Web Dashboard - Dashboard statistics and event detail
  - [ ] 14.1 Add high-severity events count and offline device count to dashboard
    - Display `offline_devices` and `high_severity_today` statistics from enhanced dashboard API
    - Update `DashboardSummary` component with the additional stat cards
    - _Requirements: 18.1_

  - [ ] 14.2 Enhance EventDetail page with complete event information
    - Display all fields: event_type, severity, timestamp, device_name, camera_name, confidence, bbox coordinates, snapshot_url (rendered as image), clip_url, ack_status, ack_user, ack_time, action_memo, raw_payload (collapsible JSON viewer)
    - _Requirements: 14.1, 14.2_

- [ ] 15. Checkpoint - Ensure frontend builds successfully and all features work
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Logging and configuration enhancements
  - [ ] 16.1 Enhance Serilog configuration with structured logging
    - Add structured logging properties for MQTT events (deviceId, topic, eventType)
    - Configure minimum log levels per namespace in `appsettings.json` (e.g., MQTTnet at Warning)
    - Ensure unhandled exceptions in background services are logged but do not terminate the process
    - _Requirements: 19.1, 19.2, 19.3, 19.4_

  - [ ] 16.2 Add port conflict detection and graceful termination
    - On startup, detect if port 8088 is already in use
    - Log error message indicating the port conflict
    - Terminate gracefully (exit code 1) instead of throwing unhandled exception
    - Make port configurable via appsettings.json Kestrel configuration
    - _Requirements: 1.1, 1.5, 1.6, 21.7_

- [ ] 17. Build and deployment configuration
  - [ ] 17.1 Configure self-contained single-file publish
    - Add publish profile or build script for: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
    - Ensure `build.ps1` and `build.sh` scripts build frontend first (`npm run build` in web/), copy to `gateway/src/wwwroot/`, then publish backend
    - Verify startup time target (< 5 seconds)
    - _Requirements: 21.1, 21.2, 21.5_

- [ ] 18. Set up test project and write property-based test infrastructure
  - [ ] 18.1 Create xUnit test project with FsCheck integration
    - Create `gateway/tests/ControlCenter.Gateway.Tests/ControlCenter.Gateway.Tests.csproj` with xUnit, FsCheck, FsCheck.Xunit, FluentAssertions packages
    - Add project reference to gateway/src
    - Create test helper classes for generating arbitrary MQTT payloads (FsCheck generators)
    - _Requirements: Design Testing Strategy_

  - [ ]* 18.2 Write property test for event deduplication idempotence (Property 5)
    - **Property 5: Event Deduplication Idempotence**
    - Processing the same MQTT message N times results in exactly one stored record
    - Uses in-memory SQLite for integration-level property test
    - **Validates: Requirements 9.7**

- [ ] 19. Final checkpoint - Ensure full build, all tests pass, and application runs correctly
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The existing code provides a solid foundation; tasks focus on enhancements to meet the full specification
- Backend uses C# (.NET 8), frontend uses TypeScript/React — no language selection needed
- FsCheck is the recommended property-based testing framework for .NET

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "2.1"] },
    { "id": 1, "tasks": ["2.2", "2.3", "3.1", "5.1"] },
    { "id": 2, "tasks": ["3.2", "3.3", "3.4", "5.2", "6.1"] },
    { "id": 3, "tasks": ["3.5", "3.6", "3.7", "3.8", "3.9", "5.3", "6.2"] },
    { "id": 4, "tasks": ["7.1", "7.2", "7.3", "9.1"] },
    { "id": 5, "tasks": ["8.1", "8.2", "8.3"] },
    { "id": 6, "tasks": ["8.4", "8.5", "8.6", "11.1", "11.2"] },
    { "id": 7, "tasks": ["12.1", "12.2", "13.1", "13.2"] },
    { "id": 8, "tasks": ["12.3", "12.4", "14.1", "14.2"] },
    { "id": 9, "tasks": ["16.1", "16.2", "17.1"] },
    { "id": 10, "tasks": ["18.1"] },
    { "id": 11, "tasks": ["18.2"] }
  ]
}
```
