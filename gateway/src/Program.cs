using ControlCenter.Gateway.Data;
using ControlCenter.Gateway.Endpoints;
using ControlCenter.Gateway.Hubs;
using ControlCenter.Gateway.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Determine data directory - use ProgramData if not in development
var dataDir = Environment.GetEnvironmentVariable("CONTROLCENTER_DATA_DIR")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Thingswell", "WindowsPackControlCenter");

if (!Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

// Ensure logs subdirectory exists
var logDir = Path.Combine(dataDir, "logs");
if (!Directory.Exists(logDir))
    Directory.CreateDirectory(logDir);

var logPath = Path.Combine(logDir, "gateway-.log");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
    .CreateLogger();

try
{
    Log.Information("Starting Control Center Gateway");
    Log.Information("Data directory: {DataDir}", dataDir);

    var builder = WebApplication.CreateBuilder(args);

    // Override connection string to use data directory for SQLite
    var dbPath = Path.Combine(dataDir, "controlcenter.db");
    builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}";

    builder.Host.UseSerilog();

    // Configure Kestrel URL from appsettings.json (Kestrel:Endpoints:Http:Url)
    var kestrelUrl = builder.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://0.0.0.0:8088";
    builder.WebHost.UseUrls(kestrelUrl);

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

    // Configure Camera Status settings from appsettings.json
    builder.Services.Configure<CameraStatusSettings>(
        builder.Configuration.GetSection(CameraStatusSettings.SectionName));

    // Register background services
    builder.Services.AddSingleton<MqttReceiverService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttReceiverService>());
    builder.Services.AddHostedService<EventNormalizerService>();
    builder.Services.AddHostedService<DeviceStatusService>();
    builder.Services.AddHostedService<CameraStatusService>();

    var app = builder.Build();

    // Check if the configured port is already in use
    var port = new Uri(kestrelUrl.Replace("0.0.0.0", "localhost")).Port;
    try
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
        listener.Start();
        listener.Stop();
    }
    catch (System.Net.Sockets.SocketException)
    {
        Log.Fatal("Port {Port} is already in use. Another instance may be running.", port);
        Environment.ExitCode = 1;
        return;
    }

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
