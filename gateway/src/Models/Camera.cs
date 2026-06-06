using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlCenter.Gateway.Models;

[Table("Cameras")]
public class Camera
{
    [Key]
    [Column("camera_id")]
    public string CameraId { get; set; } = string.Empty;

    [Column("camera_name")]
    public string CameraName { get; set; } = string.Empty;

    [Column("site_id")]
    public string? SiteId { get; set; }

    [Column("site_name")]
    public string? SiteName { get; set; }

    [Column("device_id")]
    public string? DeviceId { get; set; }

    [Column("channel_no")]
    public int ChannelNo { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("rtsp_url")]
    public string? RtspUrl { get; set; }

    [Column("onvif_enabled")]
    public bool OnvifEnabled { get; set; }

    [Column("onvif_host")]
    public string? OnvifHost { get; set; }

    [Column("onvif_username")]
    public string? OnvifUsername { get; set; }

    [Column("onvif_password")]
    public string? OnvifPassword { get; set; }

    [Column("status")]
    public string Status { get; set; } = "unknown";

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("DeviceId")]
    public Device? Device { get; set; }
}
