# ADMIN AREA STRUCTURE AUDIT

**Documento**: Analisi completa area Admin di OrderTrackingApp
**Data**: 2026-04-26
**Versione**: 1.0

---

## 1. Struttura Attuale Admin

### 1.1 Route e Controller

Il controller `AdminController` è protetto da `[HasPermission("Admin.Access")]`. Tutti i metodi richiedono questo permesso globale.

| Route | Action | Metodo HTTP | View | Descrizione |
|-------|--------|-------------|------|-------------|
| `/Admin/Users` | `Users()` | GET | `users.cshtml` | Lista utenti |
| `/Admin/NewUser` | `NewUser()` | GET | `newuser.cshtml` | Form creazione |
| `/Admin/CreateUser` | `CreateUser()` | POST | - | Crea utente |
| `/Admin/EditUser` | `EditUser()` | POST | - | Modifica utente |
| `/Admin/DeleteUser` | `DeleteUser()` | POST | - | Elimina utente |
| `/Admin/EditPermessi` | `EditPermessi()` | GET | `EditPermessi.cshtml` | Form permessi |
| `/Admin/EditPermessi` | `EditPermessiPost()` | POST | - | Salva permessi |

### 1.2 View e Form

**users.cshtml**
- Tabella lista utenti (FirstName, LastName, VisualName, Email, PhoneNumber, Username)
- Modale Bootstrap per edit inline (loadUserData JS)
- 3 bottoni per riga: Modifica, Elimina, Permessi
- Link a `/Admin/newuser`

**newuser.cshtml**
- Form creazione utente con campi: firstName, lastName, VisualName, email, phoneNumber, username, password
- Submit via POST a `CreateUser`

**EditPermessi.cshtml**
- Due sezioni: Permessi Globali + Permessi per Progetto
- Checkbox per ogni permesso categorizzato per nome (split '.')
- Accordion toggle per categorie
- Submit via POST a `EditPermessiPost`

### 1.3 Permessi Richiesti

| Azione | Permesso Necessario |
|--------|---------------------|
| Qualsiasi action AdminController | `Admin.Access` |

### 1.4 Bypass

`PermissionService.HasPermissionAsync` contiene bypass hardcoded per `SuperAdmin` (ruolo Identity):
```csharp
var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
if (isSuperAdmin) return true;
```

---

## 2. Gestione Utenti Attuale

### 2.1 Lista Utenti

- **Endpoint**: `GET /Admin/Users`
- **Controller**: `Users()` carica `_userManager.Users.ToList()`
- **View**: Tabella HTML con tutti gli utenti Identity

### 2.2 Creazione Utente

- **Form**: `/Admin/NewUser` (GET) → `CreateUser` (POST)
- **Campi**: firstName, lastName, VisualName, email, phoneNumber, username, password
- **Validazione**: Tutti obbligatori tranne phoneNumber
- **Logica**: Crea `new User` con `UserManager.CreateAsync(user, password)`

### 2.3 Modifica Utente

- **Metodo**: POST `EditUser`
- **Modalità**: Modale Bootstrap in `users.cshtml` chiama `loadUserData()` per popolare campi
- **Campi modificabili**: Tutti tranne Role
- **Password**: Opzionale, se fornita usa `RemovePasswordAsync` + `AddPasswordAsync`
- **BUG NOTO**: View ritorna a `View("Users", ...)` invece di `View(user)` - non popola campi per retry

### 2.4 Eliminazione Utente

- **Endpoint**: POST `DeleteUser`
- **Confirm**: JS `confirm()` nel form
- **Logica**: `UserManager.DeleteAsync(user)`
- **Redirect**: `RedirectToAction("Users")`

### 2.5 Assegnazione Ruoli

**NON IMPLEMENTATA**. Il modello User ha campo `Role` (stringa), ma:
- Nessuna action per assegnare ruoli
- Nessuna UI per visualizzare/modificare ruoli
- Il ruolo viene impostato solo durante creazione?

### 2.6 Assegnazione Permessi

- **Endpoint**: GET/POST `/Admin/EditPermessi`
- **Due tipi di permessi gestiti**:
  - **Globali** (`PermessiUtente` / `UserPermission`):-many to many tra User e Permission
  - **Progetto** (`ProjectPermission`): ternaria User + ProjectId + PermissionName

### 2.7 Limiti e Bug

| # | Problema | Severità |
|---|----------|----------|
| 1 | Campo `User.Role` non è usato per nulla | Alta |
| 2 | Nessuna gestione ruoli Identity nell'admin | Alta |
| 3 | EditUser ritorna view sbagliata su errore | Media |
| 4 | SuperAdmin non esiste come ruolo Identity (solo via codice) | Alta |
| 5 | No validazione email duplicata | Media |
| 6 | No conferma eliminazione con dati utente | Bassa |

---

## 3. Gestione Permessi Attuale

### 3.1 Caricamento Permessi

**PermissionService.HasPermissionAsync** esegue:
1. Check SuperAdmin bypass
2. Se `entityId` presente e permissionName inizia con "RentalRequest.", estrae `CinemaOrderId`
3. Check `ProjectPermissions` per permessi progetto-specifici
4. Check `PermessiUtente` per permessi globali

```csharp
// Flusso attuale
var hasOnProject = await _db.ProjectPermissions.AnyAsync(pp =>
    pp.UserId == u.Id &&
    pp.ProjectId == projectIdToCheck.Value &&
    pp.PermissionName == permissionName);

var hasGlobal = await _db.PermessiUtente
    .Where(up => up.UserId == u.Id)
    .Select(up => up.Permission.Name)
    .ContainsAsync(permissionName);
```

### 3.2 Salvataggio Permessi

**EditPermessiPost** esegue:
1. Delete tutte le `UserPermission` esistenti per l'utente
2. Insert nuove `UserPermission` per ogni `selectedPermissions[i]`
3. Delete tutte le `ProjectPermission` esistenti per l'utente
4. Insert nuove `ProjectPermission` per ogni `projectPermissions[i]` (formato "projectId:PermissionName")

### 3.3 Permessi Globali vs Progetto

| Tipo | Tabella | Chiave | Valore |
|------|---------|--------|--------|
| Globale | `PermessiUtente` | UserId → PermissionId | Reference a Permission |
| Progetto | `ProjectPermissions` | UserId, ProjectId → PermissionName | Stringa nome permesso |

### 3.4 Limiti vs RBAC V2

Il design RBAC V2 (`docs/RBAC_V2_DESIGN.md`) prevede:

| Feature | Attuale | RBAC V2 |
|---------|---------|---------|
| Mappatura ruolo→permessi | Non esiste | Tabella `RolePermission` |
| SuperAdmin | Bypass hardcoded | Ruolo Identity + bypass |
| Override utente | `PermessiUtente` flat | Con campo `IsGranted` (revoca) |
| Gerarchia permessi | No | Role → User → Project |
| Admin.Access | Permission granulare | Ruolo Admin + permesso |

**Problemi attuali**:
1. Nessuna tabella `RolePermission` - ruoli non hanno permessi default
2. SuperAdmin non è ruolo Identity - solo bypass codice
3. UserPermission senza `IsGranted` - non può revocare permessi ruolo
4. No audit trail per modifiche permessi
5. `HasPermission` non considera ruoli Identity per permessi

---

## 4. Stato Setup/Database

### 4.1 AppInstallation

**Esiste** in `Models/AppInstallation.cs`:

```csharp
public class AppInstallation
{
    public Guid InstallationId { get; set; }
    public DateTime InstallationDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string CurrentState { get; set; }  // InstallationState enum
    public string? PreviousState { get; set; }
    public string DatabaseProvider { get; set; } = "sqlite";
    public string? DatabasePath { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? LockedBySessionId { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? InstallationProfile { get; set; } = "express";
}
```

### 4.2 Stati Setup

```csharp
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

### 4.3 Come Verificare Setup Completato

```csharp
// Via AppDbContext
var installation = await _context.AppInstallations.FirstOrDefaultAsync();
var isComplete = installation?.CurrentState == InstallationState.Complete.ToString();
```

### 4.4 Informazioni Disponibili per /admin/database

| Informazione | Fonte | Disponibile |
|--------------|-------|-------------|
| InstallationId | AppInstallation | ✅ |
| InstallationDate | AppInstallation | ✅ |
| CompletedDate | AppInstallation | ✅ |
| CurrentState | AppInstallation | ✅ |
| DatabaseProvider | AppInstallation | ✅ |
| DatabasePath | AppInstallation | ✅ |
| LastErrorMessage | AppInstallation | ✅ |
| IsLocked | AppInstallation | ✅ |
| Versione App | _Layout.cshtml footer | ✅ (hardcoded "2.4.4") |
| Conteggio Users | UserManager | ✅ |
| Conteggio Permissions | _context.Permessi | ✅ |
| Conteggio Projects | _context.CinemaOrders | ✅ |

### 4.5 Informazioni NON Disponibili

| Informazione | Note |
|--------------|------|
| Statistiche tabelle | Rows count per entity |
| Health check database | Connection test |
| Log file path | Non configurato |
| Uptime | Non tracciato |
| Memory/CPU usage | Non implementato |

---

## 5. Proposta Struttura Futura /admin

### 5.1 Dashboard Principale

**Route**: `/admin` o `/admin/index`

```
┌────────────────────────────────────────────────────────────────┐
│                    ADMIN DASHBOARD                              │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   👥 UTENTI     │  │   🔐 PERMESSI   │  │  🗄️ DATABASE   │ │
│  │                 │  │                 │  │                 │ │
│  │  Totale: 12     │  │  Globali: 68    │  │  Provider:SQLite│ │
│  │  Attivi: 10     │  │  Progetto: 156  │  │  Stato: Complete│ │
│  │                 │  │                 │  │                 │ │
│  │  [Gestisci]     │  │  [Gestisci]     │  │  [Dettagli]     │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐                      │
│  │   📊 STATS      │  │   🔔 ATTIVITÀ   │                      │
│  │                 │  │                 │                      │
│  │  Progetti: 8    │  │  Ultimo login   │                      │
│  │  Ordini: 45     │  │  Ultimo accesso │                      │
│  │  Permessi: 224  │  │  Errori: 0      │                      │
│  │                 │  │                 │                      │
│  │  [Vedi stats]   │  │  [Vedi log]     │                      │
│  └─────────────────┘  └─────────────────┘                      │
└────────────────────────────────────────────────────────────────┘
```

### 5.2 Gestione Utenti

**Route**: `/admin/users`

**Sezioni**:
1. Lista utenti con filtri (attivi/inattivi, ruolo)
2. Creazione nuovo utente (modal o page)
3. Modifica utente esistente
4. Assegnazione ruoli Identity (non solo campo Role)
5. Assegnazione permessi globali e progetto

**Sub-route**:
- `GET /admin/users/new` - Form creazione
- `POST /admin/users/create` - Crea utente
- `GET /admin/users/{id}` - Dettagli utente
- `POST /admin/users/{id}/edit` - Modifica utente
- `POST /admin/users/{id}/delete` - Elimina utente
- `GET /admin/users/{id}/permissions` - Assegna permessi
- `POST /admin/users/{id}/permissions` - Salva permessi

### 5.3 Gestione Permessi

**Route**: `/admin/permissions`

**Sezioni**:
1. Lista permessi globali (tabella con filtri)
2. Lista permessi per progetto
3. Gestione ruoli → permessi (matrice)
4. Template permessi

**Sub-route**:
- `GET /admin/permissions` - Lista permessi
- `GET /admin/permissions/roles` - Gestione ruoli
- `GET /admin/permissions/matrix` - Matrice ruolo→permessi
- `POST /admin/permissions/roles/{id}` - Salva ruolo

### 5.4 Stato Database/Setup

**Route**: `/admin/database`

**Sezioni**:
1. Informazioni installazione
2. Stato database
3.Statistiche (count rows per tabella)
4. Health check
5. Log recenti errori
6. Link a ripristino setup (se fallito)

**Sub-route**:
- `GET /admin/database` - Dashboard database
- `GET /admin/database/health` - Health check JSON
- `POST /admin/database/reset-lock` - Sblocca installazione

### 5.5 Altre Sezioni Utili

| Sezione | Route | Descrizione |
|---------|-------|-------------|
| Settings | `/admin/settings` | Configurazione applicazione |
| Audit Log | `/admin/audit` | Log modifiche (futuro) |
| Roles | `/admin/roles` | CRUD ruoli Identity |
| API Keys | `/admin/api-keys` | Chiavi API (futuro) |

---

## 6. Patch Plan Futuro

### 6.1 File da Modificare

| File | Modifica | Priorità |
|------|---------|----------|
| `Controllers/AdminController.cs` | Aggiungere Index(), Database(), Roles(), Settings() | Alta |
| `Views/Admin/Index.cshtml` | Dashboard cards | Alta |
| `Views/Admin/Database.cshtml` | Info setup/database | Media |
| `Views/Admin/Roles.cshtml` | CRUD ruoli | Bassa |
| `Views/Admin/users.cshtml` | Fix bug edit, aggiungi ruoli column | Alta |
| `Services/PermissionService.cs` | Implementare gerarchia RBAC V2 | Alta |
| `Models/RolePermission.cs` | NUOVO - tabella ruolo→permesso | Alta |
| `Models/UserPermission.cs` | Aggiungere IsGranted | Media |

### 6.2 Nuove Action Consigliate

```csharp
// AdminController.cs

[HasPermission("Admin.Access")]
public IActionResult Index() { /* Dashboard */ }

[HasPermission("Admin.Access")]
public async Task<IActionResult> Database() { /* Info setup */ }

[HasPermission("Admin.Access")]
public async Task<IActionResult> Roles() { /* Lista ruoli */ }

[HttpGet]
[HasPermission("Admin.Access")]
public async Task<IActionResult> EditRoles(string roleId) { /* Modifica ruolo */ }

[HasPermission("Admin.Access")]
public IActionResult Permissions() { /* Lista permessi */ }

[HasPermission("Admin.Access")]
public IActionResult Settings() { /* Configurazione */ }
```

### 6.3 Nuove View Consigliate

| View | Posizione | Contenuto |
|------|-----------|----------|
| `Index.cshtml` | Views/Admin/ | Dashboard con cards |
| `Database.cshtml` | Views/Admin/ | Info setup/database |
| `Roles.cshtml` | Views/Admin/ | Lista/modifica ruoli |
| `Permissions.cshtml` | Views/Admin/ | Lista permessi |
| `Settings.cshtml` | Views/Admin/ | Configurazione |

### 6.4 Rischi

| Rischio | Probabilità | Impatto | Mitigazione |
|--------|-------------|---------|-------------|
| Breaking change permessi esistenti | Alta | Critico | Test approfonditi, backwards compat |
| Perdita dati permessi | Bassa | Critico | Backup DB prima migrazione |
| Setup bloccato non ripristinabile | Media | Alto | Aggiungere reset-lock in admin |
| SuperAdmin non più funzionante | Alta | Critico | Mantenere bypass codice |

### 6.5 Test Manuali

| Test | Passi | Risultato Atteso |
|------|-------|------------------|
| Accesso /admin senza login | Apri /admin | Redirect a login |
| Accesso /admin con utente normale | Login utente base | Access denied |
| Accesso /admin con SuperAdmin | Login SuperAdmin | Dashboard visibile |
| Creazione utente | Admin → New User | Utente creato |
| Modifica utente | Admin → Edit modal | Campi corretti post-errore |
| Assegnazione permessi | Admin → Permessi utente | Permessi salvati |
| Dashboard /admin/index | Apri /admin | Cards visibili |
| Database /admin/database | Apri /admin/database | Info AppInstallation |
| Setup incompleto | Simula setup failed | Mostra messaggio errore |

---

## Appendice A: Documenti di Riferimento

| Documento | Percorso | Utilizzo |
|-----------|----------|----------|
| RBAC V2 Design | `docs/RBAC_V2_DESIGN.md` | Target futuro gerarchia permessi |
| First Run Setup | `docs/FIRST_RUN_SETUP_ARCHITECTURE.md` | Architettura setup wizard |
| Authorization Audit | `docs/PHASE4_AUTHORIZATION_AUDIT.md` | Analisi autorizzazione |

## Appendice B: Route Map Attuale

```
/Admin/Users           → Users()        → users.cshtml
/Admin/NewUser         → NewUser()      → newuser.cshtml
/Admin/CreateUser      → CreateUser()   → (POST)
/Admin/EditUser        → EditUser()     → (POST)
/Admin/DeleteUser      → DeleteUser()   → (POST)
/Admin/EditPermessi    → EditPermessi() → EditPermessi.cshtml
/Admin/EditPermessi    → EditPermessiPost() → (POST)
```

## Appendice C: Note

- Questo documento è di sola analisi
- Non implementare modifiche
- Non fare commit
- Riferirsi a questo documento per implementazione futura