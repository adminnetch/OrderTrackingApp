# Domain Model

## Modelli Attuali

### 1. Entities

#### 1.1 User & Authorization

```csharp
public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string VisualName { get; set; }
    public string Role { get; set; }           // Admin, Manager, User, Progetti, Ordini, Esterno
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; }          // e.g. "Progetti.View"
    public string AppName { get; set; }       // e.g. "Progetti"
    public string? Description { get; set; }
}

public class UserPermission
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public int PermissionId { get; set; }
    public Permission Permission { get; set; }
}

public class ProjectPermission
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ProjectId { get; set; }
    public string PermissionName { get; set; }
}
```

#### 1.2 Cinema/Core

```csharp
public class CinemaOrder
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Director { get; set; }
    public string Producer { get; set; }
    public string AssProducer { get; set; }
    public string DoP { get; set; }
    public string Status { get; set; }                    // "Progetto Creato", etc.
    public string ProjectNumber { get; set; }
    public string DriveLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public List<ODGOrder> ODGs { get; set; }
    public List<Location> Locations { get; set; }
    public List<PianoDiLavorazione> PianiDiLavorazione { get; set; }
    public List<CentroCosto> CentriCosto { get; set; }
    public List<RentalRequest> RentalRequests { get; set; }
}

public class ODGOrder
{
    public int Id { get; set; }
    public string DayRec { get; set; }
    public string Film { get; set; }
    public string Regista { get; set; }
    public string Produttore { get; set; }
    public string Location { get; set; }
    public string Meteo { get; set; }
    public string SceneDaGirare { get; set; }
    public string Catering { get; set; }
    public string ProntiAGirare { get; set; }
    public string InizioRiprese { get; set; }
    public string PausaPranzo { get; set; }
    public string FineRiprese { get; set; }
    public string TermineLavorazione { get; set; }
    public string NoteProduzione { get; set; }
    public string NoteRegia { get; set; }
    public string InformazioniUtili { get; set; }
    public string MezziTecnici { get; set; }
    public string Costumi { get; set; }
    public string TruccoeCapelli { get; set; }
    public string SFX_VFX { get; set; }
    public string Stunt { get; set; }
    public string SpecialEquipment { get; set; }
    public int CinemaOrderId { get; set; }
    public CinemaOrder? CinemaOrder { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation
    public List<TroupeOrari> TroupeOrari { get; set; }
    public List<CastConvocazioni> CastConvocazioni { get; set; }
    public List<Trasporti> Trasporti { get; set; }
    public List<Contatto> Contatti { get; set; }
}
```

#### 1.3 ODG Subentities

```csharp
public class TroupeOrari
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Ruolo { get; set; }
    public string Orario { get; set; }
    public int ODGOrderId { get; set; }
    public ODGOrder? ODGOrder { get; set; }
}

public class CastConvocazioni
{
    public int Id { get; set; }
    public string Attore { get; set; }
    public string PickUp { get; set; }
    public string Costume { get; set; }
    public string Trucco { get; set; }
    public string Pronti { get; set; }
    public int ODGOrderId { get; set; }
    public ODGOrder? ODGOrder { get; set; }
}

public class Trasporti
{
    public int Id { get; set; }
    public string Auto { get; set; }
    public string Chi { get; set; }
    public string Dove { get; set; }
    public string Ora { get; set; }
    public int ODGOrderId { get; set; }
    public ODGOrder? ODGOrder { get; set; }
}

public class Contatto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Ruolo { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public int ODGOrderId { get; set; }
    public ODGOrder? ODGOrder { get; set; }
}
```

#### 1.4 Working Plan

```csharp
public class PianoDiLavorazione
{
    public int Id { get; set; }
    public string TitoloCortometraggio { get; set; }
    public string NomeProduzione { get; set; }
    public string Regista { get; set; }
    public string Produttore { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int CinemaOrderId { get; set; }
    public CinemaOrder CinemaOrder { get; set; }
    public List<GiornoRipresa> GiorniRipresa { get; set; }
}

public class GiornoRipresa
{
    public int Id { get; set; }
    public string Data { get; set; }
    public string Note { get; set; }
    public int PianoDiLavorazioneId { get; set; }
    public PianoDiLavorazione? PianoDiLavorazione { get; set; }
    public List<ScenaRipresa> Scene { get; set; }
    public List<AttoreRipresa> Attori { get; set; }
    public List<LocationRipresa> Locations { get; set; }
}

public class ScenaRipresa
{
    public int Id { get; set; }
    public string Numero { get; set; }
    public string Descrizione { get; set; }
    public int GiornoRipresaId { get; set; }
    public GiornoRipresa? GiornoRipresa { get; set; }
}

public class AttoreRipresa
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Ruolo { get; set; }
    public int GiornoRipresaId { get; set; }
    public GiornoRipresa? GiornoRipresa { get; set; }
}

public class LocationRipresa
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Note { get; set; }
    public int GiornoRipresaId { get; set; }
    public GiornoRipresa? GiornoRipresa { get; set; }
}
```

#### 1.5 Financial

```csharp
public class CentroCosto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public int CinemaOrderId { get; set; }
    public CinemaOrder? CinemaOrder { get; set; }
    public List<VoceSpesa> Spese { get; set; }
}

public class VoceSpesa
{
    public int Id { get; set; }
    public string Descrizione { get; set; }
    public decimal Importo { get; set; }
    public string Fornitore { get; set; }
    public DateTime Data { get; set; }
    public int CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }
}
```

#### 1.6 Rental System

```csharp
public enum RentalStatus
{
    Pending,
    Approved,
    MaterialDelivered,
    RejectedWithReason,
    RejectedWithoutReason,
    Closed,
    Archived
}

public class RentalRequest
{
    public int Id { get; set; }
    public string UserVisualName { get; set; }       // Denormalizzato da User
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int CinemaOrderId { get; set; }
    public CinemaOrder? CinemaOrder { get; set; }
    public string Type { get; set; }
    public string Client { get; set; }
    public RentalStatus Status { get; set; }
    public bool IsEditableByUser { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminModificationNote { get; set; }
    public string? ReceiptPdfPath { get; set; }
    public List<RentalRequestItem> RequestItems { get; set; }
}

public class RentalRequestItem
{
    public int Id { get; set; }
    public int RentalRequestId { get; set; }
    public RentalRequest RentalRequest { get; set; }
    public int RentalItemId { get; set; }
    public RentalItem RentalItem { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<RentalItem> Items { get; set; }
}

public class RentalItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public bool IsAvailable { get; set; }
    public string? PhotoPath { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
}

public class DamageReport
{
    public int Id { get; set; }
    public int RentalRequestId { get; set; }
    public string Description { get; set; }
    public DateTime ReportedAt { get; set; }
}
```

#### 1.7 Other

```csharp
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }        // 6 digits
    public string TrackingNumber { get; set; }     // 12 digits
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerAddress { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public string DriveLink { get; set; }
    public string? CustomerNotes { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? DocumentPath { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? ContactInfo { get; set; }
}

public class TroupeCastContact
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int CinemaOrderId { get; set; }
}

public class EmergencyContact
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public int TroupeCastContactId { get; set; }
}

public class ProjectFile
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public int CinemaOrderId { get; set; }
    public string UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
```

---

## Relazioni (ER Diagram)

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│      User       │       │   Permission    │       │UserPermission  │
├─────────────────┤       ├──────────���─���────┤       ├─────────────────┤
│ Id (PK)        │       │ Id (PK)         │       │ Id (PK)        │
│ UserName       │       │ Name (UK)       │       │ UserId (FK)    │
│ Email         │       │ AppName         │       │ PermissionId   │
│ Role          │       │ Description    │       │                │
└────────┬────────┘       └─────────────────┘       └─────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    CinemaOrder (1:N)                             │
├─────────────────────────────────────────────────────────────────────┤
│ Id (PK)                                                    │
│ Title                                                     │
│ Director                                                  │
│ Producer                                                 │
│ Status                                                   │
│ ProjectNumber                                             │
│ ...                                                      │
├─────────────────────────────────────────────────────────────┤
│         │        │         │        │         │        │         │
│         ▼        ▼         ▼         ▼         ▼         ▼         │
│    ┌──────┐ ┌────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐  │
│    │ ODG  │ │Location│ │  Piano  │ │Centro   │ │Rental   │  │
│    │Order │ │       │ │ Lav.   │ │Costo    │ │Request │  │
│    └──────┘ └────────┘ └──────────┘ └──────────┘ └────────┘  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ ODGOrder                                                   │
├─────────────────────────────────────────────────────────────────────┤
│ Id (PK)                                                    │
│ CinemaOrderId (FK)                                         │
├─────────────────────────────────────────────────────────────┤
│         │        │         │        │                         │
│         ▼        ▼         ▼         ▼                         │
│    ┌──────┐ ┌───────┐ ┌────────┐ ┌────────┐                 │
│    │Troupe│ │Cast   │ │Trasporti│ │Contatto│                 │
│    │Orari│ │Convoc.│ │        │ │       │                 │
│    └──────┘ └───────┘ └────────┘ └────────┘                 │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ RentalRequest                                              │
├─────────────────────────────────────────────────────────────────┤
│ Id (PK)                                                   │
│ CinemaOrderId (FK)                                         │
│ UserVisualName (denormalizzato)                             │
│ Status                                                   │
├─────────────────────────────────────────────────────────────────┤
│         ▼                                               │
│    ┌──────────────────────┐                             │
│    │ RentalRequestItem     │                             │
│    ├──────────────────────┤                             │
│    │ Id, RequestId, ItemId│                             │
│    └─────────┬────────────┘                             │
│              │                                        │
│              ▼                                        │
│    ┌──────────────────────┐                         │
│    │   RentalItem        │                         │
│    ├──────────────────────┤                         │
│    │ Id                  │                         │
│    │ CategoryId (FK)     │                         │
│    └─────────┬───────────┘                         │
│              │                                    │
│              ▼                                    │
│    ┌─────────────────────┐                       │
│    │   Category         │                       │
│    ├─────────────────────┤                       │
│    │ Id, Name           │                       │
│    └─────────────────────┘                       │
└─────────────────────────────────────────────────┘
```

---

## Schema Database Corrente

### Tabelle Principali

| Tabella | Note |
|--------|------|
| `AspNetUsers` | Identity |
| `AspNetUserRoles` | Identity |
| `AspNetRoleClaims` | Identity |
| `AspNetUserLogins` | Identity |
| `AspNetUserTokens` | Identity |
| `Permissions` | Custom |
| `UserPermissions` | Join table |
| `ProjectPermissions` | Permission per progetto |
| `CinemaOrders` | Progetti cinema |
| `ODGOrders` | Ordini del giorno |
| `TroupeOrari` | Orari troupe |
| `CastConvocazioni` | Convocazioni |
| `Trasporti` | Trasporti |
| `Contatto` | Contatti ODG |
| `PianiDiLavorazione` | Piani |
| `GiorniRipresa` | Giorni |
| `SceneRipresa` | Scene |
| `AttoriRipresa` | Attori |
| `LocationsRipresa` | Location ripresa |
| `CentroCosto` | Centri costo |
| `VociSpesa` | Spese |
| `RentalRequests` | Richieste noleggio |
| `RentalRequestItems` | Items richiesti |
| `Categories` | Categorie |
| `RentalItems` | Attrezzature |
| `DamageReports` | Danni |
| `Orders` | Ordini tracking |
| `Locations` | Location |
| `TroupeCastContacts` | Contatti troupe |
| `EmergencyContacts` | Contatti emergenza |
| `ProjectFiles` | File |

### Issues Schema

1. **Denormalizzazione**: `UserVisualName` in `RentalRequest` invece di foreign key a `User`
2. **No FK esplicite**: Alcune relazioni gestite da EF senza FK nel DB
3. **No indexes**: Query performance potenzialmente poor
4. **Mixed naming**: Alcune tabelle con nomi italiani (`GiorniRipresa`), altre english

---

## Schema Ideale Suggerito

### 1.建议 Naming Convention

| Attuale | Suggerito |
|--------|----------|
| CinemaOrder | Project |
| ODGOrder | DailyOrder |
| GiornoRipresa | ShootingDay |
| CentroCosto | CostCenter |
| VoceSpesa | ExpenseItem |
| TroupeOrari | CrewMember |

### 2. Schema Normalizzato

```csharp
public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }           // ex CinemaOrder.Title
    public int ProjectNumber { get; set; }      // Auto-generated
    // ...other fields

    // Add explicit FKs
    public int CreatedByUserId { get; set; }     // NEW -> UserId
    public User CreatedByUser { get; set; }     // Navigation
}

// Fix RentalRequest denormalization
public class RentalRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }              // FIX: Foreign key
    public User User { get; set; }              // FIX: Navigation
    // Remove: public string UserVisualName
    
    public int ProjectId { get; set; }          // Rename CinemaOrderId
    public Project Project { get; set; }
    
    // ...other fields
}
```

### 3.建议 Indici

```sql
CREATE INDEX IX_Project_Status ON Projects(Status);
CREATE INDEX IX_Project_ProjectNumber ON Projects(ProjectNumber);
CREATE INDEX IX_DailyOrder_ProjectId ON DailyOrders(ProjectId);
CREATE INDEX IX_ShootingDay_ProjectId ON ShootingDays(ProjectId);
CREATE INDEX IX_ExpenseItem_CostCenterId ON ExpenseItems(CostCenterId);
CREATE INDEX IX_RentalRequest_UserId ON RentalRequests(UserId);
CREATE INDEX IX_RentalRequest_ProjectId ON RentalRequests(ProjectId);
CREATE INDEX IX_RentalRequest_Status ON RentalRequests(Status);
CREATE INDEX IX_TrackingNumber ON Orders(TrackingNumber);
```

### 4.建议 Migrashed Schema

Consolidare 20+ migrazioni inSchema iniziale + 2-3 incremental migrations.

### 5.建议 Auditing

```csharp
public interface IAuditable
{
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

Implementare tramite shadow properties o base entity.

---

## Riepilogo Problemi Schema

| Problema | Impatto | Priority |
|---------|---------|----------|
| Denormalizzazione UserVisualName | Data integrity | HIGH |
| No Foreign Keys esplicite | Cascade delete issues | MEDIUM |
| No performance indexes | Query lente | MEDIUM |
| Mixed naming (IT/EN) | Confusione | LOW |
| No auditing | Compliance | MEDIUM |