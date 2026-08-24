# Project Summary

## 1. Scopo dell'Applicazione

**OrderTrackingApp** è un sistema web di gestione perproduzioni cinematografiche e noleggio attrezzature. L'applicazione permette di:

- **Gestione Progetti Cinema**: Creare e tracciare progetti cinematografici (CinemaOrder)
- **Ordini del Giorno (ODG)**: Generare e gestire daily orders per le riprese
- **Piani di Lavorazione**: Pianificare giornate di ripresa con scene, attori e location
- **Gestione Ordini**: Tracciamento ordini clienti con numeri di tracking
- **Noleggio Attrezzature**: Sistema rental per attrezzature cinematografiche con richieste, categorie e segnalazione danni
- **Gestione Location**: Catalogo location per riprese
- **Amministrazione Utenti**: Sistema di autenticazione e autorizzazione basato su permessi granulari

## 2. Architettura Attuale

### Pattern Implementati
- **MVC** (Model-View-Controller) con ASP.NET Core
- **Identity** per autenticazione/autorizzazione
- **EF Core** con MySQL per persistenza
- **Service Layer** per business logic
- **Attribute-based Authorization** con filtro custom HasPermission

### Struttura Directory
```
OrderTrackingApp/
├── Controllers/       # 14 controller
├── Models/          # 26 entity models
├── Services/        # 5 servizi (Permission, Email, ProjectStorage)
├── Filters/         # 1 filtro autorizzazione custom
├── Views/           # ~60 view
├── Migrations/      # 20+ migrazioni EF
└── wwwroot/        # Asset statici
```

## 3. Stack Tecnologico e Versioni

| Componente | Versione |
|-----------|---------|
| .NET | 8.0 |
| ASP.NET Core | 8.0 |
| Entity Framework Core | 6.0.36 |
| MySQL (Pomelo) | 6.0.2 |
| QuestPDF | 2025.4.0 |
| BCrypt.Net | 4.0.3 |
| MailKit | 4.12.0 |
| HtmlAgilityPack | 1.12.1 |
| SkiaSharp | 3.119.0 |
| EPPlus | 8.0.2 |

### Dipendenze PDF (ridondanti - Da consolidare)
- QuestPDF
- iText7 + pdfhtml
- HtmlRenderer.PdfSharp

## 4. Moduli/Funzionalità Esistenti

### 4.1 Authentication & Authorization
- Login/Logout con Identity
- Ruoli: Admin, Manager, User, Progetti, Ordini, Esterno
- Sistema permessi granulari (Permission table)
- Permessi a livello di progetto (ProjectPermission)
- Filtro HasPermission custom

### 4.2 Cinema Projects
| Entità | Descrizione |
|--------|-----------|
| CinemaOrder | Progetto cinematografico principale |
| ODGOrder | Ordine del giorno |
| TroupeOrari | Orari troupe |
| CastConvocazioni | Convocazioni attori |
| Trasporti | Mezzi di trasporto |
| Contatto | Contatti progetto |

### 4.3 Working Plans
| Entità | Descrizione |
|--------|-----------|
| PianoDiLavorazione | Piano lavorazione |
| GiornoRipresa | Giorno di ripresa |
| ScenaRipresa | Scena |
| AttoreRipresa | Attore in scena |
| LocationRipresa | Location ripresa |
| CentroCosto | Centro costo |
| VoceSpesa | Voce di spesa |

### 4.4 Rental System
| Entità | Descrizione |
|--------|-----------|
| Category | Categoria attrezzatura |
| RentalItem | Singolo oggetto |
| RentalRequest | Richiesta noleggio |
| RentalRequestItem | Item richiesto |
| DamageReport | Segnalazione danno |

### 4.5 Other
| Entità | Descrizione |
|--------|-----------|
| Order | Ordine generico tracking |
| Location | Location riprese |
| TroupeCastContact | Contatto troupe |
| EmergencyContact | Contatto emergenza |

## 5. Flusso Dati e Dipendenze

### 5.1 Dipendenze tra Entità
```
CinemaOrder
├── ODGOrder (1:N)
│   ├── TroupeOrari
│   ├── CastConvocazioni
│   ├── Trasporti
│   └── Contatto
├── Location
├── PianoDiLavorazione (1:N)
│   └── GiornoRipresa (1:N)
│       ├── ScenaRipresa
│       ├── AttoreRipresa
│       └── LocationRipresa
├── CentroCosto (1:N)
│   └── VoceSpesa
└── RentalRequest (1:N)
    └── RentalRequestItem (N:1 RentalItem)
```

### 5.2 Flusso Autorizzazione
```
User Login
    ↓
HasPermission Attribute
    ↓
PermissionService.HasPermissionAsync()
    ↓
[Check Global Permission]
    OR
[Check Project Permission]
    ↓
Allow/Deny
```

### 5.3 Database
- MySQL 10.6 su server 192.168.1.50
- Database: OrderTrackingApp
- Credenziali in appsettings.json (hardcoded - SECURITY ISSUE)

## 6. Configurazione

### Connection Strings
```json
Server: 192.168.1.50:3306
Database: OrderTrackingApp
User: prjt-ota
Password: 1234assoKAPPA
```

### Project Storage
```json
RootPath: /mnt/archivio-progetti
```

### Email
```json
SMTP: mail.projectcesare.ch:465
From: test@projectcesare.ch
```

## 7. Note di Deploy

- **Runtime**: Linux (Ubuntu/Debian)
- **Web Server**: Kestrel integrato
- **Upload Limit**: 50MB
- **Session**: 60 minuti sliding expiration
- **HTTPS**: Abilitato (redirect da HTTP)