using Microsoft.EntityFrameworkCore;
using TaxComplianceService.Models;

namespace TaxComplianceService.Data;

/// <summary>
/// DbContext de MS-2: Tax &amp; Compliance Service.
/// 
/// Implementa el patrón Multi-Tenant mediante QueryFilters globales.
/// El CurrentTenantId es establecido por el TenantMiddleware en cada request.
/// </summary>
public class TaxDbContext : DbContext
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core filtra automáticamente todas las entidades con tenant_id.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public TaxDbContext(DbContextOptions<TaxDbContext> options) : base(options) { }

    // DbSets (tablas)
    public DbSet<ConfiguracionFiscal> ConfiguracionesFiscales => Set<ConfiguracionFiscal>();
    public DbSet<DocumentoTributario> DocumentosTributarios => Set<DocumentoTributario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── ConfiguracionFiscal ──────────────────────────────────────────────
        modelBuilder.Entity<ConfiguracionFiscal>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.TipoDocumento).IsRequired().HasMaxLength(20);
            e.Property(c => c.Prefijo).HasMaxLength(10).HasDefaultValue(string.Empty);
            e.Property(c => c.PorcentajeImpuesto).HasPrecision(5, 2);
            e.Property(c => c.PaisEntidadFiscal).IsRequired().HasMaxLength(2);

            // FILTRO GLOBAL MULTI-TENANT
            e.HasQueryFilter(c => c.TenantId == CurrentTenantId);

            e.HasIndex(c => new { c.TenantId, c.TipoDocumento });
        });

        // ── DocumentoTributario ──────────────────────────────────────────────
        modelBuilder.Entity<DocumentoTributario>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.TipoDocumento).IsRequired().HasMaxLength(20);
            e.Property(d => d.Estado).IsRequired().HasMaxLength(20).HasDefaultValue("PENDIENTE");
            e.Property(d => d.ContenidoXml).IsRequired();
            e.Property(d => d.CodigoAutorizacion).HasMaxLength(100);
            e.Property(d => d.MontoTotal).HasPrecision(12, 2);
            e.Property(d => d.MontoImpuesto).HasPrecision(12, 2);

            // FILTRO GLOBAL MULTI-TENANT
            e.HasQueryFilter(d => d.TenantId == CurrentTenantId);

            // Índice para búsqueda de documentos por tenant y número de folio
            e.HasIndex(d => new { d.TenantId, d.Numero }).IsUnique();

            e.HasOne(d => d.ConfiguracionFiscal)
             .WithMany(c => c.Documentos)
             .HasForeignKey(d => d.ConfiguracionFiscalId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
