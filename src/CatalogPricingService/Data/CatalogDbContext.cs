using Microsoft.EntityFrameworkCore;
using CatalogPricingService.Models;
using System;

namespace CatalogPricingService.Data;

public class CatalogDbContext : DbContext
{
    public Guid CurrentTenantId { get; set; } // Discriminador inyectado por middleware

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Precio> Precios => Set<Precio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Precio>()
            .HasKey(p => new { p.ProductoId, p.SucursalId });

        // Filtros Globales (Global Query Filters) para Aislamiento Multi-Tenant
        modelBuilder.Entity<Categoria>().HasQueryFilter(c => c.TenantId == CurrentTenantId);
        modelBuilder.Entity<Producto>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        modelBuilder.Entity<Precio>().HasQueryFilter(p => p.TenantId == CurrentTenantId);

        // Indices
        modelBuilder.Entity<Categoria>().HasIndex(c => c.TenantId).HasDatabaseName("idx_categorias_tenant");
        modelBuilder.Entity<Producto>().HasIndex(p => p.TenantId).HasDatabaseName("idx_productos_tenant");
        modelBuilder.Entity<Producto>().HasIndex(p => new { p.TenantId, p.CodigoBarras }).IsUnique().HasDatabaseName("idx_productos_tenant_barcode");
        modelBuilder.Entity<Precio>().HasIndex(p => p.TenantId).HasDatabaseName("idx_precios_tenant");
    }
}
