using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlCenter.Gateway.Models;

[Table("Devices")]
public class Device
{
    [Key]
    [Column("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [Column("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [Column("site_id")]
    public string? SiteId { get; set; }

    [Column("site_name")]
    public string? SiteName { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("mqtt_host")]
    public string? MqttHost { get; set; }

    [Column("mqtt_port")]
    public int MqttPort { get; set; } = 1883;

    [Column("mqtt_username")]
    public string? MqttUsername { get; set; }

    [Column("mqtt_password")]
    public string? MqttPassword { get; set; }

    [Column("event_topic")]
    public string? EventTopic { get; set; }

    [Column("status_topic")]
    public string? StatusTopic { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public string Status { get; set; } = "unknown";

    public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
}
