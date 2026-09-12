using Microsoft.EntityFrameworkCore;
using SupplyChainService.Models;

namespace SupplyChainService.Data;

/// <summary>
/// DbContext de MS-6: Supply Chain &amp; Import Service.
/// 
/// Implementa el patrón Multi-Tenant mediante QueryFilters globales:
/// cada query que toque una tabla con tenant_id (directo o heredado vía
/// navegación) se filtra automáticamente por el CurrentTenantId inyectado
/// por el TenantMiddleware desde el JWT.
/// </summary>
public class SupplyChainDbContext : DbContext
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core aplica este valor en todos los filtros globales.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public SupplyChainDbContext(DbContextOptions<SupplyChainDbContext> options) : base(options) { }

    // DbSets (tablas)
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraItem> OrdenCompraItems => Set<OrdenCompraItem>();
    public DbSet<Envio> Envios => Set<Envio>();
    public DbSet<CostoLandedHistorico> CostosLandedHistorico => Set<CostoLandedHistorico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Proveedor ───────────────────────────────────────────────────────
        modelBuilder.Entity<Proveedor>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            e.Property(p => p.PaisOrigen).IsRequired().HasMaxLength(2);
            e.Property(p => p.Email).HasMaxLength(255);
            e.Property(p => p.Telefono).HasMaxLength(20);
            e.Property(p => p.MonedaFacturacion).IsRequired().HasMaxLength(3);
            e.HasIndex(p => p.TenantId);

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            e.HasQueryFilter(p => p.TenantId == CurrentTenantId);
        });

        // ── OrdenCompra ─────────────────────────────────────────────────────
        modelBuilder.Entity<OrdenCompra>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Moneda).IsRequired().HasMaxLength(3);
            e.Property(o => o.SubtotalProveedor).HasPrecision(18, 2);
            e.Property(o => o.Flete).HasPrecision(18, 2);
            e.Property(o => o.Seguro).HasPrecision(18, 2);
            e.Property(o => o.Aranceles).HasPrecision(18, 2);
            e.Property(o => o.CostoLanded).HasPrecision(18, 2);
            e.HasIndex(o => o.TenantId);
            e.HasIndex(o => o.SucursalDestinoId);

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            e.HasQueryFilter(o => o.TenantId == CurrentTenantId);

            e.HasOne(o => o.Proveedor)
             .WithMany(p => p.OrdenesCompra)
             .HasForeignKey(o => o.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict); // "emite (RESTRICT)" según ERD
        });

        // ── OrdenCompraItem ─────────────────────────────────────────────────
        modelBuilder.Entity<OrdenCompraItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.PrecioUnitario).HasPrecision(18, 2);
            e.Property(i => i.Subtotal).HasPrecision(18, 2);
            e.HasIndex(i => i.ProductoId);

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación (no tiene tenant_id propio)
            e.HasQueryFilter(i => i.OrdenCompra.TenantId == CurrentTenantId);

            e.HasOne(i => i.OrdenCompra)
             .WithMany(o => o.Items)
             .HasForeignKey(i => i.OrdenCompraId)
             .OnDelete(DeleteBehavior.Cascade); // "incluye (CASCADE)" según ERD
        });

        // ── Envio ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Envio>(e =>
        {
            e.HasKey(en => en.Id);
            e.Property(en => en.NumeroTracking).HasMaxLength(100);
            e.Property(en => en.EmpresaLogistica).IsRequired().HasMaxLength(100);

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación (no tiene tenant_id propio)
            e.HasQueryFilter(en => en.OrdenCompra.TenantId == CurrentTenantId);

            e.HasOne(en => en.OrdenCompra)
             .WithMany(o => o.Envios)
             .HasForeignKey(en => en.OrdenCompraId)
             .OnDelete(DeleteBehavior.Cascade); // "rastrea logística (CASCADE)" según ERD
        });

        // ── CostoLandedHistorico ────────────────────────────────────────────
        modelBuilder.Entity<CostoLandedHistorico>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CostoCalculado).HasPrecision(18, 2);
            e.Property(c => c.Detalle).HasColumnType("jsonb");
            e.HasIndex(c => c.ProductoId);
            e.HasIndex(c => c.TenantId);

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio, sin FK a OrdenCompra (histórico independiente)
            e.HasQueryFilter(c => c.TenantId == CurrentTenantId);
        });
    }
}