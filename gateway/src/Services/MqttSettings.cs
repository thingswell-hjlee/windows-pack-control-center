namespace ControlCenter.Gateway.Services;

public class MqttSettings
{
    public const string SectionName = "Mqtt";

    public int DefaultPort { get; set; } = 1883;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int HeartbeatTimeoutSeconds { get; set; } = 60;
    public int ChannelCapacity { get; set; } = 10_000;
    public int KeepAliveSeconds { get; set; } = 30;
}
