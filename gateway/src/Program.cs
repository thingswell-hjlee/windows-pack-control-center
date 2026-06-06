using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Endpoints;
using ControlCenter.Gateway.Hubs;
using ControlCenter.Gateway.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Control Center Gateway");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Configure Kestrel to listen on port 8088
    builder.WebHost.UseUrls("http://0.0.0.0:8088");

    // Add EF Core SQLite
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=controlcenter.db"));

    // Add SignalR
    builder.Services.AddSignalR();

    // Add CORS for localhost development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8088")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configure MQTT settings from appsettings.json
    builder.Services.Configure<MqttSettings>(
        builder.Configuration.GetSection(MqttSettings.SectionName));

    // Register background services
    builder.Services.AddSingleton<MqttReceiverService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttReceiverService>());
    builder.Services.AddHostedService<EventNormalizerService>();
    builder.Services.AddHostedService<DeviceStatusService>();

    var app = builder.Build();

    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    // Configure middleware
    app.UseCors();
    app.UseStaticFiles();

    // Map endpoints
    app.MapDevicesEndpoints();
    app.MapCamerasEndpoints();
    app.MapEventsEndpoints();
    app.MapDashboardEndpoints();

    // Map SignalR hub
    app.MapHub<EventHub>("/hubs/events");

    // Fallback to index.html for SPA routing
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
