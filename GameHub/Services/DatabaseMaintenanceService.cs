using System;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using GameHub.Models;

namespace GameHub.Services
{
    public class DatabaseMaintenanceService
    {
        private readonly GameManagementMISEntities _db;

        public DatabaseMaintenanceService(GameManagementMISEntities db)
        {
            _db = db;
        }

        public string BackupDatabase(string performedBy)
        {
            var providerConnString = _db.Database.Connection.ConnectionString;
            var builder = new SqlConnectionStringBuilder(providerConnString);
            var dbName = builder.InitialCatalog;

            var backupsDir = HttpContext.Current.Server.MapPath("~/App_Data/Backups");
            Directory.CreateDirectory(backupsDir);
            var fileName = $"{dbName}_{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
            var fullPath = Path.Combine(backupsDir, fileName);

            using (var conn = new SqlConnection(builder.ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "BACKUP DATABASE [" + dbName + "] TO DISK = @path WITH FORMAT, INIT, NAME = @name, SKIP, STATS = 5";
                cmd.Parameters.AddWithValue("@path", fullPath);
                cmd.Parameters.AddWithValue("@name", $"{dbName}_FullBackup_{DateTime.UtcNow:yyyyMMddHHmmss}");
                cmd.CommandTimeout = 0; // allow long running
                cmd.ExecuteNonQuery();
            }

            LogBackup(fullPath, performedBy);
            return fullPath;
        }

        public void RestoreDatabase(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found", backupFilePath);
            }

            var providerConnString = _db.Database.Connection.ConnectionString;
            var builder = new SqlConnectionStringBuilder(providerConnString);
            var dbName = builder.InitialCatalog;

            var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
            {
                InitialCatalog = "master"
            };

            using (var conn = new SqlConnection(masterBuilder.ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandTimeout = 0;
                cmd.CommandText =
                    "ALTER DATABASE [" + dbName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" +
                    "RESTORE DATABASE [" + dbName + "] FROM DISK = @path WITH REPLACE;" +
                    "ALTER DATABASE [" + dbName + "] SET MULTI_USER;";
                cmd.Parameters.AddWithValue("@path", backupFilePath);
                cmd.ExecuteNonQuery();
            }
        }

        private void LogBackup(string path, string performedBy)
        {
            var log = new BackupLog
            {
                BackupDate = DateTime.UtcNow,
                FilePath = path,
                PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy
            };
            _db.BackupLogs.Add(log);
            _db.SaveChanges();
        }

        public static string[] ListBackupFiles()
        {
            var backupsDir = HttpContext.Current.Server.MapPath("~/App_Data/Backups");
            if (!Directory.Exists(backupsDir))
            {
                return new string[0];
            }
            return Directory.GetFiles(backupsDir, "*.bak");
        }
    }
}
