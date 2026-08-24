using System;

namespace OrderTrackingApp.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }
    
    public class AdminDatabaseViewModel
    {
        public Guid InstallationId { get; set; }
        public string CurrentState { get; set; } = "";
        public string DatabaseProvider { get; set; } = "";
        public string DatabasePath { get; set; } = "";
        public DateTime InstallationDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public int ProjectCount { get; set; }
        public string Environment { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public string DbConnectionStatus { get; set; } = "";
        public string InstallationProfile { get; set; } = "";
        
        public string ConfigProvider { get; set; } = "";
        public string ConfigPath { get; set; } = "";
        public string ConfigHost { get; set; } = "";
        public int ConfigPort { get; set; }
        public string ConfigDatabaseName { get; set; } = "";
        public string ConfigUsername { get; set; } = "";
        public string ConfigPassword { get; set; } = "";
        public bool ConfigSsl { get; set; }
    }
}