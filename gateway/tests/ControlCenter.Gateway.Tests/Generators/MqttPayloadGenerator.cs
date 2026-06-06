using FsCheck;

namespace ControlCenter.Gateway.Tests.Generators;

/// <summary>
/// FsCheck generators for creating arbitrary MQTT event payloads for property-based testing.
/// These generators produce realistic MQTT messages that exercise the EventNormalizerService.
/// </summary>
public static class MqttPayloadGenerator
{
    /// <summary>
    /// Known event types that map to specific severity levels in EventNormalizerService.
    /// </summary>
    private static readonly string[] KnownEventTypes = new[]
    {
        "FALL_DETECTED", "ROI_INTRUSION", "WORK_ZONE_ENTRY", "HAZARD_PROXIMITY",
        "WORK_NO_HELMET", "WORK_NO_VEST", "WORK_NO_MASK", "DEVICE_OFFLINE",
        "CAMERA_OFFLINE", "SYSTEM_WARNING"
    };

    /// <summary>
    /// All event types including unknown for testing severity fallback to LOW.
    /// </summary>
    private static readonly string[] AllEventTypes = new[]
    {
        "FALL_DETECTED", "ROI_INTRUSION", "WORK_ZONE_ENTRY", "HAZARD_PROXIMITY",
        "WORK_NO_HELMET", "WORK_NO_VEST", "WORK_NO_MASK", "DEVICE_OFFLINE",
        "CAMERA_OFFLINE", "SYSTEM_WARNING", "UNKNOWN_EVENT"
    };

    /// <summary>
    /// Sample device IDs for generating realistic payloads.
    /// </summary>
    private static readonly string[] DeviceIds = new[]
    {
        "aibox-001", "aibox-002", "aibox-003", "aibox-004", "aibox-005"
    };

    /// <summary>
    /// Generates a known event type (maps to a defined severity).
    /// </summary>
    public static Arbitrary<string> KnownEventType() =>
        Gen.Elements(KnownEventTypes).ToArbitrary();

    /// <summary>
    /// Generates any event type including unknown types for testing fallback logic.
    /// </summary>
    public static Arbitrary<string> AnyEventType() =>
        Gen.OneOf(
            Gen.Elements(AllEventTypes),
            Gen.Elements("CUSTOM_EVENT", "SENSOR_ALERT", "MOTION_DETECTED", "LINE_CROSSING")
        ).ToArbitrary();

    /// <summary>
    /// Generates a valid device ID string.
    /// </summary>
    public static Gen<string> ValidDeviceId() =>
        Gen.Elements(DeviceIds);

    /// <summary>
    /// Generates a valid camera ID (string representation of channel number).
    /// </summary>
    public static Gen<string> ValidCameraId() =>
        Gen.Choose(1, 16).Select(x => x.ToString());

    /// <summary>
    /// Generates a valid Unix timestamp in milliseconds (reasonable range: 2023-2030).
    /// </summary>
    public static Gen<long> ValidTimestampMs() =>
        Gen.Choose(1672531200, 1893456000).Select(x => (long)x * 1000);

    /// <summary>
    /// Generates a valid confidence value between 0.0 and 1.0.
    /// </summary>
    public static Gen<double> ValidConfidence() =>
        Gen.Choose(0, 100).Select(x => x / 100.0);

    /// <summary>
    /// Generates a valid track ID (0 means absent, >0 means present).
    /// </summary>
    public static Gen<int> ValidTrackId() =>
        Gen.Choose(0, 500);

    /// <summary>
    /// Generates a non-zero track ID for testing event_id with track_id path.
    /// </summary>
    public static Gen<int> NonZeroTrackId() =>
        Gen.Choose(1, 500);

    /// <summary>
    /// Generates a 4-element bounding box array [x, y, w, h] with values between 0.0 and 1.0.
    /// </summary>
    public static Gen<double[]> ValidBbox() =>
        from x in Gen.Choose(0, 100).Select(v => v / 100.0)
        from y in Gen.Choose(0, 100).Select(v => v / 100.0)
        from w in Gen.Choose(1, 50).Select(v => v / 100.0)
        from h in Gen.Choose(1, 50).Select(v => v / 100.0)
        select new[] { x, y, w, h };

    /// <summary>
    /// Generates a complete valid MQTT JSON payload string with all required fields.
    /// </summary>
    public static Gen<string> ValidMqttPayload() =>
        from deviceId in ValidDeviceId()
        from tsMs in ValidTimestampMs()
        from eventType in Gen.Elements(AllEventTypes)
        from cameraId in ValidCameraId()
        from trackId in ValidTrackId()
        from confidence in ValidConfidence()
        from bbox in ValidBbox()
        from roiId in Gen.Choose(0, 10)
        select FormatPayload(deviceId, tsMs, eventType, cameraId, trackId, confidence, bbox, roiId);

    /// <summary>
    /// Generates a valid MQTT payload with a guaranteed non-zero track_id.
    /// </summary>
    public static Gen<string> PayloadWithTrackId() =>
        from deviceId in ValidDeviceId()
        from tsMs in ValidTimestampMs()
        from eventType in Gen.Elements(AllEventTypes)
        from cameraId in ValidCameraId()
        from trackId in NonZeroTrackId()
        from confidence in ValidConfidence()
        from bbox in ValidBbox()
        from roiId in Gen.Choose(0, 10)
        select FormatPayload(deviceId, tsMs, eventType, cameraId, trackId, confidence, bbox, roiId);

    /// <summary>
    /// Generates a valid MQTT payload with track_id = 0 (absent).
    /// </summary>
    public static Gen<string> PayloadWithoutTrackId() =>
        from deviceId in ValidDeviceId()
        from tsMs in ValidTimestampMs()
        from eventType in Gen.Elements(AllEventTypes)
        from cameraId in ValidCameraId()
        from confidence in ValidConfidence()
        from bbox in ValidBbox()
        from roiId in Gen.Choose(0, 10)
        select FormatPayload(deviceId, tsMs, eventType, cameraId, 0, confidence, bbox, roiId);

    /// <summary>
    /// Formats a JSON payload from individual field values.
    /// Uses invariant culture formatting for doubles to ensure valid JSON.
    /// </summary>
    private static string FormatPayload(
        string deviceId, long tsMs, string eventType, string cameraId,
        int trackId, double confidence, double[] bbox, int roiId)
    {
        var bboxJson = $"[{bbox[0]:F4}, {bbox[1]:F4}, {bbox[2]:F4}, {bbox[3]:F4}]";
        return $@"{{
  ""device_id"": ""{deviceId}"",
  ""ts_ms"": {tsMs},
  ""event_type"": ""{eventType}"",
  ""camera_id"": ""{cameraId}"",
  ""track_id"": {trackId},
  ""confidence"": {confidence:F4},
  ""bbox"": {bboxJson},
  ""roi_id"": {roiId},
  ""timestamp"": ""{DateTimeOffset.FromUnixTimeMilliseconds(tsMs):o}""
}}";
    }
}
