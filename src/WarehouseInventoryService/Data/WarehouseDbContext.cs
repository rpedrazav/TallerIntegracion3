using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Models;
using System;

namespace WarehouseInventoryService.Data;

public class WarehouseDbContext : DbContext
{
    public Guid CurrentTenantId { get; set; } // Discriminador inyectado por middleware

    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<MovimientoStock> Movimientos => Set<MovimientoStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Stock>()
            .HasKey(s => new { s.ProductoId, s.SucursalId });

        // Filtros Globales (Global Query Filters) para Aislamiento Multi-Tenant
        modelBuilder.Entity<Stock>().HasQueryFilter(s => s.TenantId == CurrentTenantId);
        modelBuilder.Entity<Lote>().HasQueryFilter(l => l.TenantId == CurrentTenantId);
        modelBuilder.Entity<MovimientoStock>().HasQueryFilter(m => m.TenantId == CurrentTenantId);

        // Indices
        modelBuilder.Entity<Stock>().HasIndex(s => s.TenantId).HasDatabaseName("idx_stock_tenant");
        modelBuilder.Entity<Lote>().HasIndex(l => l.TenantId).HasDatabaseName("idx_lotes_tenant");
        modelBuilder.Entity<MovimientoStock>().HasIndex(m => m.TenantId).HasDatabaseName("idx_movimientos_tenant");
    }
}
