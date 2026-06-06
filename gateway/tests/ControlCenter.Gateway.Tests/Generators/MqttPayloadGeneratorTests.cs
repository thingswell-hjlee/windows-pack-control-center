using System.Text.Json;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace ControlCenter.Gateway.Tests.Generators;

/// <summary>
/// Tests to verify that the MqttPayloadGenerator produces valid test data.
/// </summary>
public class MqttPayloadGeneratorTests
{
    [Property]
    public bool GeneratedPayloadsAreValidJson()
    {
        var gen = MqttPayloadGenerator.ValidMqttPayload();
        var payload = gen.Sample(0, 1).First();

        try
        {
            JsonDocument.Parse(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Property]
    public bool PayloadsWithTrackIdHaveNonZeroTrackId()
    {
        var gen = MqttPayloadGenerator.PayloadWithTrackId();
        var payload = gen.Sample(0, 1).First();

        var doc = JsonDocument.Parse(payload);
        var trackId = doc.RootElement.GetProperty("track_id").GetInt32();
        return trackId > 0;
    }

    [Property]
    public bool PayloadsWithoutTrackIdHaveZeroTrackId()
    {
        var gen = MqttPayloadGenerator.PayloadWithoutTrackId();
        var payload = gen.Sample(0, 1).First();

        var doc = JsonDocument.Parse(payload);
        var trackId = doc.RootElement.GetProperty("track_id").GetInt32();
        return trackId == 0;
    }
}
