using System.Text.Json;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Hubs;
using ControlCenter.Gateway.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Services;

public class EventNormalizerService : BackgroundService
{
    private readonly ILogger<EventNormalizerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttReceiverService _mqttReceiver;
    private readonly IHubContext<EventHub> _hubContext;

    private static readonly Dictionary<string, string> EventSeverityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FALL_DETECTED"] = "HIGH",
        ["ROI_INTRUSION"] = "HIGH",
        ["WORK_ZONE_ENTRY"] = "MEDIUM",
        ["HAZARD_PROXIMITY"] = "HIGH",
        ["WORK_NO_HELMET"] = "MEDIUM",
        ["WORK_NO_VEST"] = "MEDIUM",
        ["WORK_NO_MASK"] = "MEDIUM",
        ["DEVICE_OFFLINE"] = "HIGH",
        ["CAMERA_OFFLINE"] = "MEDIUM",
        ["SYSTEM_WARNING"] = "MEDIUM",
        ["UNKNOWN_EVENT"] = "LOW"
    };

    public EventNormalizerService(
        ILogger<EventNormalizerService> logger,
        IServiceProvider serviceProvider,
        MqttReceiverService mqttReceiver,
        IHubContext<EventHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mqttReceiver = mqttReceiver;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Normalizer Service starting...");

        await foreach (var message in _mqttReceiver.EventReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessEventMessage(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message from topic {Topic}", message.Topic);
            }
        }
    }

    private async Task ProcessEventMessage(MqttMessage message, CancellationToken stoppingToken)
    {
        // Record received_at timestamp immediately upon message receipt
        var receivedAt = DateTime.UtcNow;

        var payload = JsonDocument.Parse(message.Payload);
        var root = payload.RootElement;

        var deviceId = message.DeviceId;
        var eventType = GetJsonString(root, "event_type") ?? "UNKNOWN_EVENT";
        var tsMs = GetJsonLong(root, "ts_ms") ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var cameraId = GetJsonString(root, "camera_id") ?? GetJsonInt(root, "camera_id")?.ToString() ?? string.Empty;
        var trackId = GetJsonInt(root, "track_id") ?? 0;

        var eventId = $"{deviceId}-{tsMs}-{eventType}-{cameraId}-{trackId}";
        var severity = EventSeverityMap.GetValueOrDefault(eventType, "LOW");

        // Extract optional fields from payload
        var siteName = GetJsonString(root, "site_name");
        var roiName = GetJsonString(root, "roi_name");
        var modelVersion = GetJsonString(root, "model_version");
        var edgeAppVersion = GetJsonString(root, "edge_app_version");

        // Create scope for database lookups
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Enrich with device_name and site_name from database
        string? deviceName = null;
        string? dbSiteName = null;
        if (!string.IsNullOrEmpty(deviceId))
        {
            var device = await db.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId, stoppingToken);

            if (device != null)
            {
                deviceName = device.DeviceName;
                dbSiteName = device.SiteName;
            }
        }

        // Enrich with camera_name from database
        string? cameraName = null;
        if (!string.IsNullOrEmpty(cameraId))
        {
            var camera = await db.Cameras
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CameraId == cameraId && c.DeviceId == deviceId, stoppingToken);

            cameraName = camera?.CameraName;
        }

        // Use payload site_name if available, otherwise fallback to device's site_name
        var finalSiteName = siteName ?? dbSiteName;

        var normalizedEvent = new NormalizedEvent
        {
            EventId = eventId,
            SchemaVersion = "1.0",
            TenantId = "default",
            SiteId = GetJsonString(root, "site_id"),
            SiteName = finalSiteName,
            DeviceId = deviceId,
            DeviceName = deviceName,
            CameraId = cameraId,
            CameraName = cameraName,
            EventType = eventType,
            Severity = severity,
            Timestamp = GetJsonString(root, "timestamp") ?? DateTime.UtcNow.ToString("o"),
            TsMs = tsMs,
            TrackId = trackId,
            Bbox = GetJsonString(root, "bbox") ?? GetJsonObject(root, "bbox"),
            Confidence = GetJsonDouble(root, "confidence") ?? 0.0,
            RoiId = GetJsonInt(root, "roi_id") ?? 0,
            RoiName = roiName,
            SnapshotUrl = GetJsonString(root, "snapshot_url"),
            ClipUrl = GetJsonString(root, "clip_url"),
            ModelVersion = modelVersion,
            EdgeAppVersion = edgeAppVersion,
            AckStatus = "unconfirmed",
            SyncStatus = "pending",
            SyncRetryCount = 0,
            LastSyncTime = null,
            CloudEventId = null,
            SyncErrorMessage = null,
            RawPayload = message.Payload,
            ReceivedAt = receivedAt
        };

        var exists = await db.Events.AnyAsync(e => e.EventId == eventId, stoppingToken);
        if (!exists)
        {
            try
            {
                db.Events.Add(normalizedEvent);
                await db.SaveChangesAsync(stoppingToken);

                await _hubContext.Clients.All.SendAsync("NewEvent", normalizedEvent, stoppingToken);
                _logger.LogInformation("Normalized and stored event {EventId} of type {EventType}", eventId, eventType);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true ||
                                                 ex.InnerException?.Message.Contains("duplicate") == true ||
                                                 ex.InnerException?.Message.Contains("already exists") == true)
            {
                _logger.LogDebug("Duplicate event {EventId} detected during insert, skipping", eventId);
            }
        }
    }

    private static string? GetJsonString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }

    private static string? GetJsonObject(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object)
            return value.GetRawText();
        return null;
    }

    private static int? GetJsonInt(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetInt32();
        }
        return null;
    }

    private static long? GetJsonLong(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetInt64();
        }
        return null;
    }

    private static double? GetJsonDouble(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetDouble();
        }
        return null;
    }
}
