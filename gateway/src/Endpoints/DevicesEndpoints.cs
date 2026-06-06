using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

/// <summary>
/// Response DTO that excludes sensitive credential fields from API responses.
/// </summary>
public record DeviceResponse(
    string DeviceId,
    string DeviceName,
    string? SiteId,
    string? SiteName,
    string? Location,
    string? IpAddress,
    string? MqttHost,
    int MqttPort,
    string? MqttUsername,
    string? EventTopic,
    string? StatusTopic,
    bool Enabled,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Status
)
{
    public static DeviceResponse FromEntity(Device device) => new(
        device.DeviceId,
        device.DeviceName,
        device.SiteId,
        device.SiteName,
        device.Location,
        device.IpAddress,
        device.MqttHost,
        device.MqttPort,
        device.MqttUsername,
        device.EventTopic,
        device.StatusTopic,
        device.Enabled,
        device.Description,
        device.CreatedAt,
        device.UpdatedAt,
        device.Status
    );
}

public static class DevicesEndpoints
{
    public static void MapDevicesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/devices");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var devices = await db.Devices
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return Results.Ok(devices.Select(DeviceResponse.FromEntity));
        });

        group.MapGet("/{id}", async (string id, AppDbContext db) =>
        {
            var device = await db.Devices
                .Include(d => d.Cameras)
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            return device is null ? Results.NotFound() : Results.Ok(DeviceResponse.FromEntity(device));
        });

        group.MapPost("/", async (Device device, AppDbContext db) =>
        {
            device.CreatedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(device.DeviceId))
            {
                device.DeviceId = Guid.NewGuid().ToString();
            }

            db.Devices.Add(device);
            await db.SaveChangesAsync();
            return Results.Created($"/api/devices/{device.DeviceId}", device);
        });

        group.MapPut("/{id}", async (string id, Device updated, AppDbContext db) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            device.DeviceName = updated.DeviceName;
            device.SiteId = updated.SiteId;
            device.SiteName = updated.SiteName;
            device.Location = updated.Location;
            device.IpAddress = updated.IpAddress;
            device.MqttHost = updated.MqttHost;
            device.MqttPort = updated.MqttPort;
            device.MqttUsername = updated.MqttUsername;
            device.MqttPassword = updated.MqttPassword;
            device.EventTopic = updated.EventTopic;
            device.StatusTopic = updated.StatusTopic;
            device.Enabled = updated.Enabled;
            device.Description = updated.Description;
            device.Status = updated.Status;
            device.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(device);
        });

        group.MapDelete("/{id}", async (string id, AppDbContext db) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            db.Devices.Remove(device);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
