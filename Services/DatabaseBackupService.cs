using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ZitaDataSystem.Services
{
    public class DatabaseBackupService
    {
        private readonly string _primaryDatabase;
        private readonly string _backupDatabase;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(IConfiguration configuration, ILogger<DatabaseBackupService> logger)
        {
            // For SQLite, the connection strings should be in the format "Data Source=PRIMARY.db"
            _primaryDatabase = configuration.GetConnectionString("PrimaryDatabase")
                               ?? throw new InvalidOperationException("PrimaryDatabase connection string is missing.");
            _backupDatabase = configuration.GetConnectionString("BackupDatabase")
                              ?? throw new InvalidOperationException("BackupDatabase connection string is missing.");
            _logger = logger;
        }

        public void BackupDatabase()
        {
            try
            {
                _logger.LogInformation("Starting SQLite database backup...");

                // Extract file paths from the connection strings.
                string primaryFile = GetDataSource(_primaryDatabase);
                string backupFile = GetDataSource(_backupDatabase);

                if (!File.Exists(primaryFile))
                {
                    _logger.LogError($"Primary database file '{primaryFile}' does not exist.");
                    return;
                }

                // Copy the primary database file to the backup file.
                // The 'true' parameter overwrites the backup if it already exists.
                File.Copy(primaryFile, backupFile, true);
                _logger.LogInformation("SQLite database backup completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"SQLite database backup process failed: {ex.Message}");
            }
        }

        // Helper method to extract the file path from a SQLite connection string.
        private string GetDataSource(string connectionString)
        {
            // Assumes connection string is in the format "Data Source=YourFileName.db"
            var parts = connectionString.Split("=", 2);
            if (parts.Length == 2)
            {
                return parts[1].Trim();
            }
            throw new InvalidOperationException("Invalid SQLite connection string format.");
        }
    }
}
