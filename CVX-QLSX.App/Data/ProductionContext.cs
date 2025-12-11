using System.IO;
using Microsoft.EntityFrameworkCore;
using CVX_QLSX.App.Models;

namespace CVX_QLSX.App.Data;

/// <summary>
/// Entity Framework Core DbContext for the Production Management System.
/// </summary>
public class ProductionContext : DbContext
{
    public DbSet<ShiftSetting> ShiftSettings => Set<ShiftSetting>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();

    private readonly string _dbPath;

    public ProductionContext()
    {
        // Store database in AppData folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "CVX-QLSX");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "production.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ShiftSetting
        modelBuilder.Entity<ShiftSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShiftName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.ShiftName).IsUnique();
        });

        // Configure ProductionRecord
        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ProductId);
            entity.HasOne(e => e.Shift)
                  .WithMany(s => s.ProductionRecords)
                  .HasForeignKey(e => e.ShiftId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ProductionPlan
        modelBuilder.Entity<ProductionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QueueOrder);
            entity.HasIndex(e => e.Status);
        });

        // Seed default shifts
        modelBuilder.Entity<ShiftSetting>().HasData(
            new ShiftSetting 
            { 
                Id = 1, 
                ShiftName = "Ca Sáng", 
                StartTime = new TimeSpan(6, 0, 0), 
                EndTime = new TimeSpan(14, 0, 0), 
                TargetOutput = 1000,
                IsActive = true
            },
            new ShiftSetting 
            { 
                Id = 2, 
                ShiftName = "Ca Chiều", 
                StartTime = new TimeSpan(14, 0, 0), 
                EndTime = new TimeSpan(22, 0, 0), 
                TargetOutput = 1000,
                IsActive = true
            },
            new ShiftSetting 
            { 
                Id = 3, 
                ShiftName = "Ca Đêm", 
                StartTime = new TimeSpan(22, 0, 0), 
                EndTime = new TimeSpan(6, 0, 0), 
                TargetOutput = 800,
                IsActive = true
            }
        );
    }

    /// <summary>
    /// Ensures the database is created and up to date.
    /// </summary>
    public void Initialize()
    {
        Database.EnsureCreated();
    }
}
