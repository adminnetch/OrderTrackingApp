# Phase Admin Dashboard and Setup Lock

**Data:** 26 Aprile 2026  
**Stato:** Completato

---

## 1. Obiettivi

1. Fix login già autenticato
2. Bloccare setup dopo installazione completata
3. Creare dashboard Admin
4. Creare pagina stato database
5. Fix bug EditUser su errore

---

## 2. File Modificati

| File | Modifiche |
|------|-----------|
| `Controllers/AccountController.cs` | Redirect utente già autenticato da /login a /admin |
| `Controllers/SetupController.cs` | Blocca accesso se setup completato |
| `Controllers/AdminController.cs` | Aggiunto Index(), Database(), fix EditUser/DeleteUser |
| `Views/Admin/Index.cshtml` | Nuova view dashboard |
| `Views/Admin/Database.cshtml` | Nuova view database info |
| `Models/AdminDatabaseViewModel.cs` | Nuovo model |

---

## 3. Implementazione

### 3.1 Fix Login Già Autenticato

Se utente con ruolo SuperAdmin o Admin visita `/account/login`, viene reindirizzato a `/admin`.
Altrimenti viene reindirizzato a `/`.

```csharp
[HttpGet]
[AllowAnonymous]
public IActionResult Login(string? returnUrl = null)
{
    if (User.Identity?.IsAuthenticated == true)
    {
        if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }
        return RedirectToAction("Index", "Home");
    }
    // ...
}
```

### 3.2 Bloccare Setup Dopo Installazione

Se `AppInstallation.CurrentState == Complete`, tutti i seguenti URL reindirizzano a `/`:
- `/setup`
- `/setup/prerequisites`
- `/setup/database`
- `/setup/superadmin`
- `/setup/complete`

```csharp
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
    // ...
}
```

### 3.3 Dashboard Admin

La view `/admin` mostra 3 cards:
- Gestione Utenti -> `/admin/Users`
- Gestione Permessi -> `/admin/EditPermessi`
- Stato Database -> `/admin/Database`

### 3.4 Pagina Database Info

Mostra informazioni da `AppInstallation` e statistiche:
- InstallationId
- CurrentState
- DatabaseProvider
- DatabasePath
- InstallationDate
- CompletedDate
- UserCount
- PermissionCount
- ProjectCount

### 3.5 Fix EditUser

**Problema:** Su errori, ritornava `View("Users", ...)` con model incoerente.
**Correzione:** Ora ritorna `RedirectToAction("Users")` per mostrare errors con TempData.

---

## 4. Nuovi File

- `Views/Admin/Index.cshtml`
- `Views/Admin/Database.cshtml`
- `Models/AdminDatabaseViewModel.cs`

---

## 5. Route

| URL | Controller | Action | Descrizione |
|-----|-----------|--------|-----------|
| `/admin` | AdminController | Index | Dashboard admin |
| `/Admin/Users` | AdminController | Users | Lista utenti |
| `/Admin/EditPermessi` | AdminController | EditPermessi | Gestione permessi |
| `/Admin/Database` | AdminController | Database | Info database |

---

## 6. Build

```bash
dotnet build  # ✅ Success
```

---

## 7. Note

- RBAC V2 NON implementato
- RolePermission NON creato
- Gestione permessi NON rifatta
- Gestione utenti NON rifatta
- Rental NON toccato