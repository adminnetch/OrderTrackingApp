# Remediation Plan

## Quick Wins (0-2 Giorni)

### 1.1 Correzioni Immediate

| Task | Priority | Effort | Impact |
|------|---------|--------|--------|
| Fix typo in `Program.cs:32-33` (LoginPath/AccessDeniedPath) | CRITICAL | 5m | Fix auth broken |
| Rimuovi duplicate IEmailService registration | HIGH | 5m | Memory leak |
| Sostituisci Random con `RandomNumberGenerator` | HIGH | 1h | Thread safety |
| Fix path logo in ODGController.cs:217 | MEDIUM | 10m | PDF export works |

### 1.2 Configurazione Quick Fixes

| Task | Priority | Effort | Impact |
|------|---------|--------|--------|
| Move credenziali a environment variables | CRITICAL | 30m | Security |
| Aggiungi rate limiting base | HIGH | 30m | Security |
| Configura secure cookies | MEDIUM | 15m | Security |

### 1.3 Codice Quick Fix

| Task | Priority | Effort | Impact |
|------|---------|--------|--------|
| Aggiungi null check `_env.WebRootPath` | MEDIUM | 10m | Stability |
| Aggiungi await su async methods | LOW | 10m | Correctness |

---

## Problemi Critici (Immediate Fix)

### 2.1 Security -Credenziali Hardcoded

**Problema:** Password e API keys in appsettings.json

**Soluzione:**
```bash
# In appsettings.json
"ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION}"
}

# In appsettings.Development.json  
"ConnectionStrings": {
    "DefaultConnection": "server=...;password=..."
}
```

**Task:**
- [ ] Estrai connection string in env var
- [ ] Estrai SMTP credentials in env var
- [ ] Rimuovi hardcoded da appsettings.json
- [ ] Aggiorna deployment docs

### 2.2 API Authorization

**Problema:** Endpoint `/api/orders` e `/api/cinemaorders` senza authentication

**Soluzione:** Aggiungere `[Authorize]` attribute

**Task:**
- [ ] Proteggi `OrderController.GetOrders` 
- [ ] Proteggi `OrderController.GetOrderStates`
- [ ] Protegi `CinemaController.GetCinemaOrders`
- [ ] Proteggi `CinemaController.GetCinemaOrdersStates`
- [ ] Valuta JWT vs cookie per API

### 2.3 Random Thread Safety

**Problema:** uso `new Random()` non thread-safe

**Task:**
- [ ] Sostituisci con `RandomNumberGenerator` in Order.cs
- [ ] Sostituisci in CinemaController.cs (ProjectNumber)

---

## Refactor Medio Termine (1-2 Settimane)

### 3.1 Layer Architecture

#### 3.1.1 Introduci Repository Layer

```csharp
// IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
```

**Task:**
- [ ] Crea `IRepository<T>` base interface
- [ ] Implementa `Repository<T>` con EF Core
- [ ] Refactor CinemaOrderRepository
- [ ] Refactor ODGOrderRepository
- [ ] Refactor OrderRepository
- [ ] Refactor RentalRequestRepository

#### 3.1.2 Introduci DTOs/ViewModels

**Task:**
- [ ] Crea `CinemaOrderDto` con validazione
- [ ] Crea `ODGOrderDto`
- [ ] Crea `RentalRequestDto`
- [ ] Crea `OrderDto`
- [ ] Aggiungi AutoMapper
- [ ] Refactor controllers per usare DTOs

### 3.2 Authorization Refactor

#### 3.2.1 Policy-Based Authorization

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProjectManager", policy =>
        policy.RequireClaim("Permission", "Progetti.Edit"));
});
```

**Task:**
- [ ] Migra da HasPermissionAttribute a policies
- [ ] Estrai permission logic in AuthorizationHandler
- [ ] Semplifica controller attributes
- [ ] Rimuovi PermissionService se non necessario

### 3.3 Validation

**Task:**
- [ ] Aggiungi FluentValidation
- [ ] Crea validators per ogni DTO
- [ ] Sostituisci DataAnnotations con FluentValidation
- [ ] Rimuovi validation logic da controllers

### 3.4 Error Handling

**Task:**
- [ ] Crea GlobalExceptionHandler
- [ ] Aggiungi error logging
- [ ] Crea error response DTOs
- [ ] Gestisci 404/500 custom views

### 3.5 PDF Consolidation

**Problema:** 3 librerie PDF

**Decisione:** Mantieni QuestPDF (moderno, più performante)

**Task:**
- [ ] Rimuovi iText7 e PdfSharp
- [ ] Mantieni solo QuestPDF

---

## Hardening Produzione (2-4 Settimane)

### 4.1 Infrastructure

#### 4.1.1 Health Checks

**Task:**
- [ ] Aggiungi health check endpoint
- [ ] Verifica DB connectivity
- [ ] Configura health check in load balancer

#### 4.1.2 Caching

**Task:**
- [ ] Configura Redis o in-memory cache
- [ ] Cache permessi utente (1h TTL)
- [ ] Cache lookup tables (24h TTL)
- [ ] Aggiungi response caching per API

#### 4.1.3 Database Optimization

**Task:**
- [ ] Aggiungi indici su colonne WHERE:
  - `CinemaOrder.Status`
  - `CinemaOrder.ProjectNumber`
  - `Order.TrackingNumber`
  - `ODGOrder.CinemaOrderId`
- [ ] Implementa pagination nei controllers
- [ ] Configura query splitting
- [ ] Abilita connection pooling

### 4.2 Security Hardening

#### 4.2.1 Authentication

**Task:**
- [ ] Abilita MFA (email/TOTP)
- [ ] Account lockout policy
- [ ] Password complexity requirements
- [ ] Session management migliorato

#### 4.2.2 API Security

**Task:**
- [ ] Implementa JWT per API
- [ ] Aggiungi rate limiting
- [ ] Implementa request/response signing
- [ ] Aggiungi request ID per tracing

#### 4.2.3 Data Protection

**Task:**
- [ ] Encrypt sensitive fields
- [ ] Implementa audit logging
- [ ] GDPR compliance checks
- [ ] Data retention policy

### 4.3 Monitoring & Observability

**Task:**
- [ ] Configura Serilog
- [ ] Aggiungi application insights/telemetry
- [ ] Crea dashboard
- [ ] Configura alerting

### 4.4 Testing

**Task:**
- [ ] Scrivi unit tests per services
- [ ] Scrivi integration tests per API
- [ ] Scrivi E2E tests (Selenium/Playwright)
- [ ] Configura CI/CD

### 4.5 Dockerizzazione

**Task:**
- [ ] Crea Dockerfile
- [ ] Crea docker-compose.yml
- [ ] Crea .dockerignore
- [ ] Configura multi-stage build

---

## Roadmap Consigiata Prioritaria

### Fase 1: Stabilizzazione (Sprint 1-2)
```
[x] Fix auth typo (CRITICAL)
[x] Fix security credentials  
[ ] Fix Random thread safety
[ ] Fix API authorization
[ ] Quick wins
```

### Fase 2: Architettura Base (Sprint 3-4)
```
[ ] Introduce Repository layer
[ ] Introduce DTOs
[ ] Authorization refactor
[ ] Validation layer
```

### Fase 3: Quality (Sprint 5-6)
```
[ ] PDF consolidation
[ ] Error handling
[ ] Health checks
[ ] Caching
```

### Fase 4: Security (Sprint 7-8)
```
[ ] MFA
[ ] API security
[ ] Audit logging
[ ] GDPR
```

### Fase 5: Operations (Sprint 9-10)
```
[ ] Docker
[ ] Monitoring
[ ] CI/CD
[ ] Tests
```

### Priorità Assoluta (Prima di produzione)

1. **Credenziali** - Move to environment variables
2. **API Auth** - Protect all API endpoints
3. **Thread Safety** - Fix Random usage
4. **Rate Limiting** - Prevent abuse
5. **HTTPS** - Force HTTPS in production

---

## Esclusioni dal Refactor

Le seguenti parti sono funzionanti e NON necessitano refactor:

- **PermissionService**: Funziona correttamente
- **HasPermissionAttribute**: Implementation corretta
- **ODG PDF Export**: Funzionante con QuestPDF
- **ProjectStorageService**: Funzionante
- **EF Core migrations**: Struttura OK
- **Base MVC structure**: Segue convenzioni ASP.NET Core

---

## Stima Sforzo Totale

| Fase | Giorni Uomo |
|------|-------------|
| Quick Wins | 2 |
| Problemi Critici | 3 |
| Refactor Medio Termine | 20 |
| Hardening Produzione | 30 |
| **Totale** | **~55** |