using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZitaDataSystem.Services
{
    public class DatabaseBackupWorker : BackgroundService
    {
        private readonly ILogger<DatabaseBackupWorker> _logger;

        public DatabaseBackupWorker(ILogger<DatabaseBackupWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Database Backup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Performing database backup at {time}", DateTimeOffset.Now);
                await Task.Delay(3600000, stoppingToken); // Runs every hour
            }

            _logger.LogInformation("Database Backup Service is stopping.");
        }
    }
}
