using System;

namespace OrderTrackingApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        public DateTime TimestampUtc { get; set; }
        
        public string? UserId { get; set; }
        
        public string? Username { get; set; }
        
        public string EventType { get; set; } = "";
        
        public string? EntityName { get; set; }
        
        public string? EntityId { get; set; }
        
        public string Action { get; set; } = "";
        
        public string? IpAddress { get; set; }
        
        public string? UserAgent { get; set; }
        
        public bool Success { get; set; }
        
        public string? Details { get; set; }
    }
    
    public class AuditLogViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public string? FilterEventType { get; set; }
        public string? FilterUsername { get; set; }
    }
}