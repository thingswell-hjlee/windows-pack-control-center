using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlCenter.Gateway.Models;

[Table("Events")]
public class NormalizedEvent
{
    [Key]
    [Column("event_id")]
    public string EventId { get; set; } = string.Empty;

    [Column("schema_version")]
    public string SchemaVersion { get; set; } = "1.0";

    [Column("tenant_id")]
    public string TenantId { get; set; } = "default";

    [Column("site_id")]
    public string? SiteId { get; set; }

    [Column("site_name")]
    public string? SiteName { get; set; }

    [Column("device_id")]
    public string? DeviceId { get; set; }

    [Column("device_name")]
    public string? DeviceName { get; set; }

    [Column("camera_id")]
    public string CameraId { get; set; } = string.Empty;

    [Column("camera_name")]
    public string? CameraName { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("severity")]
    public string Severity { get; set; } = "LOW";

    [Column("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [Column("ts_ms")]
    public long TsMs { get; set; }

    [Column("track_id")]
    public int TrackId { get; set; }

    [Column("bbox")]
    public string? Bbox { get; set; }

    [Column("confidence")]
    public double Confidence { get; set; }

    [Column("roi_id")]
    public int RoiId { get; set; }

    [Column("roi_name")]
    public string? RoiName { get; set; }

    [Column("snapshot_url")]
    public string? SnapshotUrl { get; set; }

    [Column("clip_url")]
    public string? ClipUrl { get; set; }

    [Column("model_version")]
    public string? ModelVersion { get; set; }

    [Column("edge_app_version")]
    public string? EdgeAppVersion { get; set; }

    [Column("ack_status")]
    public string AckStatus { get; set; } = "unconfirmed";

    [Column("ack_user")]
    public string? AckUser { get; set; }

    [Column("ack_time")]
    public DateTime? AckTime { get; set; }

    [Column("action_memo")]
    public string? ActionMemo { get; set; }

    [Column("sync_status")]
    public string SyncStatus { get; set; } = "pending";

    [Column("sync_retry_count")]
    public int SyncRetryCount { get; set; } = 0;

    [Column("last_sync_time")]
    public DateTime? LastSyncTime { get; set; }

    [Column("cloud_event_id")]
    public string? CloudEventId { get; set; }

    [Column("sync_error_message")]
    public string? SyncErrorMessage { get; set; }

    [Column("raw_payload")]
    public string? RawPayload { get; set; }

    [Column("received_at")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
