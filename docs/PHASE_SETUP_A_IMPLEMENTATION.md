# Phase A Implementation - Setup Wizard MVP

## OrderTrackingApp - Phase A Implementation Guide

---

## 1. Panoramica

Questo documento descrive l'implementazione della **Phase A (MVP Core)** del Setup Wizard secondo `FIRST_RUN_SETUP_ARCHITECTURE.md`.

### Scope Implementativo Phase A

| Componente | Stato | Note |
|------------|-------|------|
| First Run Detection | ✅ Implementare | Middleware |
| Redirect to /setup | ✅ Implementare | Se non inizializzato |
| SetupController | ✅ Implementare | Wizard skeleton |
| Express Profile ONLY | ✅ Implementare | 3-step wizard |
| SQLite Provider | ✅ Implementare | Phase A only |
| Auto DB Creation | ✅ Implementare | SQLite automatico |
| Schema Bootstrap | ✅ Implementare | EF Migrations |
| Superadmin Creation | ✅ Implementare | Step 3 |
| Mark Complete | ✅ Implementare | State transition |
| Redirect to Login | ✅ Implementare | Final step |
| MariaDB/MySQL/PG/SS | ❌ Non implementare | Phase B/C |
| Standard/Advanced | ❌ Non implementare | Future phases |
| SignalR Live Log | ❌ Stub only | Future phase |

---

## 2. Struttura File da Creare/Modificare

```
Phase A - File da creare/modificare:

NEW FILES:
├── Models/AppInstallation.cs              ← NEW: Installation state entity
├── Services/InstallationStateService.cs ← NEW: State management
├── Services/ISetupService.cs          ← NEW: Interface definitions
├── Services/SqliteProvider.cs       ← NEW: SQLite DB provider
├── Services/SuperadminService.cs     ← NEW: Superadmin creation
├── Middleware/FirstRunMiddleware.cs ← NEW: Detection + redirect
├── Controllers/SetupController.cs  ← NEW: Wizard controller
└── Views/Setup/                  ← NEW: Wizard views
    ├── Index.cshtml
    ├── Prerequisites.cshtml
    ├── Database.cshtml
    ├── Superadmin.cshtml
    └── Complete.cshtml

MODIFY:
├── Program.cs                    ← Add middleware + services
├── OrderTrackingApp.csproj       ← Add EF SQLite package
└── Models/AppDbContext.cs   ← Add InstallationState
```

---

## 3. Entity: AppInstallation

```csharp
// Models/AppInstallation.cs
using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models;

public class AppInstallation
{
    [Key]
    public Guid InstallationId { get; set; } = Guid.NewGuid();
    
    public DateTime InstallationDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedDate { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CurrentState { get; set; } = InstallationState.NotStarted.ToString();
    
    public string? PreviousState { get; set; }
    
    [MaxLength(20)]
    public string DatabaseProvider { get; set; } = "sqlite";
    
    [MaxLength(255)]
    public string? DatabasePath { get; set; }
    
    public bool IsLocked { get; set; }
    
    public DateTime? LockedAt { get; set; }
    
    public Guid? LockedBySessionId { get; set; }
    
    public string? LastErrorMessage { get; set; }
    
    public string? InstallationProfile { get; set; } = "express";
}

public enum InstallationState
{
    NotStarted,
    PrerequisitesValidated,
    DatabaseConfigured,
    DatabaseInitialized,
    SuperadminCreated,
    Complete,
    Failed
}
```

---

## 4. Services: InstallationStateService

```csharp
// Services/IInstallationStateService.cs
using OrderTrackingApp.Models;

public interface IInstallationStateService
{
    Task<InstallationState> GetCurrentStateAsync();
    Task UpdateStateAsync(InstallationState newState);
    Task MarkCompleteAsync();
    Task<bool> CanAccessAsync(string sessionId);
    Task<string?> GetSetupSessionTokenAsync();
    Task<bool> IsFirstRunRequiredAsync();
}
```

```csharp
// Services/InstallationStateService.cs
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class InstallationStateService : IInstallationStateService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InstallationStateService> _logger;

    public InstallationStateService(AppDbContext context, ILogger<InstallationStateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InstallationState> GetCurrentStateAsync()
    {
        var installation = await GetOrCreateInstallationAsync();
        
        if (Enum.TryParse<InstallationState>(installation.CurrentState, out var state))
        {
            return state;
        }
        
        return InstallationState.NotStarted;
    }

    public async Task UpdateStateAsync(InstallationState newState)
    {
        var installation = await GetOrCreateInstallationAsync();
        
        installation.PreviousState = installation.CurrentState;
        installation.CurrentState = newState.ToString();
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Installation state changed: {Previous} -> {New}",
            installation.PreviousState, newState);
    }

    public async Task MarkCompleteAsync()
    {
        var installation = await GetOrCreateInstallationAsync();
        
        installation.CurrentState = InstallationState.Complete.ToString();
        installation.CompletedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Installation completed at {Date}", installation.CompletedDate);
    }

    public async Task<bool> CanAccessAsync(string sessionId)
    {
        var installation = await GetOrCreateInstallationAsync();
        
        if (!installation.IsLocked)
            return true;
        
        if (installation.LockedBySessionId.HasValue && 
            installation.LockedBySessionId == Guid.Parse(sessionId))
            return true;
        
        if (installation.LockedAt.HasValue)
        {
            var lockDuration = DateTime.UtcNow - installation.LockedAt.Value;
            if (lockDuration.TotalMinutes > 30)
            {
                installation.IsLocked = false;
                installation.LockedAt = null;
                installation.LockedBySessionId = null;
                await _context.SaveChangesAsync();
                return true;
            }
        }
        
        return false;
    }

    public async Task<string?> GetSetupSessionTokenAsync()
    {
        var installation = await GetOrCreateInstallationAsync();
        return installation.InstallationId.ToString();
    }

    public async Task<bool> IsFirstRunRequiredAsync()
    {
        var state = await GetCurrentStateAsync();
        return state != InstallationState.Complete;
    }

    private async Task<AppInstallation> GetOrCreateInstallationAsync()
    {
        var installation = await _context.AppInstallations.FirstOrDefaultAsync();
        
        if (installation == null)
        {
            installation = new AppInstallation();
            _context.AppInstallations.Add(installation);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("New installation created: {Id}", installation.InstallationId);
        }
        
        return installation;
    }
}
```

---

## 5. SQLite Provider (Phase A Only)

```csharp
// Services/ISqliteDatabaseProvider.cs
using OrderTrackingApp.Models;

public interface ISqliteDatabaseProvider
{
    string ProviderName => "SQLite";
    Task<bool> InitializeDatabaseAsync(string databasePath);
    string BuildConnectionString(string databasePath);
    Task<bool> TestConnectionAsync(string databasePath);
    IEnumerable<string> GetSchemaScripts();
}
```

```csharp
// Services/SqliteDatabaseProvider.cs
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class SqliteDatabaseProvider : ISqliteDatabaseProvider
{
    private readonly ILogger<SqliteDatabaseProvider> _logger;

    public SqliteDatabaseProvider(ILogger<SqliteDatabaseProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "SQLite";

    public async Task<bool> InitializeDatabaseAsync(string databasePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created database directory: {Dir}", directory);
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}");

            using var context = new AppDbContext(optionsBuilder.Options);
            await context.Database.EnsureCreatedAsync();

            _logger.LogInformation("SQLite database initialized: {Path}", databasePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite database: {Path}", databasePath);
            return false;
        }
    }

    public string BuildConnectionString(string databasePath)
    {
        return $"Data Source={databasePath}";
    }

    public async Task<bool> TestConnectionAsync(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath))
            {
                _logger.LogWarning("Database file not found: {Path}", databasePath);
                return false;
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}");

            using var context = new AppDbContext(optionsBuilder.Options);
            await context.Database.CanConnectAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite connection test failed: {Path}", databasePath);
            return false;
        }
    }

    public IEnumerable<string> GetSchemaScripts()
    {
        return new List<string>
        {
            "-- Users table created by EF Core",
            "-- Migrations handle schema"
        };
    }
}
```

---

## 6. Superadmin Service

```csharp
// Services/ISuperadminService.cs
using OrderTrackingApp.Models;

public interface ISuperadminService
{
    Task<(bool Success, List<string> Errors)> CreateSuperadminAsync(string username, string email, string password);
}
```

```csharp
// Services/SuperadminService.cs
using Microsoft.AspNetCore.Identity;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class SuperadminService : ISuperadminService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<SuperadminService> _logger;

    public SuperadminService(UserManager<User> userManager, ILogger<SuperadminService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool Success, List<string> Errors)> CreateSuperadminAsync(
        string username, string email, string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
        {
            errors.Add("Username must be between 3 and 50 characters");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            errors.Add("Valid email is required");
        }

        var passwordValidation = ValidatePassword(password);
        errors.AddRange(passwordValidation);

        if (errors.Count > 0)
        {
            return (false, errors);
        }

        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            errors.Add("Username already exists");
            return (false, errors);
        }

        var existingEmail = await _userManager.FindByEmailAsync(email);
        if (existingEmail != null)
        {
            errors.Add("Email already registered");
            return (false, errors);
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                errors.Add(error.Description);
            }
            return (false, errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Failed to assign Admin role to superadmin");
        }

        _logger.LogInformation("Superadmin created: {Username}", username);
        return (true, new List<string>());
    }

    private List<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return errors;
        }

        if (password.Length < 12)
            errors.Add("Password must be at least 12 characters");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        if (!password.Any(c => "!@#$%^&*".Contains(c)))
            errors.Add("Password must contain at least one special character (!@#$%^&*)");

        return errors;
    }
}
```

---

## 7. Middleware: First Run Detection

```csharp
// Middleware/FirstRunMiddleware.cs
using OrderTrackingApp.Services;

namespace OrderTrackingApp.Middleware;

public class FirstRunMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirstRunMiddleware> _logger;

    public FirstRunMiddleware(RequestDelegate next, ILogger<FirstRunMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IInstallationStateService stateService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var publicPaths = new[] { "/setup", "/account/login", "/account/register", "/js", "/css", "/lib", "/images" };
        if (publicPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        if (path == "/" || path.StartsWith("/home") || path.StartsWith("/account"))
        {
            var isFirstRunRequired = await stateService.IsFirstRunRequiredAsync();
            
            if (isFirstRunRequired)
            {
                _logger.LogInformation("First run required, redirecting to /setup");
                context.Response.Redirect("/setup");
                return;
            }
        }

        await _next(context);
    }
}

public static class FirstRunMiddlewareExtensions
{
    public static IApplicationBuilder UseFirstRunDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirstRunMiddleware>();
    }
}
```

---

## 8. Controller: SetupController

```csharp
// Controllers/SetupController.cs
using Microsoft.AspNetCore.Mvc;
using OrderTrackingApp.Models;
using OrderTrackingApp.Services;

namespace OrderTrackingApp.Controllers;

public class SetupController : Controller
{
    private readonly IInstallationStateService _stateService;
    private readonly ISqliteDatabaseProvider _sqliteProvider;
    private readonly ISuperadminService _superadminService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        IInstallationStateService stateService,
        ISqliteDatabaseProvider sqliteProvider,
        ISuperadminService superadminService,
        ILogger<SetupController> logger)
    {
        _stateService = stateService;
        _sqliteProvider = sqliteProvider;
        _superadminService = superadminService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var state = await _stateService.GetCurrentStateAsync();
        
        var model = new SetupWizardViewModel
        {
            CurrentState = state,
            CurrentStep = GetStepFromState(state),
            Profile = "express"
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Prerequisites()
    {
        var state = await _stateService.GetCurrentStateAsync();
        
        if (state != InstallationState.NotStarted && state != InstallationState.Failed)
        {
            return RedirectToAction(GetViewFromState(state));
        }

        var dotnetVersion = Environment.Version;
        
        var result = new PrerequisiteCheckViewModel
        {
            DotnetVersion = dotnetVersion.ToString(),
            IsDotnetValid = dotnetVersion.Major >= 6,
            AllChecksPassed = dotnetVersion.Major >= 6
        };

        if (result.AllChecksPassed)
        {
            await _stateService.UpdateStateAsync(InstallationState.PrerequisitesValidated);
        }

        return View(result);
    }

    [HttpPost]
    public async Task<IActionResult> Database([FromBody] DatabaseSetupViewModel model)
    {
        var state = await _stateService.GetCurrentStateAsync();
        
        if (state < InstallationState.PrerequisitesValidated)
        {
            return RedirectToAction("Prerequisites");
        }

        var dbPath = string.IsNullOrEmpty(model.DatabasePath) 
            ? Path.Combine(Directory.GetCurrentDirectory(), "data", "ordertracking.db")
            : model.DatabasePath;

        var success = await _sqliteProvider.InitializeDatabaseAsync(dbPath);
        
        if (!success)
        {
            ModelState.AddModelError("", "Failed to initialize database");
            return View(new DatabaseSetupViewModel());
        }

        await _stateService.UpdateStateAsync(InstallationState.DatabaseConfigured);

        return Json(new { success = true, databasePath = dbPath });
    }

    [HttpPost]
    public async Task<IActionResult> Superadmin([FromBody] SuperadminSetupViewModel model)
    {
        var state = await _stateService.GetCurrentStateAsync();
        
        if (state < InstallationState.DatabaseConfigured)
        {
            return RedirectToAction("Database");
        }

        var (success, errors) = await _superadminService.CreateSuperadminAsync(
            model.Username!, model.Email!, model.Password!);
        
        if (!success)
        {
            return Json(new { success = false, errors });
        }

        await _stateService.UpdateStateAsync(InstallationState.SuperadminCreated);

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Complete()
    {
        await _stateService.MarkCompleteAsync();
        
        _logger.LogInformation("Setup wizard completed successfully");
        
        return RedirectToAction("Index", "Account", new { area = "", login = "setup" });
    }

    private int GetStepFromState(InstallationState state)
    {
        return state switch
        {
            InstallationState.NotStarted => 1,
            InstallationState.PrerequisitesValidated => 2,
            InstallationState.DatabaseConfigured => 3,
            InstallationState.DatabaseInitialized => 3,
            InstallationState.SuperadminCreated => 4,
            InstallationState.Complete => 5,
            InstallationState.Failed => 1,
            _ => 1
        };
    }

    private string GetViewFromState(InstallationState state)
    {
        return state switch
        {
            InstallationState.NotStarted => "Prerequisites",
            InstallationState.PrerequisitesValidated => "Database",
            InstallationState.DatabaseConfigured => "Superadmin",
            InstallationState.DatabaseInitialized => "Superadmin",
            InstallationState.SuperadminCreated => "Complete",
            _ => "Index"
        };
    }
}

public class SetupWizardViewModel
{
    public InstallationState CurrentState { get; set; }
    public int CurrentStep { get; set; }
    public string Profile { get; set; } = "express";
}

public class PrerequisiteCheckViewModel
{
    public string DotnetVersion { get; set; } = "";
    public bool IsDotnetValid { get; set; }
    public bool AllChecksPassed { get; set; }
}

public class DatabaseSetupViewModel
{
    public string DatabasePath { get; set; } = "";
    public string Provider { get; set; } = "sqlite";
}

public class SuperadminSetupViewModel
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}
```

---

## 9. Program.cs Modifications

```csharp
// Program.cs - Add these modifications

// 1. Add before builder.Services.AddControllersWithViews()
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "ordertracking.db");
    var connectionString = $"Data Source={dbPath}";
    options.UseSqlite(connectionString);
});

// 2. Register Phase A services
builder.Services.AddScoped<IInstallationStateService, InstallationStateService>();
builder.Services.AddScoped<ISqliteDatabaseProvider, SqliteDatabaseProvider>();
builder.Services.AddScoped<ISuperadminService, SuperadminService>();

// 3. Add middleware BEFORE app.UseAuthorization()
app.UseFirstRunDetection();
```

---

## 10. View: Setup Index

```razor
@* Views/Setup/Index.cshtml *@
@model SetupWizardViewModel
@{
    ViewData["Title"] = "Setup Wizard";
}

<div class="setup-wizard">
    <h1>OrderTrackingApp Setup</h1>
    
    <div class="steps">
        <div class="step @(Model.CurrentStep >= 1 ? "active" : "")">
            <span class="step-number">1</span>
            <span class="step-label">Prerequisites</span>
        </div>
        <div class="step @(Model.CurrentStep >= 2 ? "active" : "")">
            <span class="step-number">2</span>
            <span class="step-label">Database</span>
        </div>
        <div class="step @(Model.CurrentStep >= 3 ? "active" : "")">
            <span class="step-number">3</span>
            <span class="step-label">Superadmin</span>
        </div>
    </div>
    
    <div class="profile-badge">
        <span class="badge">Express Profile</span>
        <small>Quick start mode (SQLite only)</small>
    </div>
    
    <form method="post" action="@Url.Action("Prerequisites")">
        <button type="submit" class="btn btn-primary">
            Start Setup
        </button>
    </form>
</div>
```

---

## 11. Acceptance Criteria

- [ ] Applicazione parte e redirect automatico a /setup se first run
- [ ] Step 1: Prerequisites check (.NET version)
- [ ] Step 2: SQLite database automatico creato
- [ ] Step 3: Superadmin creato con validazione password
- [ ] Redirect finale a /Account/Login con parametro ?login=setup
- [ ] Installazione marcata come Complete
- [ ] dotnet build passa senza errori

---

## 12. Testing Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Test flow
curl -s http://localhost:5000/ | head
curl -s http://localhost:5000/setup
```

---

*Implementation Version: 1.0*
*Last Updated: 2026-04-26*
*Author: Phase A Implementation*