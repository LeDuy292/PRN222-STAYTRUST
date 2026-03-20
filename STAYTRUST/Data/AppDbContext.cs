using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using STAYTRUST.Models;

namespace STAYTRUST.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<MeterReading> MeterReadings { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RentalContract> RentalContracts { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomImage> RoomImages { get; set; }

    public virtual DbSet<ServicePackage> ServicePackages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<FavoriteRoom> FavoriteRooms { get; set; }

    public virtual DbSet<Message> Messages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDD67121E076");

            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Room).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Room");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_User");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB5161DF631");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ElectricFee).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Month)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.RoomPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount)
                .HasComputedColumnSql("(([RoomPrice]+[ElectricFee])+[WaterFee])", false)
                .HasColumnType("decimal(14, 2)");
            entity.Property(e => e.WaterFee).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Contract).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Contract");
        });

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasKey(e => e.ReadingId).HasName("PK__MeterRea__C80F9C4ECAF9BDDB");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Month)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Room).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meter_Room");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38F8A16A87");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Invoice");
        });

        modelBuilder.Entity<RentalContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__RentalCo__C90D346954645650");

            entity.Property(e => e.Deposit).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Room).WithMany(p => p.RentalContracts)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contract_Room");

            entity.HasOne(d => d.Tenant).WithMany(p => p.RentalContracts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contract_Tenant");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Reports__D5BD48052DE2BAB4");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReportType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Reports)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Report_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939CBD986CD");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Image360Url).HasMaxLength(500);

            entity.HasOne(d => d.Landlord).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.LandlordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Landlord");
        });

        modelBuilder.Entity<RoomImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__RoomImag__7516F70C9B3B3DB3");

            entity.Property(e => e.Approved).HasDefaultValue(false);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);

            entity.HasOne(d => d.Room).WithMany(p => p.RoomImages)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Image_Room");
        });

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__ServiceP__322035CC0E5A35BF");

            entity.Property(e => e.PackageName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C125797F3");

            entity.HasIndex(e => e.Phone, "UQ__Users__5C7E359EEDFC2650").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105345C1EAA9C").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ__Users__C9F28456F9D25BCD").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__UserProf__290C88E4E66CB166");

            entity.HasIndex(e => e.UserId, "UQ__UserProf__1788CC4DBA4C710B").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IdentityNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Profile_User");
        });

        modelBuilder.Entity<FavoriteRoom>(entity =>
        {
            entity.HasKey(e => e.FavoriteId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.UserId, e.RoomId }).IsUnique();

            entity.HasOne(d => d.User).WithMany(p => p.FavoriteRooms)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Room).WithMany(p => p.FavoriteRooms)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Sender).WithMany(p => p.SentMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Receiver).WithMany(p => p.ReceivedMessages)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Room).WithMany(p => p.Messages)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
