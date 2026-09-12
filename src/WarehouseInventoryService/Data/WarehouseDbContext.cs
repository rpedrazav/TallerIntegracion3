using Microsoft.EntityFrameworkCore;
using WarehouseInventoryService.Models;

namespace WarehouseInventoryService.Data;

public class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<MovimientoStock> Movimientos => Set<MovimientoStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Llave compuesta para la tabla Stock
        modelBuilder.Entity<Stock>()
            .HasKey(s => new { s.ProductoId, s.SucursalId });

        // Aislamiento Multi-Tenant: Indices obligatorios por tenant_id (RNF-01)
        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.TenantId)
            .HasDatabaseName("idx_stock_tenant");

        modelBuilder.Entity<Lote>()
            .HasIndex(l => l.TenantId)
            .HasDatabaseName("idx_lotes_tenant");

        modelBuilder.Entity<MovimientoStock>()
            .HasIndex(m => m.TenantId)
            .HasDatabaseName("idx_movimientos_tenant");
    }
}
