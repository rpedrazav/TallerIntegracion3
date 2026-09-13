using LoyaltyCustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyCustomerService.Data;

public class LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : DbContext(options)
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core aplica este valor en todos los filtros globales.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public DbSet<ClienteAfiliado> ClientesAfiliados => Set<ClienteAfiliado>();
    public DbSet<SaldoPuntos> SaldosPuntos => Set<SaldoPuntos>();
    public DbSet<MovimientoPuntos> MovimientosPuntos => Set<MovimientoPuntos>();
    public DbSet<TierMembresia> TiersMembresia => Set<TierMembresia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TierMembresia
        modelBuilder.Entity<TierMembresia>(entity =>
        {
            entity.Property(t => t.BeneficiosJson)
                  .HasColumnType("jsonb");

            entity.HasIndex(t => t.TenantId)
                  .HasDatabaseName("idx_tiers_membresia_tenant");

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            entity.HasQueryFilter(t => t.TenantId == CurrentTenantId);
        });

        // ClienteAfiliado
        modelBuilder.Entity<ClienteAfiliado>(entity =>
        {
            entity.HasOne(c => c.Tier)
                  .WithMany(t => t.ClientesAfiliados)
                  .HasForeignKey(c => c.TierId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(c => c.TenantId)
                  .HasDatabaseName("idx_clientes_afiliados_tenant");

            entity.HasIndex(c => new { c.TenantId, c.Email })
                  .IsUnique()
                  .HasDatabaseName("idx_clientes_afiliados_tenant_email");

            entity.HasIndex(c => new { c.TenantId, c.QrCode })
                  .HasDatabaseName("idx_clientes_afiliados_tenant_qr");

            // FILTRO GLOBAL MULTI-TENANT: tenant_id propio
            entity.HasQueryFilter(c => c.TenantId == CurrentTenantId);
        });

        // SaldoPuntos (1 a 1 con ClienteAfiliado, PK = cliente_id)
        modelBuilder.Entity<SaldoPuntos>(entity =>
        {
            entity.HasKey(s => s.ClienteId);

            entity.HasOne(s => s.Cliente)
                  .WithOne(c => c.SaldoPuntos)
                  .HasForeignKey<SaldoPuntos>(s => s.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación
            entity.HasQueryFilter(s => s.Cliente!.TenantId == CurrentTenantId);
        });

        // MovimientoPuntos (1 a N con ClienteAfiliado)
        modelBuilder.Entity<MovimientoPuntos>(entity =>
        {
            entity.HasOne(m => m.Cliente)
                  .WithMany(c => c.Movimientos)
                  .HasForeignKey(m => m.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.ClienteId)
                  .HasDatabaseName("idx_movimientos_puntos_cliente");

            entity.HasIndex(m => new { m.ClienteId, m.Timestamp })
                  .HasDatabaseName("idx_movimientos_puntos_cliente_timestamp");

            // FILTRO GLOBAL MULTI-TENANT: heredado vía navegación
            entity.HasQueryFilter(m => m.Cliente!.TenantId == CurrentTenantId);
        });
    }
}
