using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Models
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ✅ AppInstallation for Setup Wizard
        public DbSet<AppInstallation> AppInstallations { get; set; } = null!;

        // ✅ DbSet principali
        public DbSet<ODGOrder> ODGOrders { get; set; } = null!;
        public DbSet<TroupeOrari> TroupeOrari { get; set; } = null!;
        public DbSet<CastConvocazioni> CastConvocazioni { get; set; } = null!;
        public DbSet<Trasporti> Trasporti { get; set; } = null!;
        public DbSet<Contatto> Contatti { get; set; } = null!;
        public DbSet<CinemaOrder> CinemaOrders { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

        // ✅ Piano di Lavorazione - Nuova struttura
        public DbSet<PianoDiLavorazione> PianiDiLavorazione { get; set; } = null!;
        public DbSet<GiornoRipresa> GiorniRipresa { get; set; } = null!;
        public DbSet<ScenaRipresa> SceneRipresa { get; set; } = null!;
        public DbSet<AttoreRipresa> AttoriRipresa { get; set; } = null!;
        public DbSet<LocationRipresa> LocationsRipresa { get; set; } = null!;
        public DbSet<CentroCosto> CentriCosto { get; set; } = null!;
        public DbSet<VoceSpesa> VociSpesa { get; set; } = null!;
        public DbSet<TroupeCastContact> TroupeCastContacts { get; set; } = null!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
        public DbSet<Permission> Permessi { get; set; } = null!;
        public DbSet<UserPermission> PermessiUtente { get; set; } = null!;
        public DbSet<ProjectPermission> ProjectPermissions { get; set; } = null!;
        public DbSet<ProjectFile> ProjectFiles { get; set; } = null!;
        public DbSet<Category>           Categories          { get; set; } = null!;
        public DbSet<RentalItem>         RentalItems         { get; set; } = null!;
        public DbSet<RentalRequest>      RentalRequests      { get; set; } = null!;
        public DbSet<RentalRequestItem>  RentalRequestItems  { get; set; } = null!;
        public DbSet<DamageReport>  DamageReport  { get; set; } = null!;
        
        // ✅ Audit Log
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;






        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Relazioni ODG
            modelBuilder.Entity<ODGOrder>()
                .HasMany(o => o.TroupeOrari)
                .WithOne(t => t.ODGOrder)
                .HasForeignKey(t => t.ODGOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ODGOrder>()
                .HasMany(o => o.CastConvocazioni)
                .WithOne(c => c.ODGOrder)
                .HasForeignKey(c => c.ODGOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ODGOrder>()
                .HasMany(o => o.Trasporti)
                .WithOne(t => t.ODGOrder)
                .HasForeignKey(t => t.ODGOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ODGOrder>()
                .HasMany(o => o.Contatti)
                .WithOne(c => c.ODGOrder)
                .HasForeignKey(c => c.ODGOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Relazione CinemaOrder -> PianiDiLavorazione (1:N)
            modelBuilder.Entity<CinemaOrder>()
                .HasMany(c => c.PianiDiLavorazione)
                .WithOne(p => p.CinemaOrder)
                .HasForeignKey(p => p.CinemaOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Relazione PianoDiLavorazione -> GiorniRipresa (1:N)
            modelBuilder.Entity<PianoDiLavorazione>()
                .HasMany(p => p.GiorniRipresa)
                .WithOne(g => g.PianoDiLavorazione)
                .HasForeignKey(g => g.PianoDiLavorazioneId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Relazione GiornoRipresa -> SceneRipresa (1:N)
            modelBuilder.Entity<GiornoRipresa>()
                .HasMany(g => g.Scene)
                .WithOne(s => s.GiornoRipresa)
                .HasForeignKey(s => s.GiornoRipresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Relazione GiornoRipresa -> AttoriRipresa (1:N)
            modelBuilder.Entity<GiornoRipresa>()
                .HasMany(g => g.Attori)
                .WithOne(a => a.GiornoRipresa)
                .HasForeignKey(a => a.GiornoRipresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Relazione GiornoRipresa -> LocationsRipresa (1:N)
            modelBuilder.Entity<GiornoRipresa>()
                .HasMany(g => g.Locations)
                .WithOne(l => l.GiornoRipresa)
                .HasForeignKey(l => l.GiornoRipresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relazione EmergencyContact -> TroupeCastContact
            modelBuilder.Entity<TroupeCastContact>()
            .HasOne(c => c.EmergencyContact)
            .WithOne(e => e.TroupeCastContact)
            .HasForeignKey<EmergencyContact>(e => e.TroupeCastContactId)
            .OnDelete(DeleteBehavior.Cascade);

            // ✅ Chiavi primarie
            modelBuilder.Entity<TroupeOrari>().HasKey(t => t.Id);
            modelBuilder.Entity<CastConvocazioni>().HasKey(c => c.Id);
            modelBuilder.Entity<Trasporti>().HasKey(t => t.Id);
            modelBuilder.Entity<Contatto>().HasKey(c => c.Id);
            modelBuilder.Entity<Location>().HasKey(l => l.Id);
            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<PianoDiLavorazione>().HasKey(p => p.Id);
            modelBuilder.Entity<GiornoRipresa>().HasKey(g => g.Id);
            modelBuilder.Entity<ScenaRipresa>().HasKey(s => s.Id);
            modelBuilder.Entity<AttoreRipresa>().HasKey(a => a.Id);
            modelBuilder.Entity<LocationRipresa>().HasKey(l => l.Id);

            modelBuilder.Entity<CinemaOrder>()
    .HasMany(co => co.CentriCosto)
    .WithOne(cc => cc.CinemaOrder)
    .HasForeignKey(cc => cc.CinemaOrderId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CentroCosto>()
                .HasMany(cc => cc.Spese)
                .WithOne(s => s.CentroCosto)
                .HasForeignKey(s => s.CentroCostoId)
                .OnDelete(DeleteBehavior.Cascade);

            // —————————————————————————————
            // 🔹 Configurazioni per il Noleggio

            // Category → RentalItem (1:N)
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Category)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // RentalItem → RentalRequestItem (1:N)
            modelBuilder.Entity<RentalItem>()
                .HasMany<RentalRequestItem>()
                .WithOne(ri => ri.RentalItem)
                .HasForeignKey(ri => ri.RentalItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // RentalRequest → RentalRequestItem (1:N)
            modelBuilder.Entity<RentalRequest>()
                .HasMany(r => r.RequestItems)
                .WithOne(ri => ri.RentalRequest)
                .HasForeignKey(ri => ri.RentalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // RentalRequest → CinemaOrder (N:1) con back-reference
            modelBuilder.Entity<RentalRequest>()
                .HasOne(r => r.CinemaOrder)
                .WithMany(co => co.RentalRequests)
                .HasForeignKey(r => r.CinemaOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Categorie AdminItem Rental
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Fotografia" },
                new Category { Id = 2, Name = "Audio" },
                new Category { Id = 3, Name = "Luci" },
                new Category { Id = 4, Name = "Grip" },
                new Category { Id = 5, Name = "Extra" }
    );

        }
    }
}
