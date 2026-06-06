using System.Collections.Concurrent;
using System.Threading.Channels;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace ControlCenter.Gateway.Services;

public class MqttMessage
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public class MqttReceiverService : BackgroundService
{
    private readonly ILogger<MqttReceiverService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttSettings _settings;
    private readonly Channel<MqttMessage> _eventChannel;
    private readonly Channel<MqttMessage> _statusChannel;
    private readonly ConcurrentDictionary<string, IMqttClient> _clients = new();
    private readonly ConcurrentDictionary<string, int> _reconnectAttempts = new();

    public ChannelReader<MqttMessage> EventReader => _eventChannel.Reader;
    public ChannelReader<MqttMessage> StatusReader => _statusChannel.Reader;

    public MqttReceiverService(
        ILogger<MqttReceiverService> logger,
        IServiceProvider serviceProvider,
        IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = mqttSettings.Value;

        var channelOptions = new BoundedChannelOptions(_settings.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        _eventChannel = Channel.CreateBounded<MqttMessage>(channelOptions);
        _statusChannel = Channel.CreateBounded<MqttMessage>(channelOptions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Receiver Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectToDevices(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MQTT Receiver Service loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ConnectToDevices(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var devices = await db.Devices
            .Where(d => d.Enabled && d.MqttHost != null)
            .ToListAsync(stoppingToken);

        foreach (var device in devices)
        {
            if (_clients.ContainsKey(device.DeviceId))
                continue;

            try
            {
                await ConnectToDevice(device, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to MQTT broker for device {DeviceId}", device.DeviceId);
            }
        }
    }

    private async Task ConnectToDevice(Device device, CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(device.MqttHost))
            return;

        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(device.MqttHost, device.MqttPort > 0 ? device.MqttPort : _settings.DefaultPort)
            .WithClientId($"control-center-{device.DeviceId}")
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(_settings.KeepAliveSeconds))
            .WithCleanSession();

        if (!string.IsNullOrEmpty(device.MqttUsername))
        {
            optionsBuilder.WithCredentials(device.MqttUsername, device.MqttPassword);
        }

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            var message = new MqttMessage
            {
                Topic = topic,
                Payload = payload,
                DeviceId = device.DeviceId
            };

            if (IsEventTopic(topic))
            {
                if (_eventChannel.Reader.Count > _settings.ChannelCapacity * 0.9)
                    _logger.LogWarning("Event channel near capacity ({Count}/{Capacity}) for device {DeviceId}", _eventChannel.Reader.Count, _settings.ChannelCapacity, device.DeviceId);
                await _eventChannel.Writer.WriteAsync(message, stoppingToken);
            }
            else if (IsStatusTopic(topic))
            {
                if (_statusChannel.Reader.Count > _settings.ChannelCapacity * 0.9)
                    _logger.LogWarning("Status channel near capacity ({Count}/{Capacity}) for device {DeviceId}", _statusChannel.Reader.Count, _settings.ChannelCapacity, device.DeviceId);
                await _statusChannel.Writer.WriteAsync(message, stoppingToken);
            }
            else
            {
                _logger.LogWarning("Received message on unknown topic {Topic} from device {DeviceId}. Message discarded", topic, device.DeviceId);
                return;
            }

            _logger.LogDebug("Received MQTT message on topic {Topic} from device {DeviceId}", topic, device.DeviceId);
        };

        client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("MQTT client disconnected from device {DeviceId} at {BrokerHost}:{BrokerPort}. Reason: {Reason}",
                device.DeviceId, device.MqttHost, device.MqttPort, e.Reason);
            _clients.TryRemove(device.DeviceId, out _);

            if (stoppingToken.IsCancellationRequested)
                return;

            var attemptCount = _reconnectAttempts.AddOrUpdate(device.DeviceId, 1, (_, current) => current + 1);

            _logger.LogInformation(
                "Attempting MQTT reconnection to device {DeviceId} at {BrokerHost}:{BrokerPort}. Attempt #{AttemptCount}, delay {ReconnectDelaySeconds}s",
                device.DeviceId, device.MqttHost, device.MqttPort, attemptCount, _settings.ReconnectDelaySeconds);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.ReconnectDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var freshDevice = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == device.DeviceId);

                if (freshDevice is null || !freshDevice.Enabled || string.IsNullOrEmpty(freshDevice.MqttHost))
                {
                    _logger.LogInformation("Device {DeviceId} no longer eligible for MQTT connection, skipping reconnect", device.DeviceId);
                    _reconnectAttempts.TryRemove(device.DeviceId, out _);
                    return;
                }

                await ConnectToDevice(freshDevice, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping, no action needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to device {DeviceId} at {BrokerHost}:{BrokerPort}. Attempt #{AttemptCount}",
                    device.DeviceId, device.MqttHost, device.MqttPort, attemptCount);
            }
        };

        var options = optionsBuilder.Build();
        await client.ConnectAsync(options, stoppingToken);

        _logger.LogInformation("MQTT connection established for device {DeviceId} at {BrokerHost}:{BrokerPort}",
            device.DeviceId, device.MqttHost, device.MqttPort);

        // Support both legacy aibox/{id}/event/# and future thingswell/{tenant}/{site}/{device}/event/{type} patterns
        // via per-device configurable topic fields
        var eventTopic = device.EventTopic ?? $"aibox/{device.DeviceId}/event/#";
        var statusTopic = device.StatusTopic ?? $"aibox/{device.DeviceId}/status/#";

        await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(eventTopic).Build(), stoppingToken);
        _logger.LogInformation("MQTT subscription confirmed for device {DeviceId}: event topic {EventTopic}",
            device.DeviceId, eventTopic);

        await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(statusTopic).Build(), stoppingToken);
        _logger.LogInformation("MQTT subscription confirmed for device {DeviceId}: status topic {StatusTopic}",
            device.DeviceId, statusTopic);

        _clients[device.DeviceId] = client;
        _reconnectAttempts.TryRemove(device.DeviceId, out _);
    }

    /// <summary>
    /// Disconnect and reconnect to a device's MQTT broker (called after MQTT settings update).
    /// </summary>
    public async Task ReconnectDevice(string deviceId)
    {
        // Disconnect existing client if present
        if (_clients.TryRemove(deviceId, out var existingClient))
        {
            try
            {
                await existingClient.DisconnectAsync();
                existingClient.Dispose();
                _logger.LogInformation("Disconnected MQTT client for device {DeviceId} for reconnection", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting MQTT client for device {DeviceId}", deviceId);
            }
        }
        // The next ConnectToDevices poll cycle will pick up the device and reconnect
    }

    /// <summary>
    /// Disconnect from a device's MQTT broker (called when device is disabled).
    /// </summary>
    public async Task DisconnectDevice(string deviceId)
    {
        if (_clients.TryRemove(deviceId, out var client))
        {
            try
            {
                await client.DisconnectAsync();
                client.Dispose();
                _logger.LogInformation("Disconnected MQTT client for disabled device {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting MQTT client for device {DeviceId}", deviceId);
            }
        }
        _reconnectAttempts.TryRemove(deviceId, out _);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MQTT Receiver Service stopping...");

        foreach (var (deviceId, client) in _clients)
        {
            try
            {
                await client.DisconnectAsync(cancellationToken: cancellationToken);
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting MQTT client for device {DeviceId}", deviceId);
            }
        }

        _clients.Clear();
        _eventChannel.Writer.TryComplete();
        _statusChannel.Writer.TryComplete();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Determines if a topic is an event topic.
    /// Supports both legacy pattern (aibox/{id}/event/#) and
    /// future Thingswell pattern (thingswell/{tenant_id}/{site_id}/{device_id}/event/{event_type}).
    /// </summary>
    private static bool IsEventTopic(string topic) => topic.Contains("/event/") || topic.EndsWith("/event");

    /// <summary>
    /// Determines if a topic is a status topic.
    /// Supports both legacy pattern (aibox/{id}/status/#) and
    /// future Thingswell pattern (thingswell/{tenant_id}/{site_id}/{device_id}/status).
    /// </summary>
    private static bool IsStatusTopic(string topic) => topic.Contains("/status/") || topic.EndsWith("/status");
}
