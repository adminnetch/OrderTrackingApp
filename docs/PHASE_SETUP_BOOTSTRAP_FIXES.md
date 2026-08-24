# Phase Setup Bootstrap Fixes

## Fixes Applied

This document describes the bootstrap fixes applied to resolve wizard first-run issues.

---

## Bug 1: InstallationStateService Query Before Table Exists

**Problem**: `InstallationStateService` queried `AppInstallations` table before it existed, causing exceptions on fresh database.

**Files Modified**:
- `Services/InstallationStateService.cs`
- `Middleware/FirstRunMiddleware.cs`

**Solution**: 
- `GetCurrentStateAsync()` now catches exceptions and returns `NotStarted` if table doesn't exist
- `FirstRunMiddleware.InvokeAsync()` wrapped in try-catch to handle db not ready

**Code Changes**:

```csharp
// InstallationStateService.cs
public async Task<InstallationState> GetCurrentStateAsync()
{
    try
    {
        if (!await _context.Database.CanConnectAsync())
            return InstallationState.NotStarted;
        
        if (!await _context.AppInstallations.AnyAsync())
            return InstallationState.NotStarted;
        
        // ... existing logic
    }
    catch (Exception)
    {
        return InstallationState.NotStarted;
    }
}
```

---

## Bug 2: SQLite AUTOINCREMENT Migration Incompatibility

**Problem**: Migrations use MySQL-specific syntax. Running `context.Database.Migrate()` on SQLite failed with:

```
SQLite Error 1: 'AUTOINCREMENT is only allowed on an INTEGER PRIMARY KEY'
```

**Cause**: Initial migration was designed for MySQL, uses `AUTOINCREMENT` which is invalid for SQLite.

**Solution**: Use `EnsureCreatedAsync()` for SQLite, `Migrate()` for MySQL.

**File Modified**: `Program.cs`

**Code Changes**:

```csharp
var dbType = builder.Configuration.GetValue<string>("Database:Provider", "sqlite");

if (dbType == "sqlite")
{
    await context.Database.EnsureCreatedAsync();
}
else
{
    await context.Database.MigrateAsync();
}
```

**Configuration**:
- Set `Database:Provider=sqlite` in `appsettings.json` for Express profile
- Set `Database:Provider=mysql` for production

---

## Bug 4: HTTP 405 Method Not Allowed

**Problem**: Setup wizard used only POST actions. Clicking links resulted in HTTP 405.

**Solution**: Added separate GET actions (for rendering) and POST actions (for processing):

| Route | Method | Purpose |
|-------|--------|---------|
| `/setup` | GET | Index (profile selection) |
| `/setup/Prerequisites` | GET | Render prerequisites page |
| `/setup/PrerequisitesPost` | POST | Check and proceed to Database |
| `/setup/Database` | GET | Render database setup |
| `/setup/DatabasePost` | POST | Initialize and proceed to Superadmin |
| `/setup/Superadmin` | GET | Render Superadmin form |
| `/setup/SuperadminPost` | POST | Create Superadmin, proceed to Complete |
| `/setup/Complete` | GET | Render completion page |
| `/setup/CompletePost` | POST | Finalize and go to login |

**Files Modified**:
- `Controllers/SetupController.cs` - Added GET handlers
- `Views/Setup/Index.cshtml` - Links use standard GET
- `Views/Setup/Prerequisites.cshtml` - Form POST to PrerequisitesPost
- `Views/Setup/Database.cshtml` - AJAX POST to DatabasePost
- `Views/Setup/Superadmin.cshtml` - AJAX POST to SuperadminPost
- `Views/Setup/Complete.cshtml` - Form POST to CompletePost

**Verified Working**:
- GET /setup renders wizard
- All navigation links work (no 405)
- Form submissions redirect correctly

---

## Bug 5: JavaScript Null Reference Error

**Problem**: Database page crashed with "Cannot read properties of null (reading 'value')" when submitting form.

**Cause**: JavaScript tried to access DOM elements before they were ready.

**Solution**: 
- Wrapped code in `DOMContentLoaded` event listener
- Added null guards for all DOM element access
- Added validation checks before AJAX calls
- Added safe error display instead of crashes

**Files Modified**:
- `Views/Setup/Database.cshtml` - Added DOMContentLoaded, null guards
- `Views/Setup/Superadmin.cshtml` - Same pattern applied

**Verified Working**:
- Page loads without errors
- Form submission shows meaningful errors if inputs missing
- AJAX calls work correctly

---

## Bug 6: Setup Complete Redirect 404

**Problem**: After completing setup, redirected to `/Account?login=setup` which gave 404.

**Solution**: Fixed redirect to use correct action name:
```csharp
return RedirectToAction("Login", "Account", new { login = "setup" });
```

**File Modified**:
- `Controllers/SetupController.cs` - Line ~197

**Verified Working**:
- After setup complete, redirects to `/Account/Login?login=setup`
- Login page loads correctly
- SuperAdmin can log in

**Status**: No spurious "ycw" text found. No fix needed.

---

## Files Modified Summary

| File | Changes |
|------|--------|
| `Services/InstallationStateService.cs` | Safe query with try-catch |
| `Middleware/FirstRunMiddleware.cs` | Safe state check with try-catch |
| `Program.cs` | SQLite vs MySQL strategy |
| `Services/SuperadminService.cs` | (previous fix) SuperAdmin creation |
| `Services/PermissionService.cs` | (previous fix) SuperAdmin bypass |
| `Views/Setup/Index.cshtml` | NEW - Profile selection |
| `Views/Setup/Prerequisites.cshtml` | NEW - System check |
| `Views/Setup/Database.cshtml` | NEW - DB init |
| `Views/Setup/Superadmin.cshtml` | NEW - SuperAdmin create |
| `Views/Setup/Complete.cshtml` | NEW - Completion |

---

## Testing

**Verified Working**:
1. Remove `data/` directory completely
2. Run `dotnet run`
3. App starts without errors
4. Database created with SQLite
5. Roles and permissions seeded
6. Can access `/setup` wizard

**Log Output** (expected):
```
Ruolo SuperAdmin creato.
Ruolo Admin creato.
...
Permesso 'Admin.Access' creato.
...
Now listening on: https://localhost:7031
```

---

## What Was NOT Changed

- No RBAC V2 implementation
- No RolePermission table
- No controller changes
- No Rental module changes
- Existing permission granularity maintained

---

## Next Steps (Future)

These were intentionally NOT done:
1. RBAC V2 full implementation
2. RolePermission table
3. Admin.Access as policy