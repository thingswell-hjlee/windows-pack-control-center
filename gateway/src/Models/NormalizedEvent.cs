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

    [Column("device_id")]
    public string? DeviceId { get; set; }

    [Column("camera_id")]
    public int CameraId { get; set; }

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

    [Column("snapshot_url")]
    public string? SnapshotUrl { get; set; }

    [Column("clip_url")]
    public string? ClipUrl { get; set; }

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

    [Column("raw_payload")]
    public string? RawPayload { get; set; }
}
