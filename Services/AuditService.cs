using OrderTrackingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderTrackingApp.Services
{
    public interface IAuditService
    {
        Task LogAsync(string eventType, string action, string? username = null, string? userId = null,
            string? entityName = null, string? entityId = null, string? ipAddress = null,
            string? userAgent = null, bool success = true, string? details = null);
        
        Task<List<AuditLog>> GetRecentLogsAsync(int limit = 200, string? eventType = null, string? username = null);
    }
    
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditService> _logger;
        
        public AuditService(AppDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task LogAsync(string eventType, string action, string? username = null, string? userId = null,
            string? entityName = null, string? entityId = null, string? ipAddress = null,
            string? userAgent = null, bool success = true, string? details = null)
        {
            try
            {
                var log = new AuditLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    UserId = userId,
                    Username = SanitizeUsername(username),
                    EventType = eventType,
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = action,
                    IpAddress = SanitizeIp(ipAddress),
                    UserAgent = SanitizeUserAgent(userAgent),
                    Success = success,
                    Details = SanitizeDetails(details)
                };
                
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Audit: {EventType} by {Username}", eventType, username ?? "anonymous");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log");
            }
        }
        
        public async Task<List<AuditLog>> GetRecentLogsAsync(int limit = 200, string? eventType = null, string? username = null)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();
                
                if (!string.IsNullOrEmpty(eventType))
                {
                    query = query.Where(l => l.EventType == eventType);
                }
                
                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(l => l.Username != null && l.Username.Contains(username));
                }
                
                return await query.OrderByDescending(l => l.TimestampUtc).Take(limit).ToListAsync();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogWarning("AuditLogs table not found, returning empty list");
                return new List<AuditLog>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading audit logs");
                return new List<AuditLog>();
            }
        }
        
        private string? SanitizeUsername(string? username)
        {
            if (string.IsNullOrEmpty(username)) return null;
            
            var sanitized = System.Text.RegularExpressions.Regex.Replace(username, @"[^\w\-_.@]", "");
            return sanitized.Length > 100 ? sanitized.Substring(0, 100) : sanitized;
        }
        
        private string? SanitizeIp(string? ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;
            if (!System.Net.IPAddress.TryParse(ip, out _)) return null;
            return ip.Length > 45 ? ip.Substring(0, 45) : ip;
        }
        
        private string? SanitizeUserAgent(string? ua)
        {
            if (string.IsNullOrEmpty(ua)) return null;
            if (ua.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("cookie", StringComparison.OrdinalIgnoreCase))
            {
                return "[sensitive]";
            }
            return ua.Length > 500 ? ua.Substring(0, 500) : ua;
        }
        
        private string? SanitizeDetails(string? details)
        {
            if (string.IsNullOrEmpty(details)) return null;
            
            var sanitized = details;
            var pattern = @"(password|token|key|secret)[^a-zA-Z0-9]*[:=][^a-zA-Z0-9]*[^\s\,]+";
            try {
                sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, pattern, "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            } catch { }
            
            return sanitized.Length > 1000 ? sanitized.Substring(0, 1000) : sanitized;
        }
    }
}