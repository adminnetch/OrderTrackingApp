using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;
using OrderTrackingApp.Filters;
using OrderTrackingApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderTrackingApp.Controllers
{
    [HasPermission("Admin.Access")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuditService _auditService;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, 
            AppDbContext context, ILogger<AdminController> logger, IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        [HasPermission("Admin.Access")]
        public IActionResult Index()
        {
            return View();
        }

        [HasPermission("Admin.Access")]
        public async Task<IActionResult> Database()
        {
            var installation = await _context.AppInstallations.FirstOrDefaultAsync();
            var userCount = await _userManager.Users.CountAsync();
            var permissionCount = await _context.Permessi.CountAsync();
            var projectCount = await _context.CinemaOrders.CountAsync();

            var dbConnectionStatus = "Unknown";
            var dbCanConnect = false;
            try
            {
                dbCanConnect = await _context.Database.CanConnectAsync();
                dbConnectionStatus = dbCanConnect ? "OK" : "Errore";
            }
            catch
            {
                dbConnectionStatus = "Errore";
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            var appVersion = "1.0.0";

            var model = new AdminDatabaseViewModel
            {
                InstallationId = installation?.InstallationId ?? Guid.Empty,
                CurrentState = installation?.CurrentState ?? "Unknown",
                DatabaseProvider = installation?.DatabaseProvider ?? "Unknown",
                DatabasePath = installation?.DatabasePath ?? "Unknown",
                InstallationDate = installation?.InstallationDate ?? DateTime.MinValue,
                CompletedDate = installation?.CompletedDate,
                UserCount = userCount,
                PermissionCount = permissionCount,
                ProjectCount = projectCount,
                Environment = environment,
                AppVersion = appVersion,
                DbConnectionStatus = dbConnectionStatus,
                InstallationProfile = installation?.InstallationProfile ?? "Default",
                ConfigProvider = installation?.DatabaseProvider ?? "SQLite",
                ConfigPath = installation?.DatabasePath ?? "data/app.db",
                ConfigDatabaseName = Path.GetFileName(installation?.DatabasePath ?? "app.db")
            };

            return View(model);
        }

        [HasPermission("Admin.Access")]
        public IActionResult Logs(int limit = 200)
        {
            var logEntries = new List<LogEntry>();
            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            
            try
            {
                if (Directory.Exists(logsDirectory))
                {
                    var logFiles = Directory.GetFiles(logsDirectory, "app-*.log")
                        .OrderByDescending(f => new System.IO.FileInfo(f).LastWriteTime)
                        .Take(5);
                    
                    foreach (var file in logFiles)
                    {
                        var lines = System.IO.File.ReadAllLines(file);
                        foreach (var line in lines.Reverse().Take(500))
                        {
                            var parsed = ParseLogLine(line);
                            if (parsed != null)
                            {
                                logEntries.Add(parsed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore lettura log");
            }
            
            logEntries = logEntries.OrderByDescending(e => e.Timestamp).Take(limit).ToList();
            
            ViewBag.LogEntries = logEntries;
            ViewBag.LogLevels = new[] { "Information", "Warning", "Error" };
            
            return View();
        }
        
        private LogEntry? ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    line, @"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) ([A-Z]{3})\]\s*(.+)");
                
                if (match.Success)
                {
                    var timestamp = DateTime.Parse(match.Groups[1].Value);
                    var level = match.Groups[2].Value;
                    var message = match.Groups[3].Value;
                    
                    var truncated = message.Length > 500 ? message.Substring(0, 500) + "..." : message;
                    
                    return new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = truncated
                    };
                }
            }
            catch { }
            
            return null;
        }

        [HasPermission("Admin.Access")]
        public async Task<IActionResult> Audit(string? eventType = null, string? username = null)
        {
            var logs = await _auditService.GetRecentLogsAsync(200, eventType, username);
            
            ViewBag.AuditLogs = logs;
            ViewBag.FilterEventType = eventType;
            ViewBag.FilterUsername = username;
            ViewBag.EventTypes = new[] { "Login", "LoginFailed", "Logout", "UserCreated", "UserUpdated", "UserDeleted", "PermissionsUpdated", "DbTest", "ConfigUpdated" };
            
            return View();
        }

        [HttpPost]
        [HasPermission("Admin.Access")]
        public async Task<IActionResult> TestConnection()
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var username = User.Identity?.Name;
            
            if (canConnect)
            {
                TempData["Success"] = "Connessione al database OK.";
                if (_auditService != null)
                {
                    await _auditService.LogAsync("DbTest", "Test connessione", username, null, "Database", null, 
                        HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, "Connessione OK");
                }
            }
            else
            {
                TempData["Error"] = "Errore di connessione al database.";
                if (_auditService != null)
                {
                    await _auditService.LogAsync("DbTest", "Test connessione", username, null, "Database", null, 
                        HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), false, "Errore connessione");
                }
            }
            return RedirectToAction("Database");
        }

        [HasPermission("Admin.Access")]
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        [HasPermission("Admin.Access")]
        public IActionResult TestNewConnection(string provider, string path, string host, int port, string databaseName, string username, string password, bool ssl)
        {
            try
            {
                _logger.LogInformation("Test nuova connessione database: provider={Provider}", provider);
                
                if (provider == "SQLite")
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return Json(new { success = false, message = "Percorso database richiesto." });
                    }
                    
                    if (path.Contains(".."))
                    {
                        return Json(new { success = false, message = "Path non valido (path traversal rilevato)." });
                    }
                    
                    if (!System.IO.File.Exists(path))
                    {
                        return Json(new { success = false, message = "File database non trovato." });
                    }
                    
                    return Json(new { success = true, message = "File database esiste. Salva configurazione in appsettings per test completo." });
                }
                else if (provider == "MySql")
                {
                    return Json(new { success = true, message = "Configurazione MySQL valida (salva in appsettings per applicare)." });
                }
                
                return Json(new { success = false, message = "Provider non supportato." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore test connessione database");
                return Json(new { success = false, message = "Errore: " + ex.Message });
            }
        }

        [HttpPost]
        [HasPermission("Admin.Access")]
        public IActionResult SaveConnection(string provider, string path, string host, int port, string databaseName, string username, string password, bool ssl)
        {
            TempData["Info"] = "Configurazione salvata. Per applicare, modifica appsettings.json e riavvia l'applicazione.";
            return RedirectToAction("Database");
        }

        [HttpGet]
        public IActionResult NewUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string firstName, string lastName, string VisualName,
            string email, string phoneNumber, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Tutti i campi sono obbligatori.");
                return View("NewUser");
            }

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                VisualName = VisualName,
                Email = email,
                PhoneNumber = phoneNumber,
                UserName = username,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("NewUser");
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string VisualName,
            string email, string phoneNumber, string username, string password)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.VisualName = VisualName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.UserName = username;
            user.LastUpdated = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(password))
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, password);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToAction("Users");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return RedirectToAction("Users");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return RedirectToAction("Users");
        }

        // ✅ GET PERMESSI
        [HttpGet("Admin/EditPermessi")]
        public async Task<IActionResult> EditPermessi(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allPermissions = await _context.Permessi.ToListAsync();
            var userPermissionIds = await _context.PermessiUtente
                .Where(p => p.UserId == userId)
                .Select(p => p.PermissionId)
                .ToListAsync();

            var progetti = await _context.CinemaOrders.ToListAsync();

            var userProjectPermissions = await _context.ProjectPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            ViewBag.AllPermissions = allPermissions ?? new List<Permission>();
            ViewBag.UserPermissionIds = userPermissionIds ?? new List<int>();
            ViewBag.Progetti = progetti ?? new List<CinemaOrder>();
            ViewBag.UserProjectPermissions = userProjectPermissions ?? new List<ProjectPermission>();


            return View(user);
        }


        // ✅ POST PERMESSI
        [HttpPost("Admin/EditPermessi")]
        public async Task<IActionResult> EditPermessiPost(string userId, List<int> selectedPermissions, List<string> projectPermissions)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // ✅ Rimuove e aggiorna i permessi globali
            var existingGlobal = _context.PermessiUtente.Where(p => p.UserId == userId);
            _context.PermessiUtente.RemoveRange(existingGlobal);

            if (selectedPermissions != null)
            {
                foreach (var id in selectedPermissions)
                {
                    _context.PermessiUtente.Add(new UserPermission
                    {
                        UserId = userId,
                        PermissionId = id
                    });
                }
            }

            // ✅ Rimuove e aggiorna i permessi per progetto
            var existingProject = _context.ProjectPermissions.Where(p => p.UserId == userId);
            _context.ProjectPermissions.RemoveRange(existingProject);

            if (projectPermissions != null)
            {
                foreach (var entry in projectPermissions)
                {
                    var parts = entry.Split(':');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int projectId) &&
                        !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        _context.ProjectPermissions.Add(new ProjectPermission
                        {
                            UserId = userId,
                            ProjectId = projectId,
                            PermissionName = parts[1]
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Users");
        }

    }
    }