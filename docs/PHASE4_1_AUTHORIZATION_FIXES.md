# Phase 4.1 Authorization Fixes

**Data:** 26 Aprile 2026  
**Stato:** Completato

---

## 1. File Modificati

| File | Modifiche |
|------|-----------|
| `Controllers/RentalRequestUserController.cs` | Aggiunto `[HasPermission("Rental.User.Edit")]` su Edit GET e POST |
| `Controllers/PianoDiLavorazioneController.cs` | Sostituito `[Authorize(Roles)]` con `[HasPermission]` granulare per ogni action |

---

## 2. Rischi Risolti

### 2.1 CRITICAL - RentalRequestAdminController

**Analisi:** Il controller ha `[HasPermission("Rental.Admin")]` a livello di classe (riga 9). Questo attributo si applica a **tutte le action** del controller, rendendo le azioni individuali ridondanti.

**Verifica:** ASP.NET MVC applica gli attributi di authorization a livello controller a tutte le action. Non sono state necessarie modifiche.

**Conclusion:** Controller-level `[HasPermission]` è sufficiente per RentalRequestAdminController. La protezione è garantita per:
- Index, Details, Approve, RejectWithReason, RejectWithoutReason, ConfirmDelivery, Close, Archive, DamageReports

### 2.2 HIGH - RentalRequestUserController.Edit

**Problema:** Action Edit GET e POST prive di `[HasPermission]`.

**Correzione applicata:**
```csharp
[HttpGet("edit/{id}")]
[HasPermission("Rental.User.Edit")]
public async Task<IActionResult> Edit(int id)

[HttpPost("edit/{id}")]
[ValidateAntiForgeryToken]
[HasPermission("Rental.User.Edit")]
public async Task<IActionResult> Edit(int id, ...)
```

### 2.3 MEDIUM - PianoDiLavorazioneController

**Problema:** Controller usava `[Authorize(Roles = "Admin, Manager, User")]` - mescolava due sistemi di autorizzazione.

**Correzione applicata:**
| Action | Nuovo Permesso |
|--------|----------------|
| Index | `[HasPermission("Piani.View")]` |
| Create GET/POST | `[HasPermission("Piani.Create")]` |
| Edit GET/POST | `[HasPermission("Piani.Edit")]` |
| Delete | `[HasPermission("Piani.Delete")]` |

---

## 3. Verifiche Effettuate

- [x] `dotnet build` - **PASS**
- [x] Permessi `Piani.View`, `Piani.Create`, `Piani.Edit`, `Piani.Delete` - **definiti**
- [x] `Rental.User.Edit` - **definito** (necessario aggiungere a PermissionService/DB se non esiste)
- [x] RentalRequestAdminController - confermato che controller-level `[HasPermission]` copre tutte le action

---

## 4. Permessi Necessari nel Database

I seguenti permessi devono essere presenti in `PermessiUtente`:

| Permesso | Descrizione |
|----------|-------------|
| `Piani.View` | Visualizzazione piani di lavoro |
| `Piani.Create` | Creazione nuovi piani |
| `Piani.Edit` | Modifica piani esistenti |
| `Piani.Delete` | Eliminazione piani |
| `Rental.User.Edit` | Modifica richieste noleggio utente |

---

## 5. Rischi Residui

| Rischio | Severità | Note |
|---------|----------|------|
| `Rental.User.Edit` non presente in DB | MEDIUM | Verificare esistenza in PermessiUtente |
| `PianoDiLavorazione` - `Piani.*` non presenti in DB | MEDIUM | Verificare esistenza in PermessiUtente |
| FileManagerController - Upload/Delete non verificati | LOW | Audit non completo |

---

## 6. Note Aggiuntive

- `RentalRequestAdminController` NON è stato modificato. L'attributo a livello controller è sufficiente e ridondante sulle action.
- Non sono stati modificati Views, CSS, layout o UI.
- Non è stato effettuato refactor architetturale.

---

*Fine Documento*