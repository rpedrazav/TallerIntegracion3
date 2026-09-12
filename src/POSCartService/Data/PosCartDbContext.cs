using Microsoft.EntityFrameworkCore;
using POSCartService.Models;

namespace POSCartService.Data;

/// <summary>
/// DbContext de MS-5: POS &amp; Cart Service.
/// 
/// Implementa el patrón Multi-Tenant mediante QueryFilters globales:
/// cada query que toque una tabla con tenant_id (directo o heredado vía
/// navegación) se filtra automáticamente por el CurrentTenantId inyectado
/// por el TenantMiddleware desde el JWT.
/// </summary>
public class PosCartDbContext : DbContext
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core aplica este valor en todos los filtros globales.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public PosCartDbContext(DbContextOptions<PosCartDbContext> options) : base(options) { }

    // DbSets (tablas)
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<ItemVenta> ItemsVenta => Set<ItemVenta>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Anulacion> Anulaciones => Set<Anulacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Turno ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Turno>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.MontoApertura).HasPrecision(18, 2);
            e.Property(t => t.MontoCierre).HasPrecision(18, 2);
            e.HasIndex(t => t.CajeroId);
            e.HasIndex(t => t.SucursalId);
            e.HasIndex(t => t.TenantId);

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            e.HasQueryFilter(t => t.TenantId == CurrentTenantId);
        });

        // ── Venta ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Venta>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Subtotal).HasPrecision(18, 2);
            e.Property(v => v.Impuestos).HasPrecision(18, 2);
            e.Property(v => v.Total).HasPrecision(18, 2);
            e.HasIndex(v => v.CajeroId);
            e.HasIndex(v => v.SucursalId);
            e.HasIndex(v => v.TenantId);

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            e.HasQueryFilter(v => v.TenantId == CurrentTenantId);

            e.HasOne(v => v.Turno)
             .WithMany(t => t.Ventas)
             .HasForeignKey(v => v.TurnoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ItemVenta ───────────────────────────────────────────────────────
        modelBuilder.Entity<ItemVenta>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.NombreProducto).IsRequired().HasMaxLength(200);
            e.Property(i => i.PrecioUnitario).HasPrecision(18, 2);
            e.Property(i => i.Subtotal).HasPrecision(18, 2);
            e.HasIndex(i => i.ProductoId);

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación (no tiene tenant_id propio)
            e.HasQueryFilter(i => i.Venta.TenantId == CurrentTenantId);

            e.HasOne(i => i.Venta)
             .WithMany(v => v.Items)
             .HasForeignKey(i => i.VentaId)
             .OnDelete(DeleteBehavior.Cascade); // "contiene (CASCADE)" según ERD
        });

        // ── Pago ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Pago>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Monto).HasPrecision(18, 2);
            e.Property(p => p.Vuelto).HasPrecision(18, 2);

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación (no tiene tenant_id propio)
            e.HasQueryFilter(p => p.Venta.TenantId == CurrentTenantId);

            e.HasOne(p => p.Venta)
             .WithMany(v => v.Pagos)
             .HasForeignKey(p => p.VentaId)
             .OnDelete(DeleteBehavior.Cascade); // "pagada con (CASCADE)" según ERD
        });

        // ── Anulacion ───────────────────────────────────────────────────────
        modelBuilder.Entity<Anulacion>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Motivo).IsRequired();
            e.HasIndex(a => a.VentaId).IsUnique(); // "revocada por" es 1:1 según ERD

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación (no tiene tenant_id propio)
            e.HasQueryFilter(a => a.Venta.TenantId == CurrentTenantId);

            e.HasOne(a => a.Venta)
             .WithOne(v => v.Anulacion)
             .HasForeignKey<Anulacion>(a => a.VentaId)
             .OnDelete(DeleteBehavior.Cascade); // "revocada por (CASCADE)" según ERD
        });
    }
}