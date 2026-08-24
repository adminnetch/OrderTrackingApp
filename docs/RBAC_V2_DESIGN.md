# RBAC V2 - Enterprise Authorization Model

## Executive Summary

Questo documento definisce un modello autorizzativo enterprise-grade per OrderTrackingApp, ispirato a ERP come Odoo. Mantiene la granularità esistente dei permessi ma introduce una vera gerarchia RBAC con:

- **Ruoli strutturati** con significato aziendale
- **Tabella RolePermission** per mapping ruolo → permessi
- **Tre livelli di override**: Role → User → Project
- **HasPermission ottimizzato** per usare la gerarchia

---

## 1. Panoramica del Modello

### 1.1 Gerarchia delle Autorizzazioni

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ENTITY LAYER                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐      ┌─────────────┐      ┌─────────────┐                     │
│   │  IDENTITY  │      │ PERMISSION │      │  PROJECT  │                     │
│   │   ROLE    │      │            │      │           │                     │
│   │           │      │  (seeded) │      │           │                     │
│   │ - SuperAd │      │            │      │           │                     │
│   │ - Admin   │      │ ODG.View  │      │ Project A │                     │
│   │ - Manager │      │ ODG.Edit  │      │ Project B │                     │
│   │ - User    │      │ Ordini.*   │      │ Project C │                     │
│   │ - Progetti│      │ File.*    │      │           │                     │
│   │ - Ordini  │      │ ...      │      │           │                     │
│   │ - Esterno │      │           │      │           │                     │
│   └─────┬─────┘      └─────┬─────┘      └─────┬─────┘                     │
│         │                   │               │                             │
│         │         MAPPA     │               │                             │
│         │    ┌────────┴──┐┴──────────┐  │                             │
│         │    │ ROLE PERMISSION │        │  │                             │
│         │    │ (nuova tabella) │        │  │                             │
│         │    └────────────────────┬───┘  │                             │
│         │                          │      │                             │
│         │            ┌─────────────┴──────┴──┐                         │
│         │            │                        │                         │
│         ▼            ▼                        ▼                         │
│   ┌─────────────────────────────────────────────┐                         │
│   │            ACCESS DECISION                  │                         │
│   │                                             │                         │
│   │  FINAL_PERMISSIONS =                         │                         │
│   │    RolePermissions                          │                         │
│   │    + UserPermissions (override ruolo)        │                         │
│   │    + ProjectPermissions (scoped override)  │                         │
│   └─────────────────────────────────────────────┘                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Flusso di Autorizzazione

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   User tenta accesso a risorsa/azione con [HasPermission("X.Y")]         │
│                                 │                                        │
│                                 ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ 1. CHECK IDENTITY ROLES                                        │   │
│   │    - L'utente ha ruolo SuperAdmin? → GRANT immediato            │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                 │                                        │
│                                 ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ 2. CALCOLA PERMESSI EFFETTIVI                                  │   │
│   │                                                                     │   │
│   │    effective = RolePermissions[ruoli]                           │   │
│   │                ∪ UserPermissions[utente]                        │   │
│   │                - revoked_by_user_override                         │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                 │                                        │
│                                 ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ 3. SE ENTITY_ID → CHECK PROJECT SCOPE                           │   │
│   │                                                                     │   │
│   │    project_perms = ProjectPermissions[utente, progetto]         │   │
│   │    effective = effective ∪ project_perms                        │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                 │                                        │
│                                 ▼                                        │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ 4. VERIFICA SE PERMESSO È PRESENTE                              │   │
│   │                                                                     │   │
│   │    if "X.Y" in effective: GRANT                                │   │
│   │    else: DENY                                                   │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Definizione Ruoli

### 2.1 Elenco Ruoli Ufficiali

| Ruolo | Livello | Significato | Scope tipico |
|------|--------|------------|-------------|
| **SuperAdmin** | 0 | Amministratore sistema assoluto | Tutto il sistema |
| **Admin** | 1 | Amministratore funzionale | Tutto il sistema |
| **Manager** | 2 | Supervisore operativo | Tutte le risorse |
| **User** | 3 | Utente operativo standard | Risorse assegnate |
| **Progetti** | 3 | Utente focalizzato Progetti | Progetti assegnati |
| **Ordini** | 3 | Utente focalizzato Ordini | Ordini assegnati |
| **Esterno** | 4 | Utente esterno | Solo lettura |

### 2.2 Gerarchia dei Ruoli

```
SUPER_ADMIN (livello 0)
│
├── ADMIN (livello 1)
│   ├── Can accedere a tutte le aree /admin
│   ├── Può gestire utenti
│   └── Può modificare permessi
│
├── MANAGER (livello 2)
│   ├── Può gestire Ordini e Progetti
│   └── Può visualizzare Finanze
│
├── USER (livello 3)
│   ├── Utente base standard
│   └── PUÒ AVERE ruoli specifici:
│       ├── PROGETTI - focus su Progetti
│       └── ORDINI - focus su Ordini
│
└── ESTERNO (livello 4)
    └── Solo visualizzazione
```

### 2.3 Assegnazione Multi-Ruolo

Un utente può avere **multiple ruoli Identity**. Esempi:

| Utente | Ruoli |
|--------|------|
| CEO/Founder | SuperAdmin |
| IT Admin | SuperAdmin |
| Responsabile Operations | Admin, Manager |
| Project Manager | Manager, Progetti |
| Assistente Ordini | User, Ordini |
| Regia esterna | Esterno |
| Fornitore | Esterno |

---

## 3. Matrice Ruolo → Permessi Default

### 3.1 Legenda

- **C** = Create
- **R** = Read/View
- **U** = Update/Edit
- **D** = Delete
- **X** = Export
- **A** = Access (per aree)
- **-** = non consentito

### 3.2 Matrice Completa

| Permesso | SuperAdmin | Admin | Manager | User | Progetti | Ordini | Esterno |
|---------|----------|-------|---------|------|----------|-------|---------|
| **ADMIN** | | | | | | | |
| Admin.Access | C/R/U/D | C/R/U/D | - | - | - | - | - |
| **ODG** | | | | | | | | |
| ODG.View | C/R/U/D | C/R/U/D | R | R | R | - | R |
| ODG.Create | C/R/U/D | C/R/U/D | C/R/U | C | - | - | - |
| ODG.Edit | C/R/U/D | C/R/U/D | U | - | - | - | - |
| ODG.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| ODG.Export | C/R/U/D | C/R/U/D | X | X | - | - | - |
| **FINANZE** | | | | | | | | |
| Finanze.View | C/R/U/D | C/R/U/D | R | - | - | - | - |
| Finanze.Details | C/R/U/D | C/R/U/D | R | - | - | - | - |
| Finanze.Create | C/R/U/D | C/R/U/D | C | - | - | - | - |
| Finanze.Edit | C/R/U/D | C/R/U/D | U | - | - | - | - |
| Finanze.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| Finanze.Download | C/R/U/D | C/R/U/D | C/R | - | - | - | - |
| Finanze.Export | C/R/U/D | C/R/U/D | X | - | - | - | - |
| **PIANI** | | | | | | | | |
| Piani.View | C/R/U/D | C/R/U/D | R | - | R | - | - |
| Piani.Create | C/R/U/D | C/R/U/D | C | - | C | - | - |
| Piani.Edit | C/R/U/D | C/R/U/D | U | - | U | - | - |
| Piani.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| **PROGETTI** | | | | | | | | |
| Progetti.View | C/R/U/D | C/R/U/D | R | R | C/R/U/D | R | R |
| Progetti.Create | C/R/U/D | C/R/U/D | C | - | C/R/U | - | - |
| Progetti.Edit | C/R/U/D | C/R/U/D | U | - | C/R/U | - | - |
| Progetti.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| Progetti.Dashboard | C/R/U/D | C/R/U/D | A | - | A | - | - |
| **CONTATTI** | | | | | | | | |
| Contatti.View | C/R/U/D | C/R/U/D | R | R | C/R/U/D | - | R |
| Contatti.Create | C/R/U/D | C/R/U/D | C | - | C/R/U | - | - |
| Contatti.Details | C/R/U/D | C/R/U/D | R | R | R | - | - |
| Contatti.Edit | C/R/U/D | C/R/U/D | U | - | U | - | - |
| Contatti.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| Contatti.Export | C/R/U/D | C/R/U/D | X | - | X | - | - |
| **FILE** | | | | | | | | | |
| File.FileRead | C/R/U/D | C/R/U/D | R | R | C/R/U | R | R |
| File.FileUpload | C/R/U/D | C/R/U/D | C | - | C/R/U | - | - |
| File.FileRename | C/R/U/D | C/R/U/D | U | - | U | - | - |
| File.Download | C/R/U/D | C/R/U/D | C/R | R | C/R | R | R |
| File.FileDelete | C/R/U/D | C/R/U/D | D | - | - | - | - |
| File.Folder.Create | C/R/U/D | C/R/U/D | C | - | C | - | - |
| File.Folder.Delete | C/R/U/D | C/R/U/D | D | - | - | - | - |
| File.Folder.Rename | C/R/U/D | C/R/U/D | U | - | U | - | - |
| **ORDINI** | | | | | | | | | |
| Ordini.View | C/R/U/D | C/R/U/D | R | R | R | C/R/U/D | R |
| Ordini.Create | C/R/U/D | C/R/U/D | C | C | - | C/R/U | - | - |
| Ordini.Edit | C/R/U/D | C/R/U/D | U | - | - | U | - |
| Ordini.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| Ordini.Details | C/R/U/D | C/R/U/D | R | R | R | R | R |
| **LOCATION** | | | | | | | | | |
| Location.View | C/R/U/D | C/R/U/D | R | R | R | C/R/U | R |
| Location.Details | C/R/U/D | C/R/U/D | R | R | R | R | - |
| Location.Create | C/R/U/D | C/R/U/D | C | - | C | C | - |
| Location.Edit | C/R/U/D | C/R/U/D | U | - | U | U | - |
| Location.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| **HOME** | | | | | | | | | |
| Home.Index.Admin | ✓ | ✓ | - | - | - | - | - |
| Home.Index.Manager | ✓ | ✓ | ✓ | - | - | - | - |
| Home.Index.Projects | ✓ | ✓ | ✓ | - | ✓ | - | - | - |
| Home.Index.Orders | ✓ | ✓ | ✓ | - | - | ✓ | - |
| Home.Index.User | ✓ | ✓ | - | ✓ | - | - | - |
| Home.Index.External | ✓ | ✓ | - | - | - | - | ✓ |
| Home.Index.Public | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Home.Privacy | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **ACCOUNT** | | | | | | | | |
| Account.Profile | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Account.UpdateProfile | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | - |
| Account.ChangePassword | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | - |
| **RENTAL** | | | | | | | | | |
| Rental.User.Index | C/R/U/D | C/R/U/D | R | R | R | R | - |
| Rental.User.Create | C/R/U/D | C/R/U/D | C | C | - | - | - |
| Rental.User.Details | C/R/U/D | C/R/U/D | R | R | R | R | - |
| Rental.User.Edit | C/R/U/D | C/R/U/D | U | - | - | - | - |
| Rental.User.Delete | C/R/U/D | C/R/U/D | - | - | - | - | - |
| Rental.User.ReportDamage | C/R/U/D | C/R/U/D | C | C | C | - | - |
| Rental.User.ExportPdf | C/R/U/D | C/R/U/D | X | X | - | - | - |
| Rental.Admin | C/R/U/D | C/R/U/D | - | - | - | - | - |

### 3.3 Note sulla Matrice

- **SuperAdmin** ha sempre tutti i permessi (bypass a livello di codice)
- **Admin** ha tutti i permessi CRUD su tutto (via RolePermission)
- La matrice definisce i **permessi default** assegnati automaticamente al ruolo
- Override a livello utente può aggiungere o rimuovere permessi

---

## 4. Nuova Tabella RolePermission

### 4.1 Modello Entity

```csharp
public class RolePermission
{
    public int Id { get; set; }

    [Required]
    public string RoleId { get; set; } = string.Empty;  // Identity Role Name

    [Required]
    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;

    public bool IsGranted { get; set; } = true;  // true = grant, false = deny

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
}
```

### 4.2 Schema Database

```sql
CREATE TABLE RolePermissions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    RoleId VARCHAR(256) NOT NULL,
    PermissionId INT NOT NULL,
    IsGranted BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(256),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id),
    UNIQUE KEY uk_role_permission (RoleId, PermissionId)
);
```

### 4.3 Seed Data

La tabella viene popolata in `Program.cs` con i permessi definiti nella matrice:

```csharp
var rolePermissions = new List<RolePermission>
{
    // SUPERADMIN - tutti i permessi
    new() { RoleId = "SuperAdmin", PermissionId = odgView, IsGranted = true },
    new() { RoleId = "SuperAdmin", PermissionId = ordiniView, IsGranted = true },
    // ... tutti i permessi ...

    // ADMIN - tutti i permessi (uguale a SuperAdmin per semplicità)
    new() { RoleId = "Admin", PermissionId = odgView, IsGranted = true },
    // ...

    // MANAGER - subset
    new() { RoleId = "Manager", PermissionId = adminAccess, IsGranted = false },
    new() { RoleId = "Manager", PermissionId = ordiniView, IsGranted = true },
    new() { RoleId = "Manager", PermissionId = ordiniEdit, IsGranted = true },
    // ...

    // USER - permessi base
    new() { RoleId = "User", PermissionId = ordiniView, IsGranted = true },
    new() { RoleId = "User", PermissionId = ordiniCreate, IsGranted = true },
    // ...

    // PROGETTI
    new() { RoleId = "Progetti", PermissionId = progettiView, IsGranted = true },
    // ...

    // ORDINI
    new() { RoleId = "Ordini", PermissionId = ordiniView, IsGranted = true },
    // ...

    // ESTERNO - solo lettura
    new() { RoleId = "Esterno", PermissionId = ordiniView, IsGranted = true },
    new() { RoleId = "Esterno", PermissionId = progettiView, IsGranted = true },
};
```

---

## 5. Livelli di Override

### 5.1 Gerarchia degli Override

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    OVERRIDE HIERARCHY                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   LIVELLO 1: ROLE PERMISSIONS (base)                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ RolePermissions[role_id] → permessi base del ruolo                 │   │
│   │                                                                   │   │
│   │ admin = {ODG.View, ODG.Create, ..., Admin.Access}                │   │
│   │ manager = {Ordini.View, Ordini.Edit, Progetti.View, ...}        │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                     │
│                                    │ UNION + OVERRIDE                     │
│                                    ▼                                     │
│   LIVELLO 2: USER PERMISSIONS (override globale)                          │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ PermessiUtente[user_id] → aggiunge o rimuove permessi utente      │   │
│   │                                                                   │   │
│   │ Se IsGranted = true: aggiunge permesso                              │   │
│   │ Se IsGranted = false: rimuove permesso (revoca)                  │   │
│   │                                                                   │   │
│   │ NOTA: campo IsGranted in UserPermission (estendere modello)        │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                     │
│                                    │ UNION + OVERRIDE                     │
│                                    ▼                                     │
│   LIVELLO 3: PROJECT PERMISSIONS (scoped)                               ���
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │ ProjectPermissions[user_id, project_id] → permessi specifici        │   │
│   │                                                                   │   │
│   │ Permette override a livello di singolo progetto                    │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   =========================================================================   │
│                                                                             │
│   PERMESSO EFFETTIVO = RolePerms(user.roles)                             │
│                     ∪ UserPerms(user)                                     │
│                     ∪ ProjectPerms(user, project)                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Modello Esteso UserPermission

```csharp
public class UserPermission
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    [Required]
    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;

    public bool IsGranted { get; set; } = true;  // NUOVO: gestisce revoca
    // true = grant (aggiungi permesso)
    // false = deny (rimuovi/escludi permesso del ruolo)
}
```

### 5.3 Logica di Override

```csharp
// Pseudo-codice per calcolo permessi effettivi
List<string> GetEffectivePermissions(string userId, List<string> userRoles, int? projectId)
{
    var perms = new HashSet<string>();

    // 1. Role permissions (base)
    foreach (var role in userRoles)
    {
        var rolePerms = db.RolePermissions
            .Where(rp => rp.RoleId == role && rp.IsGranted)
            .Select(rp => rp.Permission.Name);
        perms.UnionWith(rolePerms);
    }

    // 2. User overrides (globale)
    var userOverrides = db.PermessiUtente.Where(up => up.UserId == userId);
    foreach (var up in userOverrides)
    {
        if (up.IsGranted)
            perms.Add(up.Permission.Name);
        else
            perms.Remove(up.Permission.Name);
    }

    // 3. Project scoped overrides
    if (projectId.HasValue)
    {
        var projectPerms = db.ProjectPermissions
            .Where(pp => pp.UserId == userId && pp.ProjectId == projectId.Value);
        foreach (var pp in projectPerms)
        {
            perms.Add(pp.PermissionName);  // ProjectPermission usa Name, non ID
        }
    }

    return perms.ToList();
}
```

---

## 6. HasPermission e PermissionService Modificato

### 6.1 PermissionService V2

```csharp
public class PermissionService : IPermissionService
{
    private readonly UserManager<User> _userMgr;
    private readonly AppDbContext _db;

    public PermissionService(UserManager<User> userMgr, AppDbContext db)
    {
        _userMgr = userMgr;
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                            string permissionName,
                                            int? entityId = null)
    {
        var u = await _userMgr.GetUserAsync(user);
        if (u == null) return false;

        // SUPERADMIN BYPASS - sempre autorizzato
        var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
        if (isSuperAdmin) return true;

        // 1. GET RUOLI UTENTE
        var userRoles = await _userMgr.GetRolesAsync(u);

        // 2. CALCOLA PERMESSI EFFETTIVI
        var effectivePerms = await CalculateEffectivePermissionsAsync(u.Id, userRoles, entityId);

        // 3. CHECK
        return effectivePerms.Contains(permissionName);
    }

    private async Task<HashSet<string>> CalculateEffectivePermissionsAsync(
        string userId,
        IList<string> userRoles,
        int? entityId)
    {
        var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 2.1 ROLE PERMISSIONS
        foreach (var role in userRoles)
        {
            var rolePerms = await _db.RolePermissions
                .Where(rp => rp.RoleId == role && rp.IsGranted)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();
            perms.UnionWith(rolePerms);
        }

        // 2.2 USER OVERRIDES (globale)
        var userOverrides = await _db.PermessiUtente
            .Where(up => up.UserId == userId)
            .ToListAsync();
        foreach (var up in userOverrides)
        {
            if (up.IsGranted)
                perms.Add(up.Permission.Name);
            else
                perms.Remove(up.Permission.Name);
        }

        // 2.3 PROJECT SCOPED OVERRIDES
        if (entityId.HasValue)
        {
            // Determina projectId dall'entity
            var projectId = await ResolveProjectIdAsync(entityId.Value);
            if (projectId.HasValue)
            {
                var projectPerms = await _db.ProjectPermissions
                    .Where(pp => pp.UserId == userId && pp.ProjectId == projectId.Value)
                    .Select(pp => pp.PermissionName)
                    .ToListAsync();
                perms.UnionWith(projectPerms);
            }
        }

        return perms;
    }

    private async Task<int?> ResolveProjectIdAsync(int entityId)
    {
        // Logica per determinare il projectId da una entity
        // Implementazione specifica per tipo di entity
        return null; // Override in subclass o mapping
    }
}
```

### 6.2 HasPermissionAttribute (invariato)

L'attributo existing funziona **senza modifiche**:

```csharp
[HasPermission("Admin.Access")]  // continua a funzionare
public class AdminController : Controller { }
```

La differenza è che ora `PermissionService` usa la gerarchia RBAC per determinare se l'utente ha il permesso.

---

## 7. Admin.Access: Permesso vs Policy

### 7.1 Analisi

**Status Quo**: `Admin.Access` è un permesso granulare ( Permission con Name = "Admin.Access" )

**Problemi**:
- Chiunque abbia il permesso può accedere a /admin
- Il ruolo "Admin" non implica automaticamente l'accesso

### 7.2 Opzioni

| Opzione | Descrizione | Pro | Contro |
|---------|------------|-----|--------|
| **A. Mantenere Permesso** | `Admin.Access` resta Permission granulare | Nessuna modifica UI | Ruolo Admin non garantisce accesso |
| **B. Usare Policy** | Usare ASP.NET Core Authorization Policy | Piú flessibile | richiede refactor |
| **C. Ibrido** | Ambedue ruolo + permesso | SuperAdmin/Admin hanno accesso garantito | - |

### 7.3 Raccomandazione: Opzione C (Ibrido)

```csharp
// AdminController.cs
[HasPermission("Admin.Access")]
[Authorize( Roles = "Admin,SuperAdmin")]  // Opzionale: fallback ruolo
public class AdminController : Controller { }
```

**Motivazioni**:
1. Mantiene la granularità esistente (`Admin.Access` come permesso)
2. Aggiunge il ruolo come layer addizionale
3. Backward compatible con UI esistente
4. SuperAdmin ha sempre accesso bypass

**Nota**: Il permesso `Admin.Access` resta nella tabella Permissions per granularitàfine (es. dare accesso limitato all'area admin senza essere Admin).

---

## 8. Migrazione

### 8.1 Stato Attuale

```
┌─────────────────────────────────────────────────────────────────┐
│  CURRENT STATE                                                  │
├─────────────────────────────────────────────────────────────────┤
│  AspNetRoles: [Admin, Manager, User, Progetti, Ordini, Esterno] │
│  Permissions: [68 permessi granulari]                          │
│  PermessiUtente (UserPermission): user → permission           │
│  ProjectPermissions: user → project → permission              │
│  SuperAdmin: NON ESISTE come ruolo                            │
│  RolePermission: NON ESISTE                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Target

```
┌─────────────────────────────────────────────────────────────────┐
│  TARGET STATE                                                  │
├─────────────────────────────────────────────────────────────────┤
│  AspNetRoles: [SuperAdmin, Admin, Manager, User, Progetti,        │
│              Ordini, Esterno]                                   │
│  Permissions: [invariato]                                       │
│  PermessiUtente: [invariato + campo IsGranted]                 │
│  ProjectPermissions: [invariato]                               │
│  RolePermissions: [nuova - ruolo → permesso]                  │
│  SuperAdmin: ESISTE con bypass                                │
└─────────────────────────────────────────────────────────────────┘
```

### 8.3 Steps di Migrazione

```
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 1: CREARE MIGRAZIONE                                             │
├─────────────────────────────────────────────────────────────────────────┤
│  - Aggiungere colonna IsGranted a UserPermission (default true)         │
│  - Creare tabella RolePermissions                                      │
│  - Popolare RolePermissions con matrice ruolo→permessi                 │
│  - Aggiungere ruolo SuperAdmin in AspNetRoles                            │
│  - Popolare ruoli esistenti se non esistono                             │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 2: MODIFICARE PROGRAM.CS                                          │
├─────────────────────────────────────────────────────────────────────────┤
│  - Aggiungere "SuperAdmin" all'array roles[]                          │
│  - Seed RolePermissions dopo Permissions                             │
│  - NOTA: se RolePermissions esiste già, non duplicate                 │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 3: AGGIORNARE PERMISSIONSERVICE                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  - Implementare bypass SuperAdmin                                     │
│  - Implementare calcolo permessi effettivi (Role → User → Project)      │
│  - Modificare metodo HasPermissionAsync per usare gerarchia           │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 4: AGGIORNARE SUPERADMINSERVICE                                    │
├─────────────────────────────────────────────────────────────────────────┤
│  - Assegnare ruolo SuperAdmin al primo utente (invece di Admin)         │
│  - Assegnare TUTTI i permessi come ridondanza                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 5: (OPZIONALE) MIGRARE PERMESSI ESISTENTI                         │
├─────────────────────────────────────────────────────────────────────────┤
│  - Per ogni utente esistente:                                          │
│    - determinare ruolo Identity (dal campo User.Role o da AspNetUserRoles)│
│    - migrare permessi esistenti come UserPermissions con IsGranted=true   │
│  - Questo passo può essere skipped: i permessi esistenti funzioneranno  │
│    come override utente (livello 2)                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.4 Compatibilità all'Indietro

| Scenario | Comportamento |
|----------|---------------|
| Utente esistente con ruolo Admin | Mantiene i permessi assegnati in PermessiUtente |
| Utente esistente con permesso Admin.Access | Può accedere a /admin |
| Nuovo utente con ruolo Manager | Ottiene permessi da RolePermissions |
| Utente senza ruoli | Nessun permesso ( comportamento attuale invariato) |
| Setup wizard nuovo | Crea SuperAdmin con ruolo SuperAdmin |

### 8.5 Nota Importante

**Non rimuovere i permessi esistenti in PermessiUtente**:
- Gli utenti esistenti hanno già permessi assegnati
- Questi permessi funzionano come **livello 2 override**
- Se un utente aveva `ODG.View` assegnato, lo avrà comunque
- La nuova gerarchia aggiunge permessi basati sul ruolo, non li sostituisce

---

## 9. Impatto su altre parti del sistema

### 9.1 Controller esistenti

| Controller | Protezione attuale | Impatto V2 |
|------------|-------------------|------------|
| AdminController | `[HasPermission("Admin.Access")]` | Invariato, funziona con matrice ruoli |
| ODGController | `[HasPermission("ODG.View")]` ecc. | Invariato, funziona con matrice ruoli |
| OrderController | `[HasPermission("Ordini.View")]` | Invariato |
| ProjectController | `[HasPermission("Progetti.View")]` | Invariato |
| FileManager | `[HasPermission("File.FileRead")]` | Invariato |

### 9.2 Interfaccia Admin

| Schermata | Funzionalità | Impatto V2 |
|-----------|--------------|------------|
| EditPermessi | Assegna permessi globali | Estendere per mostrare anche ruoli |
| Users | Lista utenti | Mostra ruoli utente |
| Roles | (non esiste) | NUOVA: CRUD ruoli → permessi |

### 9.3 Setup Wizard

| Step | Modifica |
|------|---------|
| Superadmin | Crea utente con ruolo SuperAdmin |
| (nuovo) | Opzionale: assegna ruolo default |

---

## 10. Benefici del Modello V2

### 10.1 Rispetto al Modello Attuale

| Aspetto | Attuale | V2 |
|--------|---------|-----|
| Ruoli | 6 ruoli, SuperAdmin non esiste | 7 ruoli, con SuperAdmin |
| Mapping ruolo→permessi | Non esiste | Tabella esplicita |
| Override a livello utente | Si | Si (esteso) |
| Override per progetto | Si | Si |
| SuperAdmin | Non esiste | Esiste con bypass |
| Manutenzione | Difficile | Semplice: modifica matrice ruoli |
| Audit | Difficile | Semplice: vedi ruoli |

### 10.2 Confronto con ERP (Odoo)

| Feature | Odoo | OrderTracking V2 |
|---------|-----|------------------|
| Full inheritance | Si | Si |
| Groups → permessi | Si | Si (RolePermissions) |
| User override | Si | Si (UserPermission) |
| Field-level security | Parziale | No (futuro) |
| Record rules | Si | Si (ProjectPermissions) |
| Super admin | "Access Rights" | SuperAdmin bypass |

---

## 11. File da Modificare (Implementazione)

| File | Modifica | Priorità |
|------|---------|----------|
| `Models/RolePermission.cs` | NUOVO - tabella RolePermission | Alta |
| `Models/UserPermission.cs` | Aggiungere campo IsGranted | Alta |
| `Models/AppDbContext.cs` | Aggiungere DbSet, relazioni | Alta |
| `Program.cs` | Seed RolePermissions | Alta |
| `Services/PermissionService.cs` | Implementare gerarchia | Alta |
| `Services/SuperadminService.cs` | Assegnare ruolo SuperAdmin | Media |
| `Controllers/AdminController.cs` | Opzionale: aggiungere Authorize | Bassa |
| Views admin | Opzionale: CRUD ruoli | Bassa |

---

## 12. Sommario Design

### 12.1 Decisioni Chiave

| Decisione | Scelta |
|-----------|--------|
| SuperAdmin | Via codice + ruolo Identity |
| Tabella RolePermission | Nuova, obbligatoria |
| Matrice ruolo→permessi | Definita, seedata |
| Override user | Mantenuto, esteso con IsGranted |
| Override project | Mantenuto |
| Admin.Access | Mantenuto permesso, ruolo opzionale |
| Migrazione | Step-wise, senza downtime |

### 12.2 Cosa Non Cambia

- Controller esistenti
- HasPermissionAttribute
- Granularità permessi (68 permessi)
- Tabelle Permissions, PermessiUtente, ProjectPermissions

### 12.3 Cosa Nuovo

- Ruolo SuperAdmin
- Tabella RolePermission
- PermissionService con gerarchia
- Seed automatico permessi per ruolo
- Campo IsGranted in UserPermission

---

## Appendice A: Domande Aperte

1. **Record-level security**: Il modello supporta solo permessi di tipo "View/Create/Edit/Delete". Un futuro miglioramento potrebbe essere controllare specifiche istanze (es. "può vedere solo i suoi ordini").

2. **Campo di validità**: I permessi attualmente non hanno date di inizio/fine. Potrebbe essere aggiunto in futuro per ruoli temporanei.

3. **Audit trail**: Non esiste per ora, ma potrebbe essere aggiunto.

4. **Gruppi di permessi**: Potrebbe essere utile raggruppare permessi in "groups" (es. "tutti i permessi ODG") per gestione piú semplice.

---

## Appendice B: Elenco Completo Permissions

(Identico al documento precedente - 68 permessi)