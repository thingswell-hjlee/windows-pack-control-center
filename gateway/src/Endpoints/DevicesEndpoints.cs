using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using ControlCenter.Gateway.Services;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

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
            return Results.Ok(devices);
        });

        group.MapGet("/{id}", async (string id, AppDbContext db) =>
        {
            var device = await db.Devices
                .Include(d => d.Cameras)
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            return device is null ? Results.NotFound() : Results.Ok(device);
        });

        group.MapPost("/", async (Device device, AppDbContext db) =>
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(device.DeviceName))
                return Results.BadRequest(new { error = "device_name is required" });

            // If device_id provided, check for duplicate
            if (!string.IsNullOrEmpty(device.DeviceId))
            {
                var exists = await db.Devices.AnyAsync(d => d.DeviceId == device.DeviceId);
                if (exists)
                    return Results.Conflict(new { error = $"Device with id '{device.DeviceId}' already exists" });
            }
            else
            {
                device.DeviceId = Guid.NewGuid().ToString();
            }

            // Set default mqtt_port if not provided
            if (device.MqttPort <= 0)
                device.MqttPort = 1883;

            device.CreatedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            db.Devices.Add(device);
            await db.SaveChangesAsync();
            return Results.Created($"/api/devices/{device.DeviceId}", device);
        });

        group.MapPut("/{id}", async (string id, Device updated, AppDbContext db, MqttReceiverService mqttReceiver) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            // Track whether MQTT connection fields changed
            var mqttFieldsChanged = device.MqttHost != updated.MqttHost
                || device.MqttPort != updated.MqttPort
                || device.MqttUsername != updated.MqttUsername
                || device.MqttPassword != updated.MqttPassword;

            // Track whether device was previously enabled
            var wasEnabled = device.Enabled;

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

            // When a device is disabled, set its status to "disabled"
            if (!updated.Enabled)
            {
                device.Status = "disabled";
            }

            await db.SaveChangesAsync();

            // Handle MQTT connection changes
            if (!updated.Enabled)
            {
                // Device was disabled: disconnect MQTT client
                await mqttReceiver.DisconnectDevice(id);
            }
            else if (mqttFieldsChanged)
            {
                // MQTT fields changed on an enabled device: reconnect
                await mqttReceiver.ReconnectDevice(id);
            }
            else if (!wasEnabled && updated.Enabled)
            {
                // Device was just enabled: reconnect to initiate connection
                await mqttReceiver.ReconnectDevice(id);
            }

            return Results.Ok(device);
        });

        group.MapDelete("/{id}", async (string id, AppDbContext db, MqttReceiverService mqttReceiver) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
                return Results.NotFound();

            // Disconnect MQTT client before removing device
            await mqttReceiver.DisconnectDevice(id);

            db.Devices.Remove(device);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
