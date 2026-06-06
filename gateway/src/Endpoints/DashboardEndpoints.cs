using ControlCenter.Gateway.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/summary", async (AppDbContext db) =>
        {
            var totalDevices = await db.Devices.CountAsync();
            var onlineDevices = await db.Devices.CountAsync(d => d.Status == "online");
            var totalCameras = await db.Cameras.CountAsync();
            var onlineCameras = await db.Cameras.CountAsync(c => c.Status == "online");
            var unconfirmedEvents = await db.Events.CountAsync(e => e.AckStatus == "unconfirmed");

            var todayStart = DateTime.UtcNow.Date.ToString("o");
            var todayEvents = await db.Events.CountAsync(e => e.Timestamp.CompareTo(todayStart) >= 0);

            return Results.Ok(new
            {
                total_devices = totalDevices,
                online_devices = onlineDevices,
                total_cameras = totalCameras,
                online_cameras = onlineCameras,
                unconfirmed_events = unconfirmedEvents,
                today_events = todayEvents
            });
        });
    }
}
