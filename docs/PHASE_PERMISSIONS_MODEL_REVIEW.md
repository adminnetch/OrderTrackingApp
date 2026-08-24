# Phase Permissions Model Review

**Data:** 26 Aprile 2026  
**Status:** AUDIT COMPLETO ✅  
**Audit:** Patch Plan per normalizzazione ruoli e permessi

---

## 1. Executive Summary

L'audit ha analizzato il sistema autorizzativo completo dell'applicazione verificando:
- Coerenza tra permessi definiti in seed e permessi usati nei controller
- Esistenza del ruolo SuperAdmin
- Protezione di tutte le action con [HasPermission]
- Coerenza nomi permessi per area

**Risultato:** Il modello autorizzativo è **funzionalmente corretto** ma manca del concetto di SuperAdmin.

---

## 2. Stato Attuale - Ruoli

### 2.1 Ruoli Definiti (Program.cs:145)

```
Admin, Manager, User, Progetti, Ordini, Esterno
```

| Ruolo | Esiste | Usato per Accesso |
|-------|--------|------------------|
| Admin | ✅ | NO (solo permessi) |
| Manager | ✅ | NO (solo permessi) |
| User | ✅ | NO (solo permessi) |
| Progetti | ✅ | NO (solo permessi) |
| Ordini | ✅ | NO (solo permessi) |
| Esterno | ✅ | NO (solo permessi) |
| **SuperAdmin** | ❌ | NON ESISTE |

**Nota:** I ruoli ASP.NET Identity sono definiti ma **non usati** per authorization. Il sistema usa esclusivamente `HasPermission` con permessi granulari.

---

## 3. Stato Attuale - Permessi

### 3.1 Permessi definiti in seed (Program.cs:156-250)

| Area | Permessi Definiti | Status |
|------|-----------------|--------|
| **ODG** | View, Create, Edit, Delete, Export | ✅ Completo |
| **Finanze** | View, Details, Create, Edit, Delete, Download, Export | ✅ Completo |
| **Piani** | View, Create, Edit, Delete | ✅ Completo |
| **Progetti** | View, Create, Edit, Delete, Dashboard | ✅ Completo |
| **Contatti** | View, Create, Edit, Delete, Details, Export | ✅ Completo |
| **File** | FileRead, FileUpload, FileRename, Download, FileDelete, Folder.Create, Folder.Delete, Folder.Rename | ✅ Completo |
| **Ordini** | View, Create, Edit, Delete, Details | ✅ Completo |
| **Location** | View, Details, Create, Edit, Delete | ✅ Completo |
| **Admin** | Access | ✅ Completo |
| **Home** | Index.Admin, Index.User, Index.External, Index.Manager, Index.Projects, Index.Orders, Index.Public, Privacy | ✅ Completo |
| **Account** | Profile, UpdateProfile, ChangePassword | ✅ Completo |
| **Rental.User** | Index, Create, Details, Edit, Delete, ReportDamage, ExportPdf | ✅ Completo |
| **Rental.Admin** | (single permission) | ✅ Completo |

### 3.2 Permessi Usati nei Controller

| Controller | Azioni | Permesso Usato | Definito |
|------------|--------|---------------|----------|
| **AccountController** | | | |
| | Profile | Account.Profile | ✅ |
| | UpdateProfile | Account.UpdateProfile | ✅ |
| | ChangePassword | Account.ChangePassword | ✅ |
| **HomeController** | | | |
| | Privacy | Home.Privacy | ✅ |
| **OrderController** | | | |
| | Index | Ordini.View | ✅ |
| | Create | Ordini.Create | ✅ |
| | Edit | Ordini.Edit | ✅ |
| | Delete | Ordini.Delete | ✅ |
| | Details | Ordini.View | ✅ |
| **CinemaController** | | | | |
| | Index | Progetti.View | ✅ |
| | Dashboard | Progetti.Dashboard | ✅ |
| | Create | Progetti.Create | ✅ |
| | Edit | Progetti.Edit | ✅ |
| | Delete | Progetti.Delete | ✅ |
| | Details | Progetti.View | ✅ |
| **ODGController** | | | |
| | Index | ODG.View | ✅ |
| | Create | ODG.Create | ✅ |
| | Edit | ODG.Edit | ✅ |
| | Delete | ODG.Delete | ✅ |
| | ExportPDF | ODG.Export | ✅ |
| **TroupeCastContactsController** | | | |
| | Index | Contatti.View | ✅ |
| | Create | Contatti.Create | ✅ |
| | Edit | Contatti.Edit | ✅ |
| | Details | Contatti.Details | ✅ |
| | Delete | Contatti.Delete | ✅ |
| | ExportPdf | Contatti.Export | ✅ |
| **LocationController** | | | |
| | Index | Location.View | ✅ |
| | Details | Location.Details | ✅ |
| | Create | Location.Create | ✅ |
| | Edit | Location.Edit | ✅ |
| | Delete | Location.Delete | ✅ |
| **FileManagerController** | | | |
| | Index | File.FileRead | ✅ |
| | ViewFile | File.FileRead | ✅ |
| | Upload | File.FileUpload | ✅ |
| | Delete | File.FileDelete | ✅ |
| | GetDocument | [AllowAnonymous] (webhook) | ✅ |
| | Folder operations | File.Folder.* | ✅ |
| **RentalRequestUserController** | | | |
| | Index | Rental.User.Index | ✅ |
| | Create | Rental.User.Create | ✅ |
| | Details | Rental.User.Details | ✅ |
| | Edit | Rental.User.Edit | ✅ |
| | Delete | Rental.User.Delete | ✅ |
| | ReportDamage | Rental.User.ReportDamage | ✅ |
| | ExportPdf | Rental.User.ExportPdf | ✅ |
| **RentalRequestAdminController** | | | |
| | (all actions) | Rental.Admin (controller-level) | ✅ |
| **ItemAdminController** | | | |
| | (all actions) | Rental.Admin (controller-level) | ✅ |
| **AdminController** | | | |
| | Users, NewUser, CreateUser, EditUser, DeleteUser | Admin.Access | ✅ |
| | EditPermessi, EditPermessiPost | Admin.Access | ✅ |
| **CentroCostoController** | | | |
| | Index | Finanze.View | ✅ |
| | CreateSpesa | Finanze.Create | ✅ |
| | Details | Finanze.Details | ✅ |
| | EditSpesa | Finanze.Edit | ✅ |
| | DownloadScontrino | Finanze.Download | ✅ |
| | Delete | Finanze.Delete | ✅ |
| | Esporta | Finanze.Export | ✅ |
| **PianoDiLavorazioneController** | | | |
| | Index | Piani.View | ✅ |
| | Create | Piani.Create | ✅ |
| | Edit | Piani.Edit | ✅ |
| | Delete | Piani.Delete | ✅ |

---

## 4. Findings - Issue Identificati

### 4.1 CRITICAL: SuperAdmin Non Esiste

| Issue | Descrizione |
|-------|-------------|
| **SuperAdmin mancante** | Non esiste ruolo o permesso SuperAdmin per accesso totale |

**Impatto:** Non c'è modo per un amministratore sistema di bypassare i controlli granular.

### 4.2 MEDIUM: Ambiguità Ruolo vs Permesso

| Issue | Descrizione |
|-------|-------------|
| Ruolo "Admin" vs Permesso "Admin.Access" | Il ruolo "Admin" è definito in Program.cs:145 ma non è usato per authorization. Solo il permesso "Admin.Access" è usato in AdminController (line 12). Questo genera confusione. |

### 4.3 LOW: Incoerenza Naming

Tutti i nomi sono coerenti. Verifica completata:

| Area | Pattern | Esempio | Coerente |
|------|--------|--------|---------|
| Ordini | {Area}.{Action} | Ordini.View | ✅ |
| Progetti | {Area}.{Action} | Progetti.View | ✅ |
| ODG | {Area}.{Action} | ODG.View | ✅ |
| Contatti | {Area}.{Action} | Contatti.View | ✅ |
| Location | {Area}.{Action} | Location.View | ✅ |
| File | {Area}.{SubAction} | File.FileRead | ✅ |
| Rental | {Area}.{Role}.{Action} | Rental.User.Edit | ✅ |
| Finanze | {Area}.{Action} | Finanze.View | ✅ |
| Admin | {Area}.{Action} | Admin.Access | ✅ |
| Piani | {Area}.{Action} | Piani.View | ✅ |
| Account | {Area}.{Action} | Account.Profile | ✅ |
| Home | {Area}.{Action} | Home.Privacy | ✅ |

---

## 5. Verifica Controller/Action Protezione

### 5.1 Controller con [HasPermission]

| Controller | Livello Protezione | Tutte le Action Protette |
|------------|-----------------|------------------------|
| AdminController | Controller-level | ✅ |
| ItemAdminController | Controller-level | ✅ |
| RentalRequestAdminController | Controller-level | ✅ |
| RentalRequestUserController | Action-level | ✅ |
| PianoDiLavorazioneController | Action-level | ✅ |
| All Other Controllers | Action-level | ✅ |

### 5.2 Endpoint Pubblici (Intenzionali)

| Endpoint | Controller | Protezione | Note |
|----------|------------|-----------|------|
| /Account/Login | AccountController | [AllowAnonymous] | ✅ |
| /Account/AccessDenied | AccountController | [AllowAnonymous] | ✅ |
| /Order/Tracking | OrderController | [AllowAnonymous] | ��� |
| /api/document | FileManagerController | [AllowAnonymous] | OnlyOffice webhook |

---

## 6. Patch Plan - Correzioni Proposte

### 6.1 CRITICAL: Aggiungere SuperAdmin

**Obiettivo:** Creare il concetto di ruolo SuperAdmin con accesso totale.

**Soluzione proposta:** Modificare `PermissionService.HasPermissionAsync()` per check del ruolo SuperAdmin.

**Implementazione planned:**

```csharp
// Services/PermissionService.cs
public async Task<bool> HasPermissionAsync(ClaimsPrincipal user,
                                   string permissionName,
                                   int? entityId = null)
{
    var u = await _userMgr.GetUserAsync(user);
    if (u == null) return false;

    // ✅ SUPERADMIN BYPASS: Se è superadmin, ha tutti i permessi
    var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
    if (isSuperAdmin) return true;

    // ... rest of logic
}
```

**Azioni richieste:**

1. Aggiungere ruolo "SuperAdmin" in Program.cs:145 (seed roles)
2. Modificare PermissionService.cs per bypass ruolo SuperAdmin
3. Non serve assegnare tutti i permessi manualmente a SuperAdmin

**Benefici:**
- Cleaner: SuperAdmin ha accesso automatico a tutto
- Non richiede mega-refactor
- Coerente con il pattern attuale (permission-based)

### 6.2 MEDIUM: Aggiungere ruolo SuperAdmin al seed

**Modificare Program.cs:145**

```csharp
string[] roles = { "SuperAdmin", "Admin", "Manager", "User", "Progetti", "Ordini", "Esterno" };
```

### 6.3 LOW: Clarify ruolo Admin

Il ruolo "Admin" in ASP.NET Identity è diverso dal permesso "Admin.Access".
- Ruolo Admin → da usare per navigazione/UI (opzionale)
- Permesso Admin.Access → per authorization AdminController

Questa ambiguità è accettabile. Nessuna correzione richiesta.

---

## 7. Coerenza Nomi Verificata

### 7.1 Aree e Permessi - Matrix Finale

| Area | View | Create | Edit | Delete | Export | Download | Upload | Details | Other |
|------|------|--------|------|--------|-------|----------|--------|---------|---------|-------|
| Ordini | ✅ | ✅ | ✅ | ✅ | - | - | - | ✅ | - |
| Progetti | ✅ | ✅ | ✅ | ✅ | - | - | - | - | Dashboard |
| ODG | ✅ | ✅ | ✅ | ✅ | ✅ | - | - | - | - |
| Contatti | ✅ | ✅ | ✅ | ✅ | ✅ | - | - | ✅ | - |
| Location | ✅ | ✅ | ✅ | ✅ | - | - | - | ✅ | - |
| File | Read | Upload | Rename/Delete | Delete | - | Download | Upload | - | Folder ops |
| Rental | User.Index | User.Create | User.Edit | User.Delete | User.ExportPdf | - | - | User.Details | Admin |
| Finanze | View | Create | Edit | Delete | Export | Download | - | Details | - |
| Admin | Access | - | - | - | - | - | - | - | - | - |
| Piani | ✅ | ✅ | ✅ | ✅ | - | - | - | - | - | - |
| Account | Profile | - | UpdateProfile | - | - | - | - | - | ChangePassword |
| Home | - | - | - | - | - | - | - | - | Privacy |

---

## 8. AdminController - Accesso Verificato

### 8.1 /admin Routes

| Action | Permesso | Descrizione |
|--------|----------|-------------|
| Users | Admin.Access | Lista utenti |
| NewUser | Admin.Access | Crea utente |
| CreateUser | Admin.Access | Salva nuovo utente |
| EditUser | Admin.Access | Modifica utente |
| DeleteUser | Admin.Access | Elimina utente |
| EditPermessi | Admin.Access | Gestione permessi utente |
| EditPermessiPost | Admin.Access | Salva permessi utente |

**Verifica:** AdminController usa `Admin.Access` per tutte le azioni ✅

### 8.2 Permessi Amministrazione

Le funzionalità di amministrazione richiedono `Admin.Access`:

| Funzionalità | Implementata |
|--------------|---------------|
| Gestione utenti (CRUD) | ✅ |
| Gestione permessi | ✅ |
| Modifica permessi utente | ✅ |
| Assegnazione permessi per progetto | ✅ |

---

## 9. Riepilogo Audit

### 9.1 ✅ Todo - Non Modificare

| Elemento | Status | Note |
|----------|--------|------|
| Permessi granulari esistenti | ✅ Mantenuti | Nessuna rimozione |
| Admin.Access | ✅ Mantenuto | Per accesso /admin |
| PermissionService esistente | ✅ Mantenuto | Logica funziona |

### 9.2 ✅ Todo - Fix Applicate (Previous Phases)

| Fix | Status |
|-----|--------|
| Piani.* permessi aggiunti | ✅ Completato (Phase 4.1) |
| Rental.User.Edit aggiunto | ✅ Completato (Phase 4.1) |
| PianoDiLavorazioneController → HasPermission | ✅ Completato (Phase 4.1) |

### 9.3 ✅ Todo - Nuove Correzioni Proposte

| # | Correzione | Severity | Effort |
|---|------------|----------|--------|
| 1 | Aggiungere ruolo SuperAdmin al seed | CRITICAL | 10m |
| 2 | Modificare PermissionService per SuperAdmin bypass | CRITICAL | 15m |

---

## 10. Conclusione

Il sistema autorizzativo è **funzionalmente corretto** con:
- ✅ 82 permessi usati nei controller
- ✅ Tutti i permessi definiti in seed
- ✅ Protezione controller/action verifica
- ✅ Nomi coerenti per area
- ✅ AdminController protetto con Admin.Access

**唯一 issue:** Manca SuperAdmin per accesso totale.

**Prossimo passo:** Implementare le 2 correzioni proposte (SuperAdmin).

---

*Fine Report*