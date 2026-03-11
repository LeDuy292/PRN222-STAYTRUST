using Microsoft.EntityFrameworkCore;
using STAYTRUST.Models;

namespace STAYTRUST.Data
{
    public class StayTrustDbContext : DbContext
    {
        public StayTrustDbContext(DbContextOptions<StayTrustDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }
        public DbSet<RentalContract> RentalContracts { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure computed column for TotalAmount in Invoice
            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasComputedColumnSql("[RoomPrice] + [ElectricFee] + [WaterFee]");

            // Configure relationships if needed (EF Core handles most by convention)
            
            // User -> UserProfile (1-to-1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);

            // Other relationships are defined via [ForeignKey] attributes in Models,
            // but we can also set them here for clarity if needed.
        }
    }
}
