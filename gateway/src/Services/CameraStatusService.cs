using System.Net.NetworkInformation;
using System.Net.Sockets;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Hubs;
using ControlCenter.Gateway.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlCenter.Gateway.Services;

/// <summary>
/// Background service that periodically checks camera status using a waterfall approach:
/// 1. AIBox payload (if AIBox status message contains camera info)
/// 2. RTSP TCP probe (if RTSP URL is configured)
/// 3. ICMP Ping (if IP address is known)
/// 4. ONVIF query (if ONVIF is enabled)
/// First successful check wins.
/// </summary>
public class CameraStatusService : BackgroundService
{
    private readonly ILogger<CameraStatusService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<EventHub> _hubContext;
    private readonly CameraStatusSettings _settings;

    // Stores AIBox-reported camera statuses: cameraId -> status
    private readonly Dictionary<string, string> _aiBoxCameraStatuses = new();
    private readonly object _aiBoxLock = new();

    public CameraStatusService(
        ILogger<CameraStatusService> logger,
        IServiceProvider serviceProvider,
        IHubContext<EventHub> hubContext,
        IOptions<CameraStatusSettings> settings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _settings = settings.Value;
    }

    /// <summary>
    /// Called by the DeviceStatusService or MqttReceiverService when an AIBox status payload
    /// contains camera status information.
    /// </summary>
    public void UpdateAiBoxCameraStatus(string cameraId, string status)
    {
        lock (_aiBoxLock)
        {
            _aiBoxCameraStatuses[cameraId] = status;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Camera Status Service starting with check interval {Interval}s, timeout {Timeout}s, max concurrent {MaxConcurrent}",
            _settings.CheckIntervalSeconds, _settings.TimeoutSeconds, _settings.MaxConcurrentChecks);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllCameras(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Camera Status Service check cycle");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Camera Status Service stopped");
    }

    private async Task CheckAllCameras(CancellationToken stoppingToken)
    {
        List<Camera> cameras;

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            cameras = await db.Cameras
                .Where(c => c.Enabled)
                .ToListAsync(stoppingToken);
        }

        if (cameras.Count == 0)
            return;

        _logger.LogDebug("Checking status of {Count} enabled cameras", cameras.Count);

        var semaphore = new SemaphoreSlim(_settings.MaxConcurrentChecks);
        var tasks = cameras.Select(camera => CheckCameraWithSemaphore(camera, semaphore, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task CheckCameraWithSemaphore(Camera camera, SemaphoreSlim semaphore, CancellationToken stoppingToken)
    {
        await semaphore.WaitAsync(stoppingToken);
        try
        {
            var newStatus = await DetermineStatus(camera, stoppingToken);
            await UpdateCameraStatusIfChanged(camera, newStatus, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking status for camera {CameraId}", camera.CameraId);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Waterfall status check: AIBox → RTSP → Ping → ONVIF.
    /// First successful check determines the status.
    /// </summary>
    private async Task<string> DetermineStatus(Camera camera, CancellationToken stoppingToken)
    {
        // Step 1: Check AIBox-reported status
        var aiBoxStatus = CheckAiBoxStatus(camera.CameraId);
        if (aiBoxStatus != null)
        {
            _logger.LogDebug("Camera {CameraId} status from AIBox payload: {Status}", camera.CameraId, aiBoxStatus);
            return aiBoxStatus;
        }

        // Step 2: RTSP TCP probe if RTSP URL configured
        if (!string.IsNullOrEmpty(camera.RtspUrl))
        {
            var rtspResult = await CheckRtspConnection(camera, stoppingToken);
            if (rtspResult != null)
            {
                _logger.LogDebug("Camera {CameraId} RTSP probe result: {Status}", camera.CameraId, rtspResult);
                return rtspResult;
            }
        }

        // Step 3: ICMP Ping if IP address known
        if (!string.IsNullOrEmpty(camera.IpAddress))
        {
            var pingResult = await CheckPing(camera, stoppingToken);
            if (pingResult != null)
            {
                _logger.LogDebug("Camera {CameraId} ICMP ping result: {Status}", camera.CameraId, pingResult);
                return pingResult;
            }
        }

        // Step 4: ONVIF query if enabled
        if (camera.OnvifEnabled && !string.IsNullOrEmpty(camera.OnvifHost))
        {
            var onvifResult = await CheckOnvif(camera, stoppingToken);
            if (onvifResult != null)
            {
                _logger.LogDebug("Camera {CameraId} ONVIF probe result: {Status}", camera.CameraId, onvifResult);
                return onvifResult;
            }
        }

        // All checks failed or no check methods available
        return "offline";
    }

    /// <summary>
    /// Check if AIBox status payload has camera info. Returns null if no info available.
    /// </summary>
    private string? CheckAiBoxStatus(string cameraId)
    {
        lock (_aiBoxLock)
        {
            if (_aiBoxCameraStatuses.TryGetValue(cameraId, out var status))
            {
                // Remove the status so it doesn't persist indefinitely -
                // it will be refreshed by the next AIBox status message
                _aiBoxCameraStatuses.Remove(cameraId);
                return status;
            }
        }
        return null;
    }

    /// <summary>
    /// TCP connect to the RTSP port (default 554) with configured timeout.
    /// Returns "online" on successful connection, null on failure (to try next method).
    /// </summary>
    private async Task<string?> CheckRtspConnection(Camera camera, CancellationToken stoppingToken)
    {
        try
        {
            var (host, port) = ParseRtspUrl(camera.RtspUrl!);

            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            await tcpClient.ConnectAsync(host, port, cts.Token);
            return "online";
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout - connection attempt timed out, try next method
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RTSP TCP probe failed for camera {CameraId} at {RtspUrl}",
                camera.CameraId, camera.RtspUrl);
            return null;
        }
    }

    /// <summary>
    /// Send ICMP ping to camera IP with 2-second timeout.
    /// Returns "online" on successful ping, null on failure (to try next method).
    /// </summary>
    private async Task<string?> CheckPing(Camera camera, CancellationToken stoppingToken)
    {
        try
        {
            using var ping = new Ping();
            var timeout = Math.Min(_settings.TimeoutSeconds * 1000, 2000); // Use 2s timeout for ping
            var reply = await ping.SendPingAsync(camera.IpAddress!, timeout);

            if (reply.Status == IPStatus.Success)
            {
                return "online";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ICMP ping failed for camera {CameraId} at {IpAddress}",
                camera.CameraId, camera.IpAddress);
            return null;
        }
    }

    /// <summary>
    /// Attempt ONVIF connection to camera. Uses TCP connect to ONVIF port (default 80) as a proxy
    /// for full ONVIF GetDeviceInformation (without requiring a full ONVIF library).
    /// Returns "online" on successful connection, null on failure.
    /// </summary>
    private async Task<string?> CheckOnvif(Camera camera, CancellationToken stoppingToken)
    {
        try
        {
            var host = camera.OnvifHost!;
            var port = 80; // Default ONVIF HTTP port

            // Parse port from OnvifHost if it contains a port specification
            if (host.Contains(':'))
            {
                var parts = host.Split(':');
                host = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsedPort))
                {
                    port = parsedPort;
                }
            }

            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            await tcpClient.ConnectAsync(host, port, cts.Token);
            return "online";
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ONVIF probe failed for camera {CameraId} at {OnvifHost}",
                camera.CameraId, camera.OnvifHost);
            return null;
        }
    }

    /// <summary>
    /// Parse RTSP URL to extract host and port. Default port is 554.
    /// Supports formats: rtsp://host:port/path, rtsp://host/path, host:port
    /// </summary>
    private static (string host, int port) ParseRtspUrl(string rtspUrl)
    {
        var defaultPort = 554;

        try
        {
            // Try to parse as URI first
            if (Uri.TryCreate(rtspUrl, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : defaultPort;
                return (host, port);
            }

            // Fallback: try as host:port
            if (rtspUrl.Contains(':'))
            {
                var parts = rtspUrl.Split(':');
                var host = parts[0];
                if (int.TryParse(parts[1].Split('/')[0], out var port))
                {
                    return (host, port);
                }
            }

            return (rtspUrl, defaultPort);
        }
        catch
        {
            return (rtspUrl, defaultPort);
        }
    }

    /// <summary>
    /// Update the camera status in the database if it has changed, and broadcast via SignalR.
    /// </summary>
    private async Task UpdateCameraStatusIfChanged(Camera camera, string newStatus, CancellationToken stoppingToken)
    {
        if (camera.Status == newStatus)
            return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbCamera = await db.Cameras.FindAsync(new object[] { camera.CameraId }, stoppingToken);
        if (dbCamera == null)
            return;

        var oldStatus = dbCamera.Status;
        dbCamera.Status = newStatus;
        dbCamera.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(stoppingToken);

        // Broadcast status change via SignalR
        await _hubContext.Clients.All.SendAsync("CameraStatusChanged", new
        {
            cameraId = camera.CameraId,
            status = newStatus,
            updatedAt = DateTime.UtcNow
        }, stoppingToken);

        _logger.LogInformation("Camera {CameraId} status changed from {OldStatus} to {NewStatus}",
            camera.CameraId, oldStatus, newStatus);
    }
}
