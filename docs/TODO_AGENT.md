# TODO Agent - Backlog Per Priorità

## Fase 1: Quick Wins (Giorno 1)

### 1.1 Fix Urgente - Auth Rotte

- [ ] **FIX: Correggi typo path login/access-denied**
  - File: `Program.cs:32-33`
  -Da: `options.LoginPath = "/account/aogin";`
  - A: `options.LoginPath = "/account/login";`
  -Da: `options.AccessDeniedPath = "/account/accessaenied";`
  - A: `options.AccessDeniedPath = "/account/accessdenied";`

### 1.2 Fix Memory Leak

- [ ] **FIX: Rimuovi IEmailService duplicato**
  - File: `Program.cs:47-49`
  - Rimuovi riga duplicata

### 1.3 Fix Thread Safety

- [ ] **SOSTITUISCI Random con RandomNumberGenerator**
  - File: `Models/Order.cs:47-58`
  - Implementa: `System.Security.Cryptography.RandomNumberGenerator`
  ```csharp
  private static string GenerateOrderNumber()
  {
      var bytes = new byte[4];
      using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
      rng.GetBytes(bytes);
      return Math.Abs(BitConverter.ToInt32(bytes, 0) % 900000 + 100000).ToString();
  }
  ```
  - Applica anche a `Models/Order.cs:54-58` (TrackingNumber)
  - Applica a `Controllers/CinemaController.cs:66` (ProjectNumber)

### 1.4 Fix PDF Export

- [ ] **AGGIUNGI null check per logo**
  - File: `Controllers/ODGController.cs:217`
  ```csharp
  var logoPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "logo_pj_nuovo.png");
  ```

---

## Fase 2: Security Critical (Giorno 2)

### 2.1 Estrazione Credenziali

- [ ] **CIELE: Estrai DB credentials a environment variables**
  - File: `appsettings.json:3-4`
  - Rimuovi hardcoded, usa `${DB_CONNECTION}`
  - Crea `appsettings.Development.json` per develop
  - Aggiungi a `.gitignore` env file templates

- [ ] **CIELE: Estrai SMTP credentials a env var**
  - File: `appsettings.json:17-22`
  - Usa: `${EMAIL_PASSWORD}`, `${EMAIL_USERNAME}`

### 2.2 Protezione API

- [ ] **AGGIUNGI [Authorize] a OrderController API**
  - File: `Controllers/OrderController.cs:135,146`
  - Aggiungi `[Authorize]` attribute
  ```csharp
  [Authorize]
  [HttpGet("api/orders/states")]
  public async Task<IActionResult> GetOrderStates() { ... }
  
  [Authorize]
  [HttpGet("api/orders")]
  public async Task<IActionResult> GetOrders(...) { ... }
  ```

- [ ] **AGGIUNGI [Authorize] a CinemaController API**
  - File: `Controllers/CinemaController.cs:177,188`
  - Aggiungi `[Authorize]` attribute

### 2.3 Rate Limiting

- [ ] **AGGIUNGI rate limiting base**
  - File: `Program.cs`
  - Aggiungi dopo `builder.Services.AddControllersWithViews()`
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.RejectionPrefix = "429 - Too Many Requests: ";
      options.GlobalLimiter = PartitionedRateLimiter
          .AppendBucketPerMinuteLimiter<Tenant>(100);
  });
  app.UseRateLimiter();
  ```

---

## Fase 3: Refactor Base (Giorni 3-7)

### 3.1 Repository Layer

- [ ] **CREA IRepository generic**
  - Nuovo file: `Services/IRepository.cs`
  ```csharp
  public interface IRepository<T> where T : class
  {
      Task<T?> GetByIdAsync(int id);
      Task<IReadOnlyList<T>> GetAllAsync();
      Task<T> AddAsync(T entity);
      Task UpdateAsync(T entity);
      Task DeleteAsync(int id);
      Task<bool> ExistsAsync(int id);
  }
  ```

- [ ] **CREA Repository base implementation**
  - Nuovo file: `Services/Repository.cs`
  ```csharp
  public class Repository<T> : IRepository<T> where T : class
  {
      protected readonly AppDbContext _context;
      protected readonly DbSet<T> _dbSet;
      
      public Repository(AppDbContext context)
      {
          _context = context;
          _dbSet = context.Set<T>();
      }
      // Implementa tutti i metodi
  }
  ```

- [ ] **REFACTOR CinemaOrderRepository**
  - Nuovo file: `Services/CinemaOrderRepository.cs`
  - Estrai logic da `CinemaController`
  - Aggiungi metodi custom: `GetWithIncludeAsync`, `GetByStatusAsync`

- [ ] **REFACTOR ODGOrderRepository**
  - Nuovo file: `Services/ODGOrderRepository.cs`
  - Estrai logic da `ODGController`

- [ ] **REFACTOR RentalRequestRepository**
  - Nuovo file: `Services/RentalRequestRepository.cs`
  - Estrai logic da `RentalRequestUserController`, `RentalRequestAdminController`

### 3.2 DTOs

- [ ] **CREA CinemaOrderDto**
  - Nuovo file: `DTOs/CinemaOrderDto.cs`
  ```csharp
  public record CinemaOrderDto(
      int Id,
      string Title,
      string Director,
      string Producer,
      string Status,
      string ProjectNumber,
      DateTime CreatedAt,
      string CreatedBy
  );
  ```

- [ ] **CREA ODGOrderDto**
  - Nuovo file: `DTOs/ODGOrderDto.cs`

- [ ] **CREA RentalRequestDto**
  - Nuovo file: `DTOs/RentalRequestDto.cs`

- [ ] **CREA OrderDto**
  - Nuovo file: `DTOs/OrderDto.cs`

### 3.3 Map DTOs to Entities

- [ ] **AGGIUNGI AutoMapper**
  - File: `OrderTrackingApp.csproj`
  - Aggiungi: `<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />`

- [ ] **CONFIGURA AutoMapper profiles**
  - Nuovo file: `Mappings/MappingProfile.cs`

- [ ] **REFACTOR controller per usare DTOs**
  - Aggiorna `CinemaController`, `ODGController`, `OrderController`, `RentalRequestUserController`

---

## Fase 4: Quality Improvements (Giorni 8-14)

### 4.1 Validation

- [ ] **AGGIUNGI FluentValidation**
  - File: `OrderTrackingApp.csproj`
  - Aggiungi: `<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />`

- [ ] **CREA validators**
  - `Validators/CinemaOrderValidator.cs`
  - `Validators/ODGOrderValidator.cs`
  - `Validators/RentalRequestValidator.cs`

- [ ] **REGISTER validators in Program.cs**
  ```csharp
  builder.Services.AddFluentValidationAutoValidation();
  builder.Services.AddValidatorsFromAssemblyContaining<Program>();
  ```

### 4.2 Error Handling

- [ ] **CREA GlobalExceptionHandler**
  - Nuovo file: `Filters/GlobalExceptionHandler.cs`
  ```csharp
  public class GlobalExceptionHandler : IExceptionHandler
  {
      public async ValueTask<bool> TryHandleAsync(...) { ... }
  }
  ```

- [ ] **REGISTER handler in Program.cs**
  ```csharp
  builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
  ```

- [ ] **CREA ErrorResponseDto**
  - Nuovo file: `DTOs/ErrorResponseDto.cs`

### 4.3 Logging

- [ ] **AGGIUNGI Serilog**
  - File: `OrderTrackingApp.csproj`
  - Aggiungi Serilog packages
  - Configura in Program.cs

- [ ] **AGGIUNGI request logging middleware**
  - Log every request with correlation ID

### 4.4 PDF Consolidation

- [ ] **RIMUOVI iText7 e PdfSharp**
  - File: `OrderTrackingApp.csproj`
  - Rimuovi pacchetti non usati:
    - itext.bouncy-castle-adapter
    - itext.commons
    - itext.pdfhtml
    - itext7
    - itext7.pdfhtml
    - HtmlRenderer.PdfSharp

---

## Fase 5: Performance (Giorni 15-20)

### 5.1 Caching

- [ ] **CONFIGURA IMemoryCache**
  - Program.cs: `builder.Services.AddMemoryCache();`

- [ ] **IMPLEMENTA caching per lookup tables**
  - Categories lookup
  - Permission lookups

- [ ] **AGGIUNGI response caching per API readonly**
  ```csharp
  [HttpGet("api/categories")]
  [ResponseCache(Duration = 3600)]
  public async Task<IActionResult> GetCategories() { ... }
  ```

### 5.2 Pagination

- [ ] **IMPLEMENTA pagination in tutti i list endpoints**
  - Aggiungi `[FromQuery] int page = 1, int pageSize = 20`
  - Ritorna paginated result
  ```csharp
  public record PagedResult<T>(
      List<T> Items,
      int TotalCount,
      int Page,
      int PageSize,
      int TotalPages
  );
  ```

### 5.3 Database Indexes

- [ ] **CREA migration per indexes**
  ```csharp
  migrationBuilder.Sql(@"
      CREATE INDEX IX_CinemaOrders_Status ON CinemaOrders(Status);
      CREATE INDEX IX_CinemaOrders_ProjectNumber ON CinemaOrders(ProjectNumber);
      CREATE INDEX IX_ODGOrders_CinemaOrderId ON ODGOrders(CinemaOrderId);
      CREATE INDEX IX_RentalRequests_UserId ON RentalRequests(UserId);
      CREATE INDEX IX_RentalRequests_ProjectId ON RentalRequests(ProjectId);
      CREATE INDEX IX_Orders_TrackingNumber ON Orders(TrackingNumber);
  ");
  ```

### 5.4 Connection Pooling

- [ ] **CONFIGURA connection pooling**
  - Sì, già incluso in EF Core per default
  - Verifica string connection: `Pooling=true`

---

## Fase 6: Security Hardening (Giorni 21-28)

### 6.1 Authentication Policies

- [ ] **IMPLENTA password complexity**
  - Program.cs
  ```csharp
  builder.Services.Configure<IdentityOptions>(options =>
  {
      options.Password.RequireDigit = true;
      options.Password.RequireLowercase = true;
      options.Password.RequireUppercase = true;
      options.Password.RequireNonAlphanumeric = true;
      options.Password.RequiredLength = 8;
  });
  ```

- [ ] **IMPLENTA account lockout**
  ```csharp
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
  options.Lockout.MaxFailedAccessAttempts = 5;
  ```

- [ ] **IMPLEMENTA email confirmation required**
  ```csharp
  options.SignIn.RequireConfirmedEmail = true;
  ```

### 6.2 Audit Logging

- [ ] **IMPLEMENTA audit trail**
  - Nuovo file: `Services/AuditLogger.cs`
  - Log CRUD operations su entità sensibili

- [ ] **CREA AuditLog model**
  - Nuovo file: `Models/AuditLog.cs`

### 6.3 Data Protection

- [ ] **CONFIGURA data protection**
  - Già incluso in ASP.NET Core Identity
  - Verifica: `builder.Services.AddDataProtection()`

---

## Fase 7: Testing (Giorni 29-40)

### 7.1 Unit Tests

- [ ] **SETUP xUnit**
  - Aggiungi `<PackageReference Include="Microsoft.NET.Test.Sdk" />`
  - Crea `OrderTrackingApp.Tests.csproj`

- [ ] **TEST PermissionService**
  - Test `HasPermissionAsync`

- [ ] **TEST Repository**
  - Test CRUD operations

- [ ] **TEST Validators**
  - Test FluentValidation rules

### 7.2 Integration Tests

- [ ] **SETUP integration tests**
  - Usa `TestHost` o `WebApplicationFactory`

- [ ] **TEST auth flow**
  - Login, logout, authorization

- [ ] **TEST API endpoints**
  - CRUD operations su API

### 7.3 E2E Tests

- [ ] **SETUP Playwright**
  - Crea `tests/e2e` folder

- [ ] **TEST critical paths**
  - Create project, create ODG, export PDF
  - Create rental request flow

---

## Fase 8: Deployment Prep (Giorni 41-50)

### 8.1 Docker

- [ ] **CREA Dockerfile**
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
  WORKDIR /app
  EXPOSE 5000
  
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  WORKDIR /src
  COPY ["*.csproj", "./"]
  RUN dotnet restore
  COPY . .
  RUN dotnet publish -c Release -o /app/publish
  
  FROM base AS final
  WORKDIR /app
  COPY --from=/app/publish /app/publish .
  ENTRYPOINT ["dotnet", "OrderTrackingApp.dll"]
  ```

- [ ] **CREA docker-compose.yml**
  ```yaml
  version: '3.8'
  services:
    web:
      build: .
      ports:
        - "5000:5000"
      environment:
        - DB_CONNECTION=${DB_CONNECTION}
      volumes:
        - ./wwwroot:/app/wwwroot
  ```

### 8.2 CI/CD

- [ ] **CREA GitHub Actions workflow**
  - `.github/workflows/ci.yml`
  - Build, test, push to registry

- [ ] **SETUP deployment pipeline**
  - Auto-deploy on merge to main

---

## Checkpoint Finale

- [ ] **RUN all tests - pass**
- [ ] **Manual QA - complete**
- [ ] **Performance baseline - recorded**
- [ ] **Security audit - passed**
- [ ] **Documentation - updated**
- [ ] **Production deploy - successful**

---

## Note per Execution

### Task Dependencies

```
1.1 Quick Wins
  └─1.2 Security Critical
     └─2.1 Credenziali
       └─3.1 Repository Layer
         └─3.2 DTOs
           └─4.1 Validation
             └─4.2 Error Handling
               └─5.1 Caching
                 └─7.1 Unit Tests
                   └─8.1 Docker
```

### Estimated Time

| Fase | Giorni |
|------|--------|
| Fase 1: Quick Wins | 1 |
| Fase 2: Security | 1 |
| Fase 3: Refactor Base | 5 |
| Fase 4: Quality | 7 |
| Fase 5: Performance | 6 |
| Fase 6: Security | 8 |
| Fase 7: Testing | 12 |
| Fase 8: Deployment | 10 |
| **TOTALE** | **50** |