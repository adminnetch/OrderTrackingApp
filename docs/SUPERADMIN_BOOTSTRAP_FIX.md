# SuperAdmin Bootstrap Fix

## Sommario

Questo documento descrive il fix minimo implementato per fare in modo che il primo utente creato dal Setup Wizard sia un vero **SuperAdmin** con accesso totale al sistema.

## Cosa è stato fatto

### 1. Program.cs - Ruolo SuperAdmin seedato

**File**: `Program.cs:145`

```csharp
// ✅ CREA I RUOLI BASE (SuperAdmin + funzionali)
string[] roles = { "SuperAdmin", "Admin", "Manager", "User", "Progetti", "Ordini", "Esterno" };
```

Il ruolo `SuperAdmin` viene creato automaticamente all'avvio dell'applicazione se non esiste.

### 2. PermissionService.cs - SuperAdmin bypass

**File**: `Services/PermissionService.cs:29-31`

```csharp
// 2) SUPERADMIN BYPASS: se ha il ruolo SuperAdmin, ha sempre accesso
var isSuperAdmin = await _userMgr.IsInRoleAsync(u, "SuperAdmin");
if (isSuperAdmin) return true;
```

Quando un utente con ruolo `SuperAdmin` chiama `HasPermissionAsync`, il metodo restituisce `true` per qualsiasi permissionName, senza verificare i permessi nel database.

### 3. SuperadminService.cs - Setup crea SuperAdmin

**File**: `Services/SuperadminService.cs:62-102`

Il primo utente creato dal Setup Wizard:
1. Viene creato come utente Identity
2. Il ruolo `SuperAdmin` viene creato se non esiste
3. Il ruolo `SuperAdmin` viene assegnato all'utente
4. La proprietà `Role` dell'utente viene impostata a `"SuperAdmin"`
5. **Tutti i permessi granulari** vengono assegnati all'utente come ridondanza

## Perché questo fix è sufficiente ora

Il fix implementato:
- ✅ Crea il ruolo `SuperAdmin`
- ✅ Il primo utente ha ruolo `SuperAdmin`
- ✅ Il PermissionService fa bypass per SuperAdmin
- ✅ L'utente ha tutti i permessi (ridondanza)
- ✅ Backward compatible con utenti esistenti
- ✅ Nessuna modifica ai controller
- ✅ Nessuna modifica alle view
- ✅ Nessuna migrazione database

## Cosa NON è stato implementato (per RBAC V2 futuro)

Questo fix è **minimo**, non è il RBAC V2 completo. Le seguenti funzionalità restano da implementare:

| Feature | Stato | Note |
|---------|-------|------|
| Tabella RolePermission | ❌ Non implementata | Mapping ruolo → permessi esplicito |
| Matrice ruolo→permessi | ❌ Non implementata | Seed automatico permessi per ruolo |
| Campo IsGranted | ❌ Non implementato | Revoca permessi a livello utente |
| Admin.Access come policy | ❌ Non implementato | Ruolo Opzionale su controller |
| CRUD ruoli in admin | ❌ Non implementato | UI per gestire ruoli |
| Audit trail | ❌ Non implementato | Log modifiche |

## Flusso attuale

```
Setup Wizard → Crea utente
                ↓
         Assegna ruolo SuperAdmin
                ↓
         Assegna TUTTI i permessi
                ↓
         Login → HasPermission("X.Y")
                ↓
         SuperAdmin bypass → TRUE
```

## Verifica

Per verificare che il fix funzioni:

1. Avviare l'applicazione fresh (cancella database)
2. Completa il Setup Wizard
3. Verifica che l'utente creato abbia ruolo `SuperAdmin`
4. Verifica che possa accedere a qualsiasi risorsa protetta
5. Verifica che Non richieda permessi espliciti per operazioni CRUD

## Rischi e limiti

1. **Bypass totale**: SuperAdmin può fare qualsiasi cosa, anche operazioni di delete irreversibili
2. **Nessuna granularità**: Non c'è modo di limitare SuperAdmin temporalmente o per area
3. **Ridondanza nel DB**: I permessi sono assegnati anche se non necessari (il bypass funziona senza)

Questi limiti sono accettabili per ora e saranno risolti con RBAC V2 futuro.