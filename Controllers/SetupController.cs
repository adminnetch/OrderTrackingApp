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

    private async Task<bool> IsSetupCompletedAsync()
    {
        var state = await _stateService.GetCurrentStateAsync();
        return state == InstallationState.Complete;
    }

    public async Task<IActionResult> Index()
    {
        if (await IsSetupCompletedAsync())
        {
            return RedirectToAction("Index", "Home");
        }
        
        var state = await _stateService.GetCurrentStateAsync();
        
        var model = new SetupWizardViewModel
        {
            CurrentState = state,
            CurrentStep = GetStepFromState(state),
            Profile = "express"
        };
        
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Prerequisites()
    {
        if (await IsSetupCompletedAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        var state = await _stateService.GetCurrentStateAsync();
        
        if (state >= InstallationState.PrerequisitesValidated)
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
        
        return View(result);
    }

    [HttpPost]
    [Route("setup/prerequisitespost")]
    public async Task<IActionResult> PrerequisitesPost()
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
            return RedirectToAction("Database");
        }

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Database()
    {
        if (await IsSetupCompletedAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        var state = await _stateService.GetCurrentStateAsync();
        
        if (state >= InstallationState.DatabaseConfigured)
        {
            return RedirectToAction(GetViewFromState(state));
        }
        
        if (state < InstallationState.PrerequisitesValidated)
        {
            return RedirectToAction("Prerequisites");
        }
        
        return View(new DatabaseSetupViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> DatabasePost([FromBody] DatabaseSetupViewModel model)
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
            return Json(new { success = false, errors = new[] { "Failed to initialize database" } });
        }

        await _stateService.UpdateStateAsync(InstallationState.DatabaseConfigured);

        return Json(new { success = true, databasePath = dbPath, redirectUrl = Url.Action("Superadmin", "Setup") });
    }

    [HttpGet]
    public async Task<IActionResult> Superadmin()
    {
        if (await IsSetupCompletedAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        var state = await _stateService.GetCurrentStateAsync();
        
        if (state >= InstallationState.SuperadminCreated)
        {
            return RedirectToAction("Complete");
        }
        
        if (state < InstallationState.DatabaseConfigured)
        {
            return RedirectToAction("Database");
        }
        
        return View(new SuperadminSetupViewModel());
    }

    [HttpPost]
    [Route("setup/superadminpost")]
    public async Task<IActionResult> SuperadminPost([FromBody] SuperadminSetupViewModel model)
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

        return Json(new { success = true, redirectUrl = Url.Action("Complete", "Setup") });
    }

    [HttpGet]
    public async Task<IActionResult> Complete()
    {
        if (await IsSetupCompletedAsync())
        {
            return RedirectToAction("Index", "Home");
        }

        var state = await _stateService.GetCurrentStateAsync();
        
        if (state < InstallationState.SuperadminCreated)
        {
            return RedirectToAction("Superadmin");
        }
        
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CompletePost()
    {
        await _stateService.MarkCompleteAsync();
        
        _logger.LogInformation("Setup wizard completed successfully");
        
        return RedirectToAction("Login", "Account", new { login = "setup" });
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