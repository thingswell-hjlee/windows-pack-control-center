namespace ControlCenter.Gateway.Services;

public class CameraStatusSettings
{
    public const string SectionName = "CameraStatus";

    public int CheckIntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 3;
    public int MaxConcurrentChecks { get; set; } = 5;
}
