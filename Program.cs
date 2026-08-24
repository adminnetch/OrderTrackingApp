using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Filters;
using OrderTrackingApp.Middleware;
using OrderTrackingApp.Models;
using QuestPDF.Infrastructure;
using OrderTrackingApp.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

var builder = WebApplication.CreateBuilder(args);

// ✅ CONFIGURA CARTELLA LOGS
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);

// ✅ CONFIGURA SERILOG CON FILE ROLLING
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(logsDir, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ✅ CONFIGURA PDF
QuestPDF.Settings.License = LicenseType.Community;

// ✅ CONFIGURA MVC
builder.Services.AddControllersWithViews();

// ✅ CONFIGURA RATE LIMITING (ASP.NET 8 native)
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(policyName: "Default", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(10);
    });
});

// ✅ CONFIGURA DATABASE (default: MySQL, can be overridden by Phase A setup)
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//         new MySqlServerVersion(new Version(10, 6))));

// Phase A: Register setup services
builder.Services.AddScoped<IInstallationStateService, InstallationStateService>();
builder.Services.AddScoped<ISqliteDatabaseProvider, SqliteDatabaseProvider>();
builder.Services.AddScoped<ISuperadminService, SuperadminService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Dynamic DB provider: SQLite for Phase A (first run), MySQL for production
var dbProvider = builder.Configuration.GetValue<string>("Database:Provider", "sqlite");
if (dbProvider == "sqlite")
{
    var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
    if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
    var sqlitePath = Path.Combine(dataDir, "ordertracking.db");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={sqlitePath}"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(10, 6))));
}

// ✅ CONFIGURA IDENTITY
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Lockout configuration per brute force protection
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// subito dopo services.AddIdentity<...>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// ✅ CONFIGURA COOKIE DI AUTENTICAZIONE
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/accessdenied";
    options.LogoutPath = "/account/logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".OrderTrackingApp.Auth";
});

// ✅ CONFIGURA COOKIE POLICY
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.OnAppendCookie = context => Log.Information("Cookie appended: {CookieName}", context.CookieName);
    options.OnDeleteCookie = context => Log.Information("Cookie deleted: {CookieName}", context.CookieName);
});

// ✅ REGISTRAZIONE DEL SERVIZIO PROJECT STORAGE
builder.Services.AddSingleton<ProjectStorageService>();
// in Program.cs, prima di .Build()
builder.WebHost.ConfigureKestrel(options =>
{
    // ad esempio 50 MB
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});
builder.Services.AddScoped<IEmailService, EmailService>();




var app = builder.Build();

// ✅ INIZIALIZZAZIONE DATABASE
// Per SQLite: EnsureCreated (tabelle vanilla)
// Per MySQL: Migrate (usa migration MySQL-specifiche)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var dbType = builder.Configuration.GetValue<string>("Database:Provider", "sqlite");
        
        if (dbType == "sqlite")
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // ✅ CREA I RUOLI BASE (SuperAdmin + funzionali)
        string[] roles = { "SuperAdmin", "Admin", "Manager", "User", "Progetti", "Ordini", "Esterno" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Console.WriteLine($"Ruolo {role} creato.");
            }
        }

        // ✅ CREA I PERMESSI BASE
        var permissionList = new List<Permission>
        {
            new() { Name = "ODG.View", AppName = "ODG", Description = "Vedere elenco ODG" },
            new() { Name = "ODG.Create", AppName = "ODG", Description = "Creare ODG" },
            new() { Name = "ODG.Edit", AppName = "ODG", Description = "Modificare ODG" },
            new() { Name = "ODG.Delete", AppName = "ODG", Description = "Eliminare ODG" },
            new() { Name = "ODG.Export", AppName = "ODG", Description = "Esportare ODG" },


            new() { Name = "Finanze.View", AppName = "Finanze", Description = "Visualizzare spese" },
            new() { Name = "Finanze.Details", AppName = "Finanze", Description = "Visualizzare dettaglio spese" },
            new() { Name = "Finanze.Create", AppName = "Finanze", Description = "Aggiungere spesa" },
            new() { Name = "Finanze.Edit", AppName = "Finanze", Description = "Modificare spesa" },
            new() { Name = "Finanze.Delete", AppName = "Finanze", Description = "Eliminare spesa" },
            new() { Name = "Finanze.Download", AppName = "Finanze", Description = "Download Scontrino / Ricevuta" },
            new() { Name = "Finanze.Export", AppName = "Finanze", Description = "Esportare rendiconto" },


            new() { Name = "Piani.View", AppName = "Piani", Description = "Visualizzare piani" },
            new() { Name = "Piani.Create", AppName = "Piani", Description = "Creare piano" },
            new() { Name = "Piani.Edit", AppName = "Piani", Description = "Modificare piano" },
            new() { Name = "Piani.Delete", AppName = "Piani", Description = "Eliminare piano" },


            new() { Name = "Progetti.View", AppName = "Progetti", Description = "Visualizzare progetti" },
            new() { Name = "Progetti.Create", AppName = "Progetti", Description = "Creare progetto" },
            new() { Name = "Progetti.Edit", AppName = "Progetti", Description = "Modificare progetto" },
            new() { Name = "Progetti.Delete", AppName = "Progetti", Description = "Eliminare progetto" },
            new() { Name = "Progetti.Dashboard", AppName = "Progetti", Description = "Accesso dashboard progetto" },


            new() { Name = "Contatti.View", AppName = "Contatti", Description = "Visualizza i contatti del progetto" },
            new() { Name = "Contatti.Create", AppName = "Contatti", Description = "Crea i contatti del progetto" },
            new() { Name = "Contatti.Details", AppName = "Contatti", Description = "Visualizza i dettagli dei contatti del progetto" },
            new() { Name = "Contatti.Edit", AppName = "Contatti", Description = "Modificare i contatti del progetto" },
            new() { Name = "Contatti.Delete", AppName = "Contatti", Description = "Elimina i contatti del progetto" },
            new() { Name = "Contatti.Export", AppName = "Contatti", Description = "Esporta i contatti del progetto" },


            new() { Name = "File.FileRead", AppName = "FileManager", Description = "Leggere file progetto" },
            new() { Name = "File.FileUpload", AppName = "FileManager", Description = "Caricare file progetto" },
            new() { Name = "File.FileRename", AppName = "FileManager", Description = "Rinominare file progetto" },
            new() { Name = "File.Download", AppName = "FileManager", Description = "Scaricare file e cartelle progetto" },
            new() { Name = "File.FileDelete", AppName = "FileManager", Description = "Cancellare file progetto" },
            new() { Name = "File.Folder.Create", AppName = "FileManager", Description = "Creare cartelle progetto" },
            new() { Name = "File.Folder.Delete", AppName = "FileManager", Description = "Eliminare cartelle progetto" },
            new() { Name = "File.Folder.Rename", AppName = "FileManager", Description = "Rinominare cartelle progetto" },


            new() { Name = "Ordini.View", AppName = "Ordini", Description = "Visualizzare ordini" },
            new() { Name = "Ordini.Create", AppName = "Ordini", Description = "Creare ordine" },
            new() { Name = "Ordini.Edit", AppName = "Ordini", Description = "Modificare ordine" },
            new() { Name = "Ordini.Delete", AppName = "Ordini", Description = "Eliminare ordine" },
            new() { Name = "Ordini.Details", AppName = "Ordini", Description = "Vedere dettaglio ordine" },


            new() { Name = "Location.View", AppName = "Location", Description = "Visualizzare location" },
            new() { Name = "Location.Details", AppName = "Location", Description = "Visualizzare dettagli location" },
            new() { Name = "Location.Create", AppName = "Location", Description = "Aggiungere location" },
            new() { Name = "Location.Edit", AppName = "Location", Description = "Modificare location" },
            new() { Name = "Location.Delete", AppName = "Location", Description = "Eliminare location" },


            new() { Name = "Admin.Access", AppName = "Admin", Description = "Accesso area amministrazione" },


            new() { Name = "Home.Index.Admin", AppName = "Home", Description = "Dashboard Admin" },
            new() { Name = "Home.Index.User", AppName = "Home", Description = "Dashboard Utente" },
            new() { Name = "Home.Index.External", AppName = "Home", Description = "Dashboard Esterno" },
            new() { Name = "Home.Index.Manager", AppName = "Home", Description = "Dashboard Manager" },
            new() { Name = "Home.Index.Projects", AppName = "Home", Description = "Dashboard Progetti" },
            new() { Name = "Home.Index.Orders", AppName = "Home", Description = "Dashboard Ordini" },
            new() { Name = "Home.Index.Public", AppName = "Home", Description = "Accesso pubblico" },
            new() { Name = "Home.Privacy", AppName = "Home", Description = "Visualizzare privacy" },


            new() { Name = "Account.Profile", AppName = "Account", Description = "Visualizzare profilo" },
            new() { Name = "Account.UpdateProfile", AppName = "Account", Description = "Modificare profilo" },
            new() { Name = "Account.ChangePassword", AppName = "Account", Description = "Modificare password" },

        // Permessi Noleggi User

            new() { Name = "Rental.User.Index", AppName = "Rental", Description = "Visualizza Noleggi" }, // User e admin Visione
            new() { Name = "Rental.User.Create", AppName = "Rental", Description = "Crea Noleggi" }, // User e admin Visione
            new() { Name = "Rental.User.Details", AppName = "Rental", Description = "Dettaglio Noleggio" }, // User e Admin Dettaglio
            new() { Name = "Rental.User.Edit", AppName = "Rental", Description = "Modifica Noleggio" }, // User e Admin Modifica
            new() { Name = "Rental.User.Delete", AppName = "Rental", Description = "Elimina Noleggio" }, // SuperAdmin, Delete
            new() { Name = "Rental.User.ReportDamage", AppName = "Rental", Description = "Notifica un Danno" }, // Notifica Danno
            new() { Name = "Rental.User.ExportPdf", AppName = "Rental", Description = "Esporta PDF" }, // Esporta PDF

        // Permessi Noleggi Admin    

            new() { Name = "Rental.Admin", AppName = "Rental Admin", Description = "Amministratore del Rental" }, // SuperAdmin
            
        };

        foreach (var perm in permissionList)
        {
            if (!context.Permessi.Any(p => p.Name == perm.Name))
            {
                context.Permessi.Add(perm);
                Console.WriteLine($"Permesso '{perm.Name}' creato.");
            }
        }

        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore durante l'inizializzazione del database: {ex.Message}");
    }
}

// ✅ MIDDLEWARE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security headers middleware (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self';");

    await next();
});

app.UseRouting();

app.UseCookiePolicy(); // Cookie policy middleware

app.UseRateLimiter();

// Phase A: First Run Detection Middleware
app.UseFirstRunDetection();

app.UseAuthentication();
app.UseAuthorization();

// ✅ ROUTING DEFAULT
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();