# Code Audit Report

## 1. Code Smells

### 1.1 Programmazione Inconsistente

| Location | Issue |
|----------|-------|
| `Program.cs:47-49` | **Duplicate IEmailService registration** - registrato due volte |
| `Program.cs:32-33` | **Typo nei path cookie**: `/account/aogin` invece di `/account/login`, `/account/accessaenied` invece di `/account/accessdenied` |
| `Order.cs:47-58` | **Random non thread-safe** per generazione numeri - possibili duplicati |
| `CinemaController.cs:66` | Same Random issue per ProjectNumber |

### 1.2 Naming Violations

| Location | Issue |
|----------|-------|
| `Models/TroupeCastContact.cs` | Enum `Role` come stringa invece di enum type-safe |
| `Models/RentalRequest.cs:41` | `Type` come stringa generica |
| `Models/Permission.cs` | `AppName` confusione con feature names |

### 1.3 Magic Strings/Numbers

| Location | Issue |
|----------|-------|
| `Program.cs:69` | Ruoli hardcoded in array |
| `Controllers/ODGController.cs:218` | Path logo hardcoded: `"images/logo_pj_nuovo.png"` |
| `OrderTrackingApp.csproj` | Package versioni non specificate (uso latest) |

### 1.4 Cattive Pratiche MVC

| Location | Issue |
|----------|-------|
| `ODGController.cs:410-459` | Reflection per UpdateCollection - fragile e lenta |
| `CinemaController.cs:117-134` | Logica business nel controller |
| `AdminController.cs:148-197` | Logica permessi nel controller |
| `HomeController.cs:29-52` | Catena if-else per role selection |

## 2. Bug Potenziali

### 2.1 concurrency/Thread Safety

| Severity | Issue | Location |
|----------|-------|----------|
| **HIGH** | `Random` non thread-safe per OrderNumber/TrackingNumber | `Order.cs:47-58` |
| **HIGH** | Stesso problema per ProjectNumber | `CinemaController.cs:66` |
| **MEDIUM** | Race condition su permessi utente | `AdminController.cs:157-170` |

### 2.2 Data Integrity

| Severity | Issue | Location |
|----------|-------|----------|
| **MEDIUM** | Cascade delete senza conferma | App-wide |
| **MEDIUM** | No optimistic concurrency | Tutti i model |
| **LOW** | Decimal precision non definita | `VoceSpesa.cs` |

### 2.3 Null Reference

| Severity | Issue | Location |
|----------|-------|----------|
| **MEDIUM** | `_env.WebRootPath` può essere null | `ODGController.cs:217` |
| **MEDIUM** | `ProjectStorageService` senza null check | `ProjectStorageService.cs:13-14` |
| **LOW** | Navigation properties senza null check | Multiple views |

## 3. Problemi Architetturali

### 3.1 Architecture Smells

| Issue | Impatto | Rimedi |
|-------|---------|-------|
| **No Repository Pattern** | Logica DB sparpagliata nei controller | Introdurre Repository<T> |
| **No DTOs** | Entity esposte direttamente alle view | Introdurre DTOs/ViewModels |
| **Fat Controllers** | Logica business nei controller | Extract a Service layer |
| **No API Layer** | Controllers mix MVC e API | Separare API controllers |
| **No Validation Layer** | Input validation inconsistente | Usare FluentValidation |

### 3.2 Dependency Issues

| Issue | Descrizione |
|-------|-----------|
| **Scoped/Singleton mixing** | PermissionService è Scoped, ProjectStorageService è Singleton |
| **Service location** | `HttpContext.RequestServices.GetRequiredService` in controller - anti-pattern |
| **No DI convention** | Registrazioni manuali in Program.cs |

### 3.3 Circular Dependency Risk

| Controllers | Servizi |
|------------|--------|
| CinemaController | ProjectStorageService |
| ODGController | UserManager |

## 4. Problemi di Sicurezza

### 4.1 Critici

| Issue | Severity | Location |
|-------|----------|---------|
| **Credenziali hardcoded** | CRITICAL | `appsettings.json:3-4` |
| **Password SMTP esposta** | CRITICAL | `appsettings.json:21` |
| **No brute-force protection** | HIGH | `AccountController.cs` |
| **API senza auth** | HIGH | `OrderController.cs:145-171` |

### 4.2 Medium

| Issue | Severity | Location |
|-------|----------|---------|
| **Path traversal** | HIGH | `FileManagerController` (presunto) |
| **No rate limiting** | MEDIUM | Global |
| **XSS potential** | MEDIUM | Views senza @Html.Raw controllato |
| **CSRF parziale** | MEDIUM | Alcuni form mancano @Html.AntiForgeryToken |

### 4.3 Basso

| Issue | Severity | Location |
|-------|----------|---------|
| **No security headers** | LOW | Program.cs |
| **No HSTS config** | LOW | Solo basic UseHsts |
| **Error details in prod** | LOW | - |

### 4.4 Informational

- SSL/TLS config non visibile
- No audit logging
- No failed login logging

## 5. Technical Debt

### 5.1 Codice Duplicato

| Duplicato | Originale |
|----------|-----------|
| `RentalRequestAdminController` | Probabilmente `ItemAdminController` |
| `RentalRequestUserController` | parte di RentalRequestUserController |
| View `IndexLoggedIn*.cshtml` | Potrebbero essere una sola |

### 5.2 Dipendenze Inutilizzate

| Package | Uso Effettivo |
|---------|------------|
| **EPPlus** | Presente ma probabilmente non usato |
| **HtmlAgilityPack** | Html parsing - da verificare uso |
| **BouncyCastle** | Crittografia - da verificare uso |
| **QuestPDF** vs **iText** vs **PdfSharp** | 3 librerie PDF - ridondanti |

### 5.3 Feature Incomplete

| Feature | Status |
|---------|-------|
| **Export PDF Rental** | Non implementato (`RentalRequestUserController.cs:261-264`) |
| **FileManager** | Presente controller, da verificare funzionalità |
| **Email Service** | Da verificare piena integrazione |
| **Status tracking** | View esiste ma poco chiaro |

### 5.4 Legacy Code

| Code | Note |
|------|------|
| `Order.cs` | Sistema legacy probabilmente non in uso |
| `SeedData.cs` | Non più usato (Categories in migration) |
| `LoginViewModel.cs` | Custom, potrebbe usare built-in |

### 5.5 Configurazione Tech Debt

| Issue |
|-------|
| Versioni EF Core 6 (non LTS) |
| Versioni pacchetti mixing stable/beta |
| No .editorglobal.json |
| No analysers configurati |

## 6. File Duplicati o Inutilizzati

### 6.1 File Sospetti (da verificare uso)

| File | Possibile Uso |
|------|-------------|
| `dotnet-install.sh` | Build script, non necessario in repo |
| `dotnet-sdk-*.tar.gz` | SDK cached - non necessario |
| `packages-microsoft-prod.deb` | Package cache |
| `OrderTrackingApp.csproj.Backup.tmp` | Backup file |
| `OrderTrackingApp.csproj.user` | User-specific |
| `test.docx` | File di test |

### 6.2 Codice Non Raggiungibile

- Alcune route API non documentate
- vecchie action methods
- vecchie view

## 7. Problemi Database

### 7.1 Schema Issues

| Issue |
|-------|
| **No foreign key constraints** esplicite in alcuni punti |
| **Denormalizzazione** evidente (es. UserVisualName in RentalRequest) |
| **No indexes** su colonne usate in WHERE frequenti |
| **No migrations** per optimization |

### 7.2 Migration Issues

- 20+ migrazioni - difficile manutenzione
- Some migrations sembrano ridondanti
- No squash migration

## 8. Code Coverage & Testing

- **Nessun test** presente nel progetto
- No unit tests
- No integration tests

## 9. Performance Concerns

| Issue | Impatto |
|-------|---------|
| **No caching** | Query DB ripetute |
| **No pagination** | Liste intere caricate |
| **No eager loading** selettivo | N+1 query problem |
| **No connection pooling** config | - |

## 10. Summary Matrix

| Category | Count | Critical | High | Medium | Low |
|----------|-------|----------|------|--------|-----|
| Code Smells | 15+ | 0 | 2 | 13 |
| Bugs | 8 | 2 | 3 | 3 |
| Security | 10+ | 2 | 4 | 4 |
| Architecture | 6 | - | - | - |
| Tech Debt | 12 | - | - | - |