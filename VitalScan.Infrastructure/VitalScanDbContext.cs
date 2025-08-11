
using global::VitalScan.Domain;
using Microsoft.EntityFrameworkCore;
using VitalScan.Domain;

namespace VitalScan.Infrastructure;

public class VitalScanDbContext : DbContext
{
    public VitalScanDbContext(DbContextOptions<VitalScanDbContext> options) : base(options) { }

    public DbSet<ServiceOffering> Services => Set<ServiceOffering>();
    public DbSet<Practitioner> Practitioners => Set<Practitioner>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceOffering>().Property(p => p.PriceAud).HasPrecision(10, 2);

        // Seed minimal data
        modelBuilder.Entity<ServiceOffering>().HasData(
            new ServiceOffering { Id = 1, Name = "Meta Hunter Scan", Description = "Full-body bioresonance scan.", DurationMinutes = 60, PriceAud = 129 },
            new ServiceOffering { Id = 2, Name = "Follow-up Session", Description = "Review & harmonisation.", DurationMinutes = 45, PriceAud = 89 }
        );

        modelBuilder.Entity<Practitioner>().HasData(
            new Practitioner { Id = 1, FullName = "Dan Lukic", Bio = "Bioresonance practitioner." },
            new Practitioner { Id = 2, FullName = "Alex Tan", Bio = "Holistic health specialist." }
        );

        modelBuilder.Entity<Clinic>().HasData(
            new Clinic { Id = 1, Name = "VitalScan Clinic", Address = "123 Collins St, Melbourne VIC", Timezone = "Australia/Melbourne" }
        );
    }
}

