using Microsoft.EntityFrameworkCore;
using CatalogPricingService.Models;

namespace CatalogPricingService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Precio> Precios => Set<Precio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Llave primaria compuesta para Precios: producto_id + sucursal_id
        modelBuilder.Entity<Precio>()
            .HasKey(p => new { p.ProductoId, p.SucursalId });

        // Aislamiento Multi-Tenant: Indices obligatorios por tenant_id (RNF-01)
        modelBuilder.Entity<Categoria>()
            .HasIndex(c => c.TenantId)
            .HasDatabaseName("idx_categorias_tenant");

        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.TenantId)
            .HasDatabaseName("idx_productos_tenant");

        // Codigo de barras unico dentro del mismo tenant
        modelBuilder.Entity<Producto>()
            .HasIndex(p => new { p.TenantId, p.CodigoBarras })
            .IsUnique()
            .HasDatabaseName("idx_productos_tenant_barcode");

        modelBuilder.Entity<Precio>()
            .HasIndex(p => p.TenantId)
            .HasDatabaseName("idx_precios_tenant");
    }
}
