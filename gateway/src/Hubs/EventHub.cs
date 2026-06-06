using Microsoft.AspNetCore.SignalR;

namespace ControlCenter.Gateway.Hubs;

public class EventHub : Hub
{
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
}
