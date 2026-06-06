using System.Globalization;
using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Hubs;
using ControlCenter.Gateway.Models;
using CsvHelper;
using MiniExcelLibs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ControlCenter.Gateway.Endpoints;

public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/", async (
            string? device_id,
            string? camera_id,
            string? event_type,
            string? severity,
            string? ack_status,
            string? start_date,
            string? end_date,
            int? page,
            int? page_size,
            AppDbContext db) =>
        {
            var query = db.Events.AsQueryable();

            if (!string.IsNullOrEmpty(device_id))
                query = query.Where(e => e.DeviceId == device_id);

            if (!string.IsNullOrEmpty(camera_id))
                query = query.Where(e => e.CameraId == camera_id);

            if (!string.IsNullOrEmpty(event_type))
                query = query.Where(e => e.EventType == event_type);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);

            if (!string.IsNullOrEmpty(ack_status))
                query = query.Where(e => e.AckStatus == ack_status);

            if (!string.IsNullOrEmpty(start_date) && DateTime.TryParse(start_date, out var startDt))
            {
                var startMs = new DateTimeOffset(startDt.ToUniversalTime()).ToUnixTimeMilliseconds();
                query = query.Where(e => e.TsMs >= startMs);
            }

            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParse(end_date, out var endDt))
            {
                var endMs = new DateTimeOffset(endDt.ToUniversalTime()).ToUnixTimeMilliseconds();
                query = query.Where(e => e.TsMs <= endMs);
            }

            var pageNum = page ?? 1;
            var pageSize = page_size ?? 50;
            var total = await query.CountAsync();

            var events = await query
                .OrderByDescending(e => e.TsMs)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                items = events,
                total,
                page = pageNum,
                page_size = pageSize,
                total_pages = (int)Math.Ceiling((double)total / pageSize)
            });
        });

        group.MapGet("/{id}", async (string id, AppDbContext db) =>
        {
            var ev = await db.Events.FindAsync(id);
            return ev is null ? Results.NotFound() : Results.Ok(ev);
        });

        group.MapPut("/{id}/ack", async (string id, AckRequest request, AppDbContext db, IHubContext<EventHub> hubContext) =>
        {
            var ev = await db.Events.FindAsync(id);
            if (ev is null)
                return Results.NotFound();

            // Idempotent: if already confirmed, return current state without modification
            if (ev.AckStatus == "confirmed")
                return Results.Ok(ev);

            ev.AckStatus = "confirmed";
            ev.AckUser = request.AckUser;
            ev.AckTime = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Broadcast EventAcknowledged via SignalR
            await hubContext.Clients.All.SendAsync("EventAcknowledged", new
            {
                eventId = id,
                ackStatus = ev.AckStatus,
                ackUser = ev.AckUser,
                ackTime = ev.AckTime
            });

            return Results.Ok(ev);
        });

        group.MapPut("/{id}/memo", async (string id, MemoRequest request, AppDbContext db, IHubContext<EventHub> hubContext) =>
        {
            var ev = await db.Events.FindAsync(id);
            if (ev is null)
                return Results.NotFound();

            ev.ActionMemo = request.ActionMemo;
            await db.SaveChangesAsync();

            // Broadcast EventMemoUpdated via SignalR
            await hubContext.Clients.All.SendAsync("EventMemoUpdated", new
            {
                eventId = id,
                actionMemo = ev.ActionMemo
            });

            return Results.Ok(ev);
        });

        group.MapGet("/export/csv", async (
            string? device_id,
            string? camera_id,
            string? event_type,
            string? severity,
            string? ack_status,
            string? start_date,
            string? end_date,
            AppDbContext db) =>
        {
            var query = db.Events.AsQueryable();

            if (!string.IsNullOrEmpty(device_id))
                query = query.Where(e => e.DeviceId == device_id);

            if (!string.IsNullOrEmpty(camera_id))
                query = query.Where(e => e.CameraId == camera_id);

            if (!string.IsNullOrEmpty(event_type))
                query = query.Where(e => e.EventType == event_type);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);

            if (!string.IsNullOrEmpty(ack_status))
                query = query.Where(e => e.AckStatus == ack_status);

            if (!string.IsNullOrEmpty(start_date) && DateTime.TryParse(start_date, out var startDt))
            {
                var startMs = new DateTimeOffset(startDt.ToUniversalTime()).ToUnixTimeMilliseconds();
                query = query.Where(e => e.TsMs >= startMs);
            }

            if (!string.IsNullOrEmpty(end_date) && DateTime.TryParse(end_date, out var endDt))
            {
                var endMs = new DateTimeOffset(endDt.ToUniversalTime()).ToUnixTimeMilliseconds();
                query = query.Where(e => e.TsMs <= endMs);
            }

            var events = await query.OrderByDescending(e => e.TsMs).ToListAsync();

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(events);
            var content = writer.ToString();

            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(content),
                "text/csv",
                "events_export.csv");
        });
    }
}

public record AckRequest(string AckUser);
public record MemoRequest(string ActionMemo);
