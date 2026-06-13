using System.Collections.Concurrent;
using System.Text.Json;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlCenter.Gateway.Services;

public class DeviceStatusService : BackgroundService
{
    private readonly ILogger<DeviceStatusService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttReceiverService _mqttReceiver;
    private readonly IHubContext<EventHub> _hubContext;
    private readonly MqttSettings _settings;
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat = new();

    public DeviceStatusService(
        ILogger<DeviceStatusService> logger,
        IServiceProvider serviceProvider,
        MqttReceiverService mqttReceiver,
        IHubContext<EventHub> hubContext,
        IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mqttReceiver = mqttReceiver;
        _hubContext = hubContext;
        _settings = mqttSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Status Service starting...");

        var statusTask = ProcessStatusMessages(stoppingToken);
        var heartbeatTask = CheckHeartbeats(stoppingToken);

        await Task.WhenAll(statusTask, heartbeatTask);
    }

    private async Task ProcessStatusMessages(CancellationToken stoppingToken)
    {
        await foreach (var message in _mqttReceiver.StatusReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessStatusMessage(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing status message from topic {Topic}", message.Topic);
            }
        }
    }

    private async Task ProcessStatusMessage(MqttMessage message, CancellationToken stoppingToken)
    {
        var deviceId = message.DeviceId;

        // Check if device is disabled; if so, ignore the status message
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var device = await db.Devices.FindAsync(new object[] { deviceId }, stoppingToken);
        if (device != null && !device.Enabled)
        {
            _logger.LogDebug("Ignoring status message for disabled device {DeviceId}", deviceId);
            return;
        }

        _lastHeartbeat[deviceId] = DateTime.UtcNow;

        string status = "online";
        try
        {
            var payload = JsonDocument.Parse(message.Payload);
            if (payload.RootElement.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetString() ?? "online";
            }
        }
        catch
        {
            // If payload is not valid JSON, treat as heartbeat (device is online)
        }

        await UpdateDeviceStatus(deviceId, status, stoppingToken);
    }

    private async Task UpdateDeviceStatus(string deviceId, string status, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var device = await db.Devices.FindAsync(new object[] { deviceId }, stoppingToken);
        if (device != null && device.Status != status)
        {
            device.Status = status;
            device.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(stoppingToken);

            await _hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
            {
                deviceId,
                status,
                updatedAt = DateTime.UtcNow
            }, stoppingToken);

            _logger.LogInformation("Device {DeviceId} status changed to {Status}", deviceId, status);
        }
    }

    private async Task CheckHeartbeats(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.HeartbeatCheckIntervalSeconds), stoppingToken);

                var now = DateTime.UtcNow;
                var heartbeatTimeout = TimeSpan.FromSeconds(_settings.HeartbeatTimeoutSeconds);

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var (deviceId, lastSeen) in _lastHeartbeat)
                {
                    // Check if device is still enabled before applying timeout
                    var device = await db.Devices.FindAsync(new object[] { deviceId }, stoppingToken);
                    if (device != null && !device.Enabled)
                    {
                        // Device is disabled; skip heartbeat check and remove from tracking
                        _lastHeartbeat.TryRemove(deviceId, out _);
                        _logger.LogDebug("Skipping heartbeat check for disabled device {DeviceId}", deviceId);
                        continue;
                    }

                    if (now - lastSeen > heartbeatTimeout)
                    {
                        await UpdateDeviceStatus(deviceId, "offline", stoppingToken);
                        _lastHeartbeat.TryRemove(deviceId, out _);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking heartbeats");
            }
        }
    }
}
