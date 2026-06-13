using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

public static class CamerasEndpoints
{
    public static void MapCamerasEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cameras");

        group.MapGet("/", async (string? device_id, AppDbContext db) =>
        {
            var query = db.Cameras.AsQueryable();
            if (!string.IsNullOrEmpty(device_id))
                query = query.Where(c => c.DeviceId == device_id);
            var cameras = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return Results.Ok(cameras);
        });

        group.MapGet("/{id}", async (string id, AppDbContext db) =>
        {
            var camera = await db.Cameras.FindAsync(id);
            return camera is null ? Results.NotFound() : Results.Ok(camera);
        });

        group.MapPost("/", async (Camera camera, AppDbContext db) =>
        {
            // Validate required fields (Requirements 5.1, 5.2)
            if (string.IsNullOrWhiteSpace(camera.CameraId))
                return Results.BadRequest(new { error = "camera_id is required" });
            if (string.IsNullOrWhiteSpace(camera.CameraName))
                return Results.BadRequest(new { error = "camera_name is required" });
            if (string.IsNullOrWhiteSpace(camera.DeviceId))
                return Results.BadRequest(new { error = "device_id is required" });

            // Validate device exists (Requirement 5.3)
            var deviceExists = await db.Devices.AnyAsync(d => d.DeviceId == camera.DeviceId);
            if (!deviceExists)
                return Results.BadRequest(new { error = $"Device '{camera.DeviceId}' not found" });

            // Check for duplicate camera_id (Requirement 5.5)
            var exists = await db.Cameras.AnyAsync(c => c.CameraId == camera.CameraId);
            if (exists)
                return Results.Conflict(new { error = $"Camera with id '{camera.CameraId}' already exists" });

            camera.CreatedAt = DateTime.UtcNow;
            camera.UpdatedAt = DateTime.UtcNow;

            db.Cameras.Add(camera);
            await db.SaveChangesAsync();
            return Results.Created($"/api/cameras/{camera.CameraId}", camera);
        });

        group.MapPut("/{id}", async (string id, Camera updated, AppDbContext db) =>
        {
            var camera = await db.Cameras.FindAsync(id);
            if (camera is null)
                return Results.NotFound();

            camera.CameraName = updated.CameraName;
            camera.SiteId = updated.SiteId;
            camera.SiteName = updated.SiteName;
            camera.DeviceId = updated.DeviceId;
            camera.ChannelNo = updated.ChannelNo;
            camera.Location = updated.Location;
            camera.IpAddress = updated.IpAddress;
            camera.RtspUrl = updated.RtspUrl;
            camera.OnvifEnabled = updated.OnvifEnabled;
            camera.OnvifHost = updated.OnvifHost;
            camera.OnvifUsername = updated.OnvifUsername;
            camera.OnvifPassword = updated.OnvifPassword;
            camera.Enabled = updated.Enabled;
            camera.Description = updated.Description;
            camera.UpdatedAt = DateTime.UtcNow;

            // When a camera is disabled, automatically set status to "disabled" (Requirement 7.6)
            if (!updated.Enabled)
            {
                camera.Status = "disabled";
            }
            else
            {
                camera.Status = updated.Status;
            }

            await db.SaveChangesAsync();
            return Results.Ok(camera);
        });

        group.MapDelete("/{id}", async (string id, AppDbContext db) =>
        {
            var camera = await db.Cameras.FindAsync(id);
            if (camera is null)
                return Results.NotFound();

            db.Cameras.Remove(camera);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
