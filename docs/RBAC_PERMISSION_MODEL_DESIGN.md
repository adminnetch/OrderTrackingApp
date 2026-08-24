# RBAC + Permission Model Design

## 1. Analisi del Contesto Attuale

### 1.1 Situazione del Codice

**Program.cs** (linee 145-260):
- Crea 6 ruoli Identity: `Admin`, `Manager`, `User`, `Progetti`, `Ordini`, `Esterno`
- Crea ~50 permessi granulari organizzati per modulo:
  - `ODG.View/Create/Edit/Delete/Export`
  - `Finanze.*` (6 permessi)
  - `Piani.*` (4 permessi)
  - `Progetti.*` (5 permessi)
  - `Contatti.*` (6 permessi)
  - `File.*` (8 permessi - FileRead, FileUpload, FileRename, Download, FileDelete, Folder.Create, Folder.Delete, Folder.Rename)
  - `Ordini.*` (5 permessi)
  - `Location.*` (5 permessi)
  - `Admin.Access` (1 permesso)
  - `Home.Index.*` (8 permessi - dashboard specifiche)
  - `Account.*` (3 permessi)
  - `Rental.User.*` (6 permessi)
  - `Rental.Admin` (1 permesso)

**PermissionService.cs** (linee 22-60):
- Verifica permesso globale in `PermessiUtente` (tabella UserPermission)
- Verifica permesso a livello di progetto in `ProjectPermissions`
- NON implementa bypass per SuperAdmin

**HasPermissionAttribute.cs** (linee 23-90):
- Estrae `entityId` da route/query/form
- Delega a `PermissionService.HasPermissionAsync()`

**AdminController.cs** (linee 12, 120-197):
- Richiede `[HasPermission("Admin.Access")]`
- Gestisce assegnazione permessi globali (UserPermission)
- Gestisce assegnazione permessi per progetto (ProjectPermission)

**SuperadminService.cs** (linee 73-81):
- Crea utente come `Admin` role
- NON esiste ruolo SuperAdmin
- Il primo utente viene creato con ruolo `Admin`

### 1.2 Gap Rilevati

| Gap | Impatto |
|-----|---------|
| Nessun ruolo SuperAdmin | Non esiste modo di garantire accesso illimitato |
| SuperadminService assegna ruolo Admin | Confusione tra SuperAdmin e Admin funzionale |
| PermissionService non fa bypass | SuperAdmin non può accedere a tutto senza permessi espliciti |
| Ruolo Admin ha solo ruolo Identity | Non ha automaticamente tutti i permessi granulari |

---

## 2. Definizioni formali

### 2.1 Gerarchia dei concetti

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENTITÀ IN SISTEMA                        │
├─────────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │   IDENTITY ROLE │    │  PERMESSO         │                │
│  │  (ruolo Identity)│    │  GRANULARE        │                │
│  │                 │    │  (Permission)     │                │
│  │ - Admin         │    │                  │                │
│  │ - Manager       │    │ - Admin.Access   │                │
│  │ - User          │    │ - Progetti.View   │                │
│  │ - Progetti      │    │ - Ordini.Edit    │                │
│  │ - Ordini        │    │ - File.Upload    │                │
│  │ - Esterno      │    │ - ...            │                │
│  │ - SuperAdmin   │    │                  │                │
│  └────────┬────────┘    └────────┬────────┘                │
│           │                       │                         │
│           │     ASSEGNA            │                         │
│           └───────┬───────────────┘                         │
│                   │                                         │
│                   ▼                                         │
│         ┌─────────────────────┐                           │
│         │   UTENTE (User)      │                           │
│         │                      │                           │
│         │ - ha ruoli Identity  │                           │
│         │ - ha permessi globali│ ──► UserPermission       │
│         │ - ha permessi per    │ ──► ProjectPermission    │
│         │   progetto specifico  │                           │
│         └─────────────────────┘                           │
│                                                             │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Distinzione dei concetti

| Concetto | Tabella | Significato |
|----------|---------|-------------|
| **Identity Role** | `AspNetRoles` | Ruolo astrato di Identity (Admin, Manager, User, SuperAdmin...). Definisce la "classe" dell'utente. |
| **Permission** | `Permissions` | Enum di stringhe che rappresentano un'azione specifica nel sistema. Sono seedate in `Program.cs`. |
| **UserPermission** | `PermessiUtente` | Assegnazione diretta di un Permission a un utente. Valida per l'intero sistema. |
| **ProjectPermission** | `ProjectPermissions` | Assegnazione di un Permission a un utente LIMITATAMENTE a un progetto specifico. |

### 2.3 Matrice delle relazioni

```
                    ┌──────────────┬──────────────┬─────────────────────┐
                    │ Identity     │ Permission   │ Progetto            │
                    │ Role         │ (globale)    │ (specifico)        │
                    ├──────────────┼──────────────┼─────────────────────┤
     ASPNETROLES     │     ✓        │      -       │         -           │
     PERMISSIONS    │     -        │      ✓       │         -           │
     PERMESSIUTENTE │     -        │      ✓       │         -           │
     PROJECTPERM    │     -        │      -       │         ✓           │
                    └──────────────┴──────────────┴─────────────────────┘
```

---

## 3. Proposta Ruoli Ufficiali

### 3.1 Elenco ruoli

| Ruolo | Tipo | Uso designato |
|-------|------|---------------|
| **SuperAdmin** | Identity | Amministratore sistema assoluto. Bypass completo su tutti i permessi. |
| **Admin** | Identity | Amministratore funzionale. Accesso a `/admin`. Gestione utenti e permessi. |
| **Manager** | Identity | Supervisore operazioni. Dashboard Manager, gestione Ordini/Progetti. |
| **User** | Identity | Utente base standard. Utilizzo quotidiano. |
| **Progetti** | Identity | Utente focalizzato su gestione progetti. Dashboard Progetti. |
| **Ordini** | Identity | Utente focalizzato su gestione ordini. Dashboard Ordini. |
| **Esterno** | Identity | Utente esterno con accesso limitato. Solo visualizzazione. |

### 3.2 Ruoli Identity vs Ruoli funzionali

**Razionale**:
- I ruoli Identity (`SuperAdmin`, `Admin`, `Manager`, `User`, `Progetti`, `Ordini`, `Esterno`) sono ruoli tecnici registrati in `AspNetRoles`.
- Possono essere combinati (un utente può avere più ruoli Identity).
- I ruoli "funzionali" (`Admin`, `Manager`, `User`...) coincidono con ruoli Identity per semplicità.
- Il ruolo `SuperAdmin` è un ruolo speciale che conferisce tutti i permessi senza necessità di assegnazione esplicita.

---

## 4. Mapping Ruolo → Permessi

### 4.1 Tabella dei permessi per ruolo

| Ruolo | Permessi inclusi automaticamente | Note |
|------|--------------------------------|------|
| **SuperAdmin** | TUTTI i permessi (tutte le Permission) | Implementato via codice con bypass |
| **Admin** | `Admin.Access` + tutti i permessi di gestione | Accesso completo a `/admin`, gestione utenti, Edit su tutti i moduli |
| **Manager** | `Home.Index.Manager` + View su ODG/Progetti/Ordini + Edit su Ordini + Edit su Progetti | Gestione operativa |
| **User** | `Home.Index.User` + View su ODG/Progetti + Create/Edit su Ordini | Utentebase |
| **Progetti** | `Home.Index.Projects` + Progetti.View/Create/Edit + File.* + Contatti.* + Location.* | Gestione progetti |
| **Ordini** | `Home.Index.Orders` + Ordini.View/Create/Edit + Location.View | Gestione ordini |
| **Esterno** | `Home.Index.External` + View limitato | Solo visualizzazione |

### 4.2 Dettaglio permessi per ruolo

#### SuperAdmin
```
ODG.View, ODG.Create, ODG.Edit, ODG.Delete, ODG.Export
Finanze.View, Finanze.Details, Finanze.Create, Finanze.Edit, Finanze.Delete, Finanze.Download, Finanze.Export
Piani.View, Piani.Create, Piani.Edit, Piani.Delete
Progetti.View, Progetti.Create, Progetti.Edit, Progetti.Delete, Progetti.Dashboard
Contatti.View, Contatti.Create, Contatti.Details, Contatti.Edit, Contatti.Delete, Contatti.Export
File.FileRead, File.FileUpload, File.FileRename, File.Download, File.FileDelete, File.Folder.Create, File.Folder.Delete, File.Folder.Rename
Ordini.View, Ordini.Create, Ordini.Edit, Ordini.Delete, Ordini.Details
Location.View, Location.Details, Location.Create, Location.Edit, Location.Delete
Admin.Access
Home.Index.Admin, Home.Index.User, Home.Index.External, Home.Index.Manager, Home.Index.Projects, Home.Index.Orders, Home.Index.Public, Home.Privacy
Account.Profile, Account.UpdateProfile, Account.ChangePassword
Rental.User.Index, Rental.User.Create, Rental.User.Details, Rental.User.Edit, Rental.User.Delete, Rental.User.ReportDamage, Rental.User.ExportPdf
Rental.Admin
```

#### Admin
```
Admin.Access (obbligatorio per /admin)
+ Tutti i permessi CRUD su tutti i moduli
+ Gestione utenti (tramite AdminController)
```
Nota: Admin non ha automaticamente tutti i permessi granulari via codice, ma li ha assegnati esplicitamente nel seed o tramite AdminController.

#### Manager
```
Home.Index.Manager
ODG.View
Progetti.View, Progetti.Edit
Ordini.View, Ordini.Create, Ordini.Edit
Location.View, Location.Details
Rental.User.View, Rental.User.Create
Account.Profile, Account.UpdateProfile
```

#### User (base)
```
Home.Index.User
ODG.View
Progetti.View
Ordini.View, Ordini.Create
Location.View
Rental.User.View
Account.Profile, Account.UpdateProfile
```

#### Progetti
```
Home.Index.Projects
Progetti.View, Progetti.Create, Progetti.Edit, Progetti.Dashboard
Contatti.View, Contatti.Create, Contatti.Details, Contatti.Edit
File.FileRead, File.FileUpload, File.FileRename, File.Download, File.FileDelete, File.Folder.Create
Location.View, Location.Details, Location.Create
```

#### Ordini
```
Home.Index.Orders
Ordini.View, Ordini.Create, Ordini.Edit, Ordini.Details
Location.View, Location.Details
```

#### Esterno
```
Home.Index.External
Solo permessi di sola lettura per le risorse condivise
```

---

## 5. Strategia SuperAdmin

### 5.1 Bypass in PermissionService

```csharp
public async Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                   string permissionName,
                                   int? entityId = null)
{
    var u = await _userMgr.GetUserAsync(user);
    if (u == null) return false;

    // ✅ BYPASS SUPERADMIN: se l'utente ha il ruolo SuperAdmin, ha sempre accesso
    var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
    if (isSuperAdmin) return true;

    // ... resto della logica originale
}
```

### 5.2 Ruolo SuperAdmin seedato

In `Program.cs`, aggiungere il ruolo SuperAdmin ai ruoli creati:

```csharp
// ✅ CREA I RUOLI BASE
string[] roles = { "SuperAdmin", "Admin", "Manager", "User", "Progetti", "Ordini", "Esterno" };
```

### 5.3 Setup SuperAdmin

In `SuperadminService.cs`, modificare:

```csharp
// Assegna ruolo SuperAdmin invece di Admin
var roleResult = await _userManager.AddToRoleAsync(user, "SuperAdmin");
if (!roleResult.Succeeded)
{
    _logger.LogWarning("Failed to assign SuperAdmin role to superadmin");
}

// Also set the Role property on the User model
user.Role = "SuperAdmin";
await _userManager.UpdateAsync(user);
```

### 5.4 Ridondanza: assegnazione di tutti i permessi

Opzionale, come ridondanza:

```csharp
// Dopo aver creato lo SuperAdmin, assegnagli TUTTI i permessi
var allPermissions = await _db.Permessi.ToListAsync();
foreach (var perm in allPermissions)
{
    if (!_db.PermessiUtente.Any(p => p.UserId == user.Id && p.PermissionId == perm.Id))
    {
        _db.PermessiUtente.Add(new UserPermission
        {
            UserId = user.Id,
            PermissionId = perm.Id
        });
    }
}
await _db.SaveChangesAsync();
```

**Nota**: Il bypass in codice (`PermissionService`) è preferibile perché garantisce accesso anche se i permessi nel DB vengono cancellati accidentalmente. L'assegnazione nel DB è un layer di sicurezza aggiuntivo.

---

## 6. Strategia Admin

### 6.1 Accesso a /admin

Il controller `AdminController.cs` usa:

```csharp
[HasPermission("Admin.Access")]
public class AdminController : Controller { ... }
```

### 6.2 Opzioni di protezione

**Opzione A: Solo permesso (status quo)**

```csharp
[HasPermission("Admin.Access")]  // Solo permesso granulare
```

**Opzione B: Solo ruolo**

```csharp
[Authorize(Roles = "Admin")]  // Solo ruolo Identity
```

**Opzione C: Ruolo O permesso (raccomandato)**

```csharp
[Authorize(Roles = "Admin")]
[HasPermission("Admin.Access")]
```

oppure nel middleware custom.

### 6.3 Raccomandazione

Per `/admin`, usare **entrambi**:
- Ruolo `Admin` (o `SuperAdmin`) per accesso base
- Permesso `Admin.Access` per granularità

Questo permette di:
1. Assegnare ruolo Admin → accesso a /admin
2. Revocare il permesso Admin.Access → blocca accesso anche se ha ruolo

Il codice attuale richiede solo il permesso, che è corretto. Si suggerisce di aggiungere anche il ruolo come alternativa.

---

## 7. Proposta Database

### 7.1 Schema attuale

```
┌─────────────────┬─────────────────────────────┐
│  Tabella        │  Contenuto                  │
├─────────────────┼─────────────────────────────┤
│ AspNetRoles     │ Identity roles (Admin, ...) │
│ Permissions    │ Seeded permissions (tutte) │
│ PermessiUtente  │ User → Permission (globale) │
│ ProjectPerms   │ User → Project → Permission │
└─────────────────┴─────────────────────────────┘
```

### 7.2 Opzioni di design

#### Opzione A: Mantenere schema attuale (raccomandato)

**Vantaggi**:
- Nessuna modifica al database
- Nessuna migrazione necessaria
- I permessi vengono assegnati direttamente a utenti (via AdminController)
- I ruoli Identity servono solo come "bucket" logico

**Svantaggi**:
- Non c'è mapping ruolo → permessi automatico
- Ogni nuovo utente deve avere permessi assegnati manualmente

#### Opzione B: Tabella RolePermission

```csharp
public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; }      // Identity Role ID
    public int PermissionId { get; set; }
}
```

**Vantaggi**:
- Mapping esplicito ruolo → permessi
- Assegnazione automatica al login

**Svantaggi**:
- Necessita migrazione
- Maggiore complessità
- Ridondante rispetto a UserPermission se si assegnano permessi direttamente

### 7.3 Decisione

**Si raccomanda l'Opzione A**: mantenere lo schema attuale.

**Motivazioni**:
1. Il sistema attuale funziona: AdminController assegna permessi via `PermessiUtente`
2. L'aggiunta di SuperAdmin avviene con modifica minima
3. La migrazione non è necessaria
4. I permessi granulari esistono giá nel seed

Il ruolo `SuperAdmin` ottiene tutti i permessi via **bypass in PermissionService**, non tramite assegnazione automatica basata sul ruolo.

---

## 8. Piano Migrazione

### 8.1 Step definiti

```
┌──────────────────────────────────────────────────────────────┐
│  STEP 1: Aggiungere ruolo SuperAdmin                           │
├──────────────────────────────────────────────────────────────┤
│  File: Program.cs                                         │
│  Modifica: aggiungere "SuperAdmin" all'array roles[]        │
│  Impatto: basso (solo seed ruolo se non esiste)            │
│  Rischi: nessuno                                        │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  STEP 2: implementare bypass SuperAdmin in PermissionSvc  │
├──────────────────────────────────────────────────────────────┤
│  File: PermissionService.cs                                │
│  Modifica: aggiungere check ruolo SuperAdmin all'inizio     │
│  Impatto: basso (logica aggiuntiva, non modifica esistente)│
│  Rischi: nessuno                                        │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  STEP 3: modificare SuperadminService per assegnare ruolo     │
├──────────────────────────────────────────────────────────────┤
│  File: SuperadminService.cs                              │
│  Modifica: assegnare ruolo "SuperAdmin" invece di "Admin" │
│  Impatto: medio (setup esistente crea utente con ruolo diverso)│
│  Rischi: basso (solo per nuovi setup)                  │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  STEP 4: (opzionale) assegnare permessi a utenti esistenti  │
├──────────────────────────────────────────────────────────────┤
│  File: AdminController o script migrazione                  │
│  Modifica: opzionale - assegnare permessi base ai ruoli   │
│  Impatto: opzionale                                   │
│  Rischi: nessuno                                      │
└──────────────────────────────────────────────────────────────┘
```

### 8.2 Ordinamento implementazione

```
Step 1 → Step 2 → Step 3 (opzionale Step 4)
```

### 8.3 Impatto su utenti esistenti

- **Utenti esistenti**: nessun impatto, mantengono ruoli e permessi attuali
- **Nuovi setup**: SuperAdmin viene creato con ruolo SuperAdmin
- **Admin esistenti**: possono essere assegnati a ruolo SuperAdmin manualmente se necessario

### 8.4 Cosa NON modificare

- ❌ NON rimuovere permessi granulari esistenti
- ❌ NON modificare la tabella Permissions (giá seedata)
- ❌ NON togliere permessi a utenti esistenti
- ❌ NON alterare la logica di HasPermissionAttribute

---

## 9. Patch Plan Tecnico

### 9.1 File da modificare

| File | Modifica | Ordine |
|------|---------|--------|
| `Program.cs` | Aggiungere "SuperAdmin" ai ruoli | 1 |
| `Services/PermissionService.cs` | Aggiungere bypass SuperAdmin | 2 |
| `Services/SuperadminService.cs` | Assegnare ruolo SuperAdmin | 3 |

### 9.2 Dettaglio modifiche

#### Program.cs

```csharp
// Linea 145 (circa)
string[] roles = { "SuperAdmin", "Admin", "Manager", "User", "Progetti", "Ordini", "Esterno" };
//                                                                 ^^^^^^^^^^
//                                                           Aggiungere SuperAdmin
```

#### PermissionService.cs

```csharp
public async Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                       string permissionName,
                                       int? entityId = null)
{
    var u = await _userMgr.GetUserAsync(user);
    if (u == null) return false;

    // ✅ BYPASS SUPERADMIN
    var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
    if (isSuperAdmin) return true;

    // ... resto del codice esistente
}
```

#### SuperadminService.cs

```csharp
// Linea 73-81 (circa)
var roleResult = await _userManager.AddToRoleAsync(user, "SuperAdmin");  // Modificato
if (!roleResult.Succeeded)
{
    _logger.LogWarning("Failed to assign SuperAdmin role to superadmin");
}

user.Role = "SuperAdmin";  // Modificato
await _userManager.UpdateAsync(user);
```

### 9.3 Rischi

| Rischio | Probabilità | Impatto | Mitigazione |
|--------|-------------|---------|------------|
| Errore typos in ruolo "SuperAdmin" | Bassa | Critico | Verificare match esatto stringa |
| Bypass mai raggiunto (bug logica) | Bassa | Critico | Testare con utente SuperAdmin |
| Setup esistente rotto | Bassa | Critico | Testare setup wizard dopo modifica |

### 9.4 Test manuali

| # | Test | Verifica attesa |
|---|------|----------------|
| 1 | Setup wizard → crea utente | Nuovo utente ha ruolo SuperAdmin |
| 2 | Login come SuperAdmin | Accesso a TUTTI i controller senza errore |
| 3 | Login come Admin con permesso | Accesso a /admin |
| 4 | Login come User senza permessi | Accesso negato a risorse protette |
| 5 | Revoca ruolo SuperAdmin | Utente perde accesso automatico |

---

## 10. Sommario Decisioni

| Decisione | Scelta | Motivazione |
|-----------|--------|-------------|
| SuperAdmin bypass | Via codice in PermissionService | Piú sicuro e immediato |
| Ruolo SuperAdmin | Seedato in Program.cs | Necessario per check ruolo |
| Assegnazione permessi | Mantenere attuale (via AdminController) | Non serve migrazione |
| Tabella RolePermission | Non necessaria | Schema attuale sufficiente |
| Protezione /admin | Mantenere `[HasPermission("Admin.Access")]` | Funziona correttamente |

---

## 11. Appendice: Permessi Completi

### 11.1 Elenco Permissions (dal seed attuale)

| Permission | AppName |
|------------|---------|
| `ODG.View` | ODG |
| `ODG.Create` | ODG |
| `ODG.Edit` | ODG |
| `ODG.Delete` | ODG |
| `ODG.Export` | ODG |
| `Finanze.View` | Finanze |
| `Finanze.Details` | Finanze |
| `Finanze.Create` | Finanze |
| `Finanze.Edit` | Finanze |
| `Finanze.Delete` | Finanze |
| `Finanze.Download` | Finanze |
| `Finanze.Export` | Finanze |
| `Piani.View` | Piani |
| `Piani.Create` | Piani |
| `Piani.Edit` | Piani |
| `Piani.Delete` | Piani |
| `Progetti.View` | Progetti |
| `Progetti.Create` | Progetti |
| `Progetti.Edit` | Progetti |
| `Progetti.Delete` | Progetti |
| `Progetti.Dashboard` | Progetti |
| `Contatti.View` | Contatti |
| `Contatti.Create` | Contatti |
| `Contatti.Details` | Contatti |
| `Contatti.Edit` | Contatti |
| `Contatti.Delete` | Contatti |
| `Contatti.Export` | Contatti |
| `File.FileRead` | FileManager |
| `File.FileUpload` | FileManager |
| `File.FileRename` | FileManager |
| `File.Download` | FileManager |
| `File.FileDelete` | FileManager |
| `File.Folder.Create` | FileManager |
| `File.Folder.Delete` | FileManager |
| `File.Folder.Rename` | FileManager |
| `Ordini.View` | Ordini |
| `Ordini.Create` | Ordini |
| `Ordini.Edit` | Ordini |
| `Ordini.Delete` | Ordini |
| `Ordini.Details` | Ordini |
| `Location.View` | Location |
| `Location.Details` | Location |
| `Location.Create` | Location |
| `Location.Edit` | Location |
| `Location.Delete` | Location |
| `Admin.Access` | Admin |
| `Home.Index.Admin` | Home |
| `Home.Index.User` | Home |
| `Home.Index.External` | Home |
| `Home.Index.Manager` | Home |
| `Home.Index.Projects` | Home |
| `Home.Index.Orders` | Home |
| `Home.Index.Public` | Home |
| `Home.Privacy` | Home |
| `Account.Profile` | Account |
| `Account.UpdateProfile` | Account |
| `Account.ChangePassword` | Account |
| `Rental.User.Index` | Rental |
| `Rental.User.Create` | Rental |
| `Rental.User.Details` | Rental |
| `Rental.User.Edit` | Rental |
| `Rental.User.Delete` | Rental |
| `Rental.User.ReportDamage` | Rental |
| `Rental.User.ExportPdf` | Rental |
| `Rental.Admin` | Rental Admin |

**Totale: 68 permessi**