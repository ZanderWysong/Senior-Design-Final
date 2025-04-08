using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using ZitaDataSystem;
using ZitaDataSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Set working directory to executable's directory
var exePath = Assembly.GetExecutingAssembly().Location;
var exeDir = Path.GetDirectoryName(exePath);
if (!string.IsNullOrEmpty(exeDir))
{
    Directory.SetCurrentDirectory(exeDir);
}

// Register services
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<SDDGService>();
builder.Services.AddSingleton<EndpointsService>();
builder.Services.AddSingleton<DatabaseBackupService>();
builder.Services.AddHostedService<Worker>();

// Controller + XML formatter support
builder.Services.AddControllers().AddXmlSerializerFormatters();

// Windows service support
builder.Host.UseWindowsService();

// Configure frontend CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Bind to custom port
builder.WebHost.UseUrls("http://localhost:8836");

var app = builder.Build();

// Middleware order is important
app.UseRouting();
app.UseCors("AllowFrontend");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // This activates [Route] attributes
});

// Start recurring DB backup service
var backupService = app.Services.GetRequiredService<DatabaseBackupService>();
var timer = new System.Timers.Timer(3600000); // 1 hour
timer.Elapsed += (sender, e) => backupService.BackupDatabase();
timer.Start();

app.Run();
