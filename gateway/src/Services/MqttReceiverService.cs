using System.Threading.Channels;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;
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
    private readonly Channel<MqttMessage> _eventChannel;
    private readonly Channel<MqttMessage> _statusChannel;
    private readonly Dictionary<string, IMqttClient> _clients = new();

    public ChannelReader<MqttMessage> EventReader => _eventChannel.Reader;
    public ChannelReader<MqttMessage> StatusReader => _statusChannel.Reader;

    public MqttReceiverService(
        ILogger<MqttReceiverService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventChannel = Channel.CreateUnbounded<MqttMessage>();
        _statusChannel = Channel.CreateUnbounded<MqttMessage>();
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
            .WithTcpServer(device.MqttHost, device.MqttPort)
            .WithClientId($"control-center-{device.DeviceId}")
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

            if (topic.Contains("/event/"))
            {
                await _eventChannel.Writer.WriteAsync(message, stoppingToken);
            }
            else if (topic.Contains("/status/"))
            {
                await _statusChannel.Writer.WriteAsync(message, stoppingToken);
            }

            _logger.LogDebug("Received MQTT message on topic {Topic} from device {DeviceId}", topic, device.DeviceId);
        };

        client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("MQTT client disconnected from device {DeviceId}: {Reason}", device.DeviceId, e.Reason);
            _clients.Remove(device.DeviceId);

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                try
                {
                    await ConnectToDevice(device, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect to device {DeviceId}", device.DeviceId);
                }
            }
        };

        var options = optionsBuilder.Build();
        await client.ConnectAsync(options, stoppingToken);

        var eventTopic = device.EventTopic ?? $"aibox/{device.DeviceId}/event/#";
        var statusTopic = device.StatusTopic ?? $"aibox/{device.DeviceId}/status/#";

        await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(eventTopic).Build(), stoppingToken);
        await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(statusTopic).Build(), stoppingToken);

        _clients[device.DeviceId] = client;
        _logger.LogInformation("Connected to MQTT broker for device {DeviceId} at {Host}:{Port}", device.DeviceId, device.MqttHost, device.MqttPort);
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
        _eventChannel.Writer.Complete();
        _statusChannel.Writer.Complete();

        await base.StopAsync(cancellationToken);
    }
}
