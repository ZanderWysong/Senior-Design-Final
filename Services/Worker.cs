using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ZitaDataSystem.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            // Get the current assembly version.
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            
            // Get the last write time of the executing assembly.
            var exePath = assembly.Location;
            var lastModified = File.GetLastWriteTime(exePath);

            // Log version info on startup.
            _logger.LogInformation($"Worker starting. Assembly Version: {version}, Executable last modified: {lastModified}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Zita Data System Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Zita Data System Service is running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }

            _logger.LogInformation("Zita Data System Service is stopping.");
        }
    }
}
