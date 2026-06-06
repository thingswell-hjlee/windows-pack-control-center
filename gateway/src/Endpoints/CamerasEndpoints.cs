using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

public static class CamerasEndpoints
{
    public static void MapCamerasEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cameras");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var cameras = await db.Cameras
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return Results.Ok(cameras);
        });

        group.MapGet("/{id}", async (string id, AppDbContext db) =>
        {
            var camera = await db.Cameras.FindAsync(id);
            return camera is null ? Results.NotFound() : Results.Ok(camera);
        });

        group.MapPost("/", async (Camera camera, AppDbContext db) =>
        {
            camera.CreatedAt = DateTime.UtcNow;
            camera.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(camera.CameraId))
            {
                camera.CameraId = Guid.NewGuid().ToString();
            }

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
            camera.Status = updated.Status;
            camera.Enabled = updated.Enabled;
            camera.Description = updated.Description;
            camera.UpdatedAt = DateTime.UtcNow;

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
