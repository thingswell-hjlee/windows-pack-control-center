using Microsoft.AspNetCore.SignalR;

namespace ControlCenter.Gateway.Hubs;

public class EventHub : Hub
{
    // Server-to-client message types:
    // "NewEvent" - NormalizedEvent object (sent from EventNormalizerService)
    // "DeviceStatusChanged" - { deviceId, status, updatedAt } (sent from DeviceStatusService)
    // "CameraStatusChanged" - { cameraId, status, updatedAt } (sent from CameraStatusService)
    // "EventAcknowledged" - { eventId, ackStatus, ackUser, ackTime } (sent from Events endpoint)
    // "EventMemoUpdated" - { eventId, actionMemo } (sent from Events endpoint)

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendNewEvent(object eventData)
    {
        await Clients.All.SendAsync("NewEvent", eventData);
    }

    public async Task SendDeviceStatusChanged(object statusData)
    {
        await Clients.All.SendAsync("DeviceStatusChanged", statusData);
    }

    public async Task SendCameraStatusChanged(object statusData)
    {
        await Clients.All.SendAsync("CameraStatusChanged", statusData);
    }

    public async Task SendEventAcknowledged(object ackData)
    {
        await Clients.All.SendAsync("EventAcknowledged", ackData);
    }

    public async Task SendEventMemoUpdated(object memoData)
    {
        await Clients.All.SendAsync("EventMemoUpdated", memoData);
    }
}
