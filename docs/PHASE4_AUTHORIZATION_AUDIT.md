# Phase 4 Authorization & Permission Audit Report

**Data:** 26 Aprile 2026  
**Stato Audit:** Completato

---

## 1. Controller/Action Pubblici - Panoramica

| Controller | Azione | Protezione | Tipo | Note |
|------------|-------|-----------|------|------|
| **AccountController** | | | | |
| | Login (GET) | [AllowAnonymous] | MVC | OK - Login page pubblico |
| | Login (POST) | [AllowAnonymous] | MVC | OK - Autenticazione |
| | AccessDenied | [AllowAnonymous] | MVC | OK - Pagina errore |
| | Logout | - | POST | Richiede auth cookie |
| | Profile | [HasPermission("Account.Profile")] | MVC | ✅ Protetto |
| | UpdateProfile | [HasPermission("Account.UpdateProfile")] | MVC | ✅ Protetto |
| | ChangePassword | [HasPermission("Account.ChangePassword")] | MVC | ✅ Protetto |
| **HomeController** | | | | |
| | Index | Nessuno (usa IsAuthenticated) | MVC | Semi-pubblico |
| | Privacy | [HasPermission("Home.Privacy")] | MVC | ✅ Protetto |
| **OrderController** | | | | |
| | Index | [HasPermission("Ordini.View")] | MVC | ✅ Protetto |
| | Create | [HasPermission("Ordini.Create")] | MVC | ✅ Protetto |
| | Edit | [HasPermission("Ordini.Edit")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("Ordini.Delete")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Ordini.View")] | MVC | ✅ Protetto |
| | **GetOrderStates** | **[Authorize]** | API | ✅ Protetto |
| | **GetOrders** | **[Authorize]** | API | ✅ Protetto |
| | **Tracking** | **[AllowAnonymous]** | MVC | OK - Tracking pubblico |
| | Tracking (POST) | [AllowAnonymous] | MVC | OK - Ricerca tracking |
| **CinemaController** | | | |
| | Index | [HasPermission("Progetti.View")] | MVC | ✅ Protetto |
| | Dashboard | [HasPermission("Progetti.Dashboard")] | MVC | ✅ Protetto |
| | Create | [HasPermission("Progetti.Create")] | MVC | ✅ Protetto |
| | Edit | [HasPermission("Progetti.Edit")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("Progetti.Delete")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Progetti.View")] | MVC | ✅ Protetto |
| | **GetCinemaOrdersStates** | **[Authorize]** | API | ✅ Protetto |
| | **GetCinemaOrders** | **[Authorize]** | API | ✅ Protetto |
| **ODGController** | | | |
| | Index | [HasPermission("ODG.View")] | MVC | ✅ Protetto |
| | Create | [HasPermission("ODG.Create")] | MVC | ✅ Protetto |
| | Edit | [HasPermission("ODG.Edit")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("ODG.Delete")] | MVC | ✅ Protetto |
| | ExportPDF | [HasPermission("ODG.Export")] | MVC | ✅ Protetto |
| **TroupeCastContactsController** | | | |
| | Index | [HasPermission("Contatti.View")] | MVC | ✅ Protetto |
| | Create | [HasPermission("Contatti.Create")] | MVC | ✅ Protetto |
| | Edit | [HasPermission("Contatti.Edit")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Contatti.Details")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("Contatti.Delete")] | MVC | ✅ Protetto |
| | ExportPdf | [HasPermission("Contatti.Export")] | MVC | ✅ Protetto |
| **LocationController** | | | |
| | Index | [HasPermission("Location.View")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Location.Details")] | MVC | ✅ Protetto |
| | Create | [HasPermission("Location.Create")] | MVC | ✅ Protetto |
| | Edit | [HasPermission("Location.Edit")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("Location.Delete")] | MVC | ✅ Protetto |
| **FileManagerController** | | | |
| | Index | [HasPermission("File.FileRead")] | MVC | ✅ Protetto |
| | ViewFile | [HasPermission("File.FileRead")] | MVC | ✅ Protetto |
| | Upload | ? | MVC | DA VERIFICARE |
| | Delete | ? | MVC | DA VERIFICARE |
| | **GetDocument** | **[AllowAnonymous]** | API | ⚠️ OnlyOffice webhook |
| **RentalRequestUserController** | | | |
| | Index | [HasPermission("Rental.User.Index")] | MVC | ✅ Protetto |
| | Create | [HasPermission("Rental.User.Create")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Rental.User.Details")] | MVC | ✅ Protetto |
| | Edit | **NESSUNO** | MVC | ⚠️ Controllo manuale |
| | Delete | [HasPermission("Rental.User.Delete")] | MVC | ✅ Protetto |
| | ReportDamage | [HasPermission("Rental.User.ReportDamage")] | MVC | ✅ Protetto |
| | ExportPdf | [HasPermission("Rental.User.ExportPdf")] | MVC | ✅ Protetto |
| **RentalRequestAdminController** | | | |
| | Index | [HasPermission("Rental.Admin")] | Controller | ✅ Protetto |
| | Details | **NESSUNO** | MVC | ⚠️ Amministratore |
| | Approve | **NESSUNO** | MVC | ⚠️ Amministratore |
| | RejectWithReason | **NESSUNO** | MVC | ⚠️ Amministratore |
| | RejectWithoutReason | **NESSUNO** | MVC | ⚠️ Amministratore |
| | ConfirmDelivery | **NESSUNO** | MVC | ⚠️ Amministratore |
| | Close | **NESSUNO** | MVC | ⚠️ Amministratore |
| | Archive | **NESSUNO** | MVC | ⚠️ Amministratore |
| | DamageReports | **NESSUNO** | MVC | ⚠️ Amministratore |
| **ItemAdminController** | | | |
| | Index | [HasPermission("Rental.Admin")] | Controller | ✅ Protetto |
| | Create | [HasPermission("Rental.Admin")] | Controller | ✅ Protetto |
| | Edit | [HasPermission("Rental.Admin")] | Controller | ✅ Protetto |
| | Delete | [HasPermission("Rental.Admin")] | Controller | ✅ Protetto |
| **AdminController** | | | |
| | Users | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | NewUser | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | CreateUser | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | EditUser | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | DeleteUser | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | EditPermessi | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| | EditPermessiPost | [HasPermission("Admin.Access")] | Controller | ✅ Protetto |
| **CentroCostoController** | | | |
| | Index | [HasPermission("Finanze.View")] | MVC | ✅ Protetto |
| | CreateSpesa | [HasPermission("Finanze.Create")] | MVC | ✅ Protetto |
| | Details | [HasPermission("Finanze.Details")] | MVC | ✅ Protetto |
| | EditSpesa | [HasPermission("Finanze.Edit")] | MVC | ✅ Protetto |
| | DownloadScontrino | [HasPermission("Finanze.Download")] | MVC | ✅ Protetto |
| | Delete | [HasPermission("Finanze.Delete")] | MVC | ✅ Protetto |
| | Esporta | [HasPermission("Finanze.Export")] | MVC | ✅ Protetto |
| **PianoDiLavorazioneController** | | | |
| | Index | **[Authorize(Roles)]** | MVC | ⚠️ Ruoli RBAC |
| | Create | **[Authorize(Roles)]** | MVC | ⚠️ Ruoli RBAC |
| | Edit | **[Authorize(Roles)]** | MVC | ⚠️ Ruoli RBAC |
| | Delete | **[Authorize(Roles)]** | MVC | ⚠️ Ruoli RBAC |

---

## 2. Endpoint API Esposti

### 2.1 API PROTETTE ✅

| Endpoint | Metodo | Autenticazione | Note |
|----------|-------|---------------|------|
| `/api/orders/states` | GET | [Authorize] | ✅ Protetto correttamente |
| `/api/orders` | GET | [Authorize] | ✅ Protetto correttamente |
| `/api/cinemaorders/states` | GET | [Authorize] | ✅ Protetto correttamente |
| `/api/cinemaorders` | GET | [Authorize] | ✅ Protetto correttamente |

### 2.2 ENDPOINT PUBBLICI (intenzionali) ✅

| Endpoint | Metodo | Autenticazione | Note |
|----------|-------|---------------|------|
| `/Order/Tracking` | GET | [AllowAnonymous] | ✅ Tracking pubblico |
| `/Order/Tracking` | POST | [AllowAnonymous] | ✅ Ricerca ordine |
| `/Account/Login` | GET/POST | [AllowAnonymous] | ✅ Login page |
| `/Account/AccessDenied` | GET | [AllowAnonymous] | ✅ Pagina errore |
| **OnlyOffice Webhook** | GET | [AllowAnonymous] | ⚠️ Solo per integrazione |

---

## 3. Endpoint Sensibili NON Protetti - RISCHI IDENTIFICATI

### 3.1 CRITICAL - Nessuna Protezione

| Endpoint | Controller | Rischio | Severità |
|----------|-----------|--------|----------|
| **RentalRequestUserController.Edit (GET/POST)** | RentalRequestUserController:139,169 | Utente può modificare richieste di altri utenti - controllo manuale debole | **HIGH** |
| **RentalRequestAdminController (tutte le action)** | RentalRequestAdminController:21-122 | Action amministratore SENZA protezione - solo [HasPermission("Rental.Admin")] a livello di controller ma action singole non protette | **CRITICAL** |
| **PianoDiLavorazioneController** | PianoDiLavorazioneController:22-157 | Usa `[Authorize(Roles = "Admin, Manager, User")]` - mescola due sistemi di autorizzazione! | **MEDIUM** |

### 3.2 Issue di Coerenza

| Controller | Issue | Impatto |
|------------|-------|---------|
| **RentalRequestAdminController** | Controller ha `[HasPermission("Rental.Admin")]` ma action POST (Approve, Reject, Close, etc) non hanno attributi - dipendono dalla protezione di classe | MEDIUM - Se la protezione di classe fallisce, tutte le action sono esposte |
| **PianoDiLavorazioneController** | Usa Authorize(Roles) invece di HasPermission - mescola policy ASP.NET con permission custom | BASSO - Due sistemi diversi |

---

## 4. Rischi di Privilege Escalation

### 4.1 Permission Model Analysis

Il sistema usa due layer di autorizzazione:

1. **HasPermission Attribute** - Verifica permesso globale o per progetto
2. **PermissionService** - Implementa la logica di verifica

**Verificato:** `HasPermissionAttribute.cs` estrae correttamente `{xxxId}` dalla route/query/form prima di chiamare `PermissionService.HasPermissionAsync()`.

### 4.2 Potential Bypass Vectors

| Vector | Status | Note |
|--------|--------|------|
| **Parameter Tampering** | ✅ Protetto | HasPermission estrae ID da multiple sources |
| **Project Permission Escalation** | ✅ Protetto | PermissionService verifica permessi per progetto |
| **Role-based to Permission Bypass** | ⚠️ Rischio | PianoDiLavorazione usa Roles invece di HasPermission |

### 4.3 Action Admin Non Protette

**RentalRequestAdminController** (riga 42-122):
```csharp
[Route("rental/admin")]
[HasPermission("Rental.Admin")]  // Solo a livello controller!
public class RentalRequestAdminController : Controller
{
    // Queste action NON hanno [HasPermission] individuale:
    // Approve, RejectWithReason, RejectWithoutReason,
    // ConfirmDelivery, Close, Archive, DamageReports
}
```

**Rischio:** Se la protezione a livello controller failsafe, tutte le action amministrative sono accessibili a chi ha "Rental.Admin".

---

## 5. Verifica Permessi Progetto/Globali

### 5.1 Sistema Attuale

- **PermissionService** implementa:
  - Permessi globali: `PermessiUtente` table
  - Permessi per progetto: `ProjectPermissions` table

- **HasPermissionAttribute**:
  1. Estrae entityId da route/query/form
  2. Chiama `PermissionService.HasPermissionAsync(user, permission, entityId)`

### 5.2 Pozzo Permessi Definito

I permessi usati nel codice:

| Area | Permesso | Uso |
|------|----------|-----|
| Account | Account.Profile | ✅ |
| Account | Account.UpdateProfile | ✅ |
| Account | Account.ChangePassword | ✅ |
| Home | Home.Privacy | ✅ |
| Progetti | Progetti.View | ✅ |
| Progetti | Progetti.Dashboard | ✅ |
| Progetti | Progetti.Create | ✅ |
| Progetti | Progetti.Edit | ✅ |
| Progetti | Progetti.Delete | ✅ |
| ODG | ODG.View | ✅ |
| ODG | ODG.Create | ✅ |
| ODG | ODG.Edit | ✅ |
| ODG | ODG.Delete | ✅ |
| ODG | ODG.Export | ✅ |
| Contatti | Contatti.View/Create/Edit/Delete/Details/Export | ✅ |
| Location | Location.View/Details/Create/Edit/Delete | ✅ |
| File | File.FileRead | ✅ |
| Rental | Rental.User.Index | ✅ |
| Rental | Rental.User.Create | ✅ |
| Rental | Rental.User.Delete | ✅ |
| Rental | Rental.User.ReportDamage | ✅ |
| Rental | Rental.User.ExportPdf | ✅ |
| Rental | Rental.Admin | ✅ |
| Finanze | Finanze.View/Create/Details/Edit/Download/Delete/Export | ✅ |
| Admin | Admin.Access | ✅ |

---

## 6. Summary Matrix

| Category | Totale | Protetti | Non Protetti | Note |
|-----------|-------|---------|-------------|------|
| MVC Actions | 80+ | ~75 | ~5 | Vedere dettagli |
| API Endpoints | 4 | 4 | 0 | Tutti protetti |
| Controller-Level | 13 | 12 | 1 | RentalRequestAdminController parziale |

| Severity | Issue | Count |
|----------|-------|-------|
| **CRITICAL** | RentalRequestAdminController action non protette | 1 |
| **HIGH** | RentalRequestUserController.Edit senza HasPermission | 1 |
| **MEDIUM** | PianoDiLavorazioneController usa Roles invece di HasPermission | 1 |

---

## 7. Priorità Correzioni

### Phase 4.1 - URGENT (Fix prima possibile)

| # | Fix | Controller | Severity | Effort |
|---|-----|-----------|-----------|----------|--------|
| 1 | **AGGIUNGI [HasPermission] a RentalRequestAdminController action** | RentalRequestAdminController:21-122 | CRITICAL | 30m |
| 2 | **AGGIUNGI [HasPermission] a RentalRequestUserController.Edit** | RentalRequestUserController:139,169 | HIGH | 15m |
| 3 | **Correggi PianoDiLavorazioneController** - converti a HasPermission | PianoDiLavorazioneController | MEDIUM | 30m |

### Phase 4.2 - Next Sprint

| # | Fix | Note |
|---|-----|------|
| 4 | Aggiungi audit logging per permessi negati |
| 5 | Verifica FileManagerController action rimanenti |
| 6 | Test penetrazione permission bypass |

---

## 8. Patch Plan - Fase Successiva

### Step 1: Fix RentalRequestAdminController

```csharp
// Aggiungi a ogni action:
[HasPermission("Rental.Admin")]
public async Task<IActionResult> Approve(int id) { ... }

[HasPermission("Rental.Admin")]
public async Task<IActionResult> RejectWithReason(int id, string reason) { ... }

// etc per tutte le action
```

### Step 2: Fix RentalRequestUserController

```csharp
[HttpGet("edit/{id}")]
[HasPermission("Rental.User.Edit")]  // AGGIUNGERE
public async Task<IActionResult> Edit(int id) { ... }

[HttpPost("edit/{id}")]
[HasPermission("Rental.User.Edit")]  // AGGIUNGERE
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, ...) { ... }
```

### Step 3: Fix PianoDiLavorazioneController

```csharp
// Sostituisci [Authorize(Roles = "Admin, Manager, User")]
// con [HasPermission("PianoDiLavorazione.View")]
```

### Step 4: Validazione

- [ ] Run dotnet build - pass
- [ ] Test manuale authorization
- [ ] Verifica che permission denied funziona
- [ ] Verifica audit log

---

## 9. Conclusioni

L'audit ha rivelato che il sistema di autorizzazione è **generalmente solido** con:

✅ **Punti di forza:**
- HasPermission attribute correttamente implementato
- PermissionService estrae entity ID da multiple sources
- API endpoints principali protetti
- Solo login/tracking pubblici come previsto

⚠️ **Aree da correggere:**
- RentalRequestAdminController: action amministrative senza protezione esplicita
- RentalRequestUserController.Edit: action parzialmente protetta
- PianoDiLavorazioneController: mescola due sistemi (Roles + HasPermission)

**R Raccomandazione:** Implementare le fix in Phase 4.1 prima del deploy in produzione.

---

*Fine Report*