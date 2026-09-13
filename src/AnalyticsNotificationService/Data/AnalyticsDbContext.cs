using AnalyticsNotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsNotificationService.Data;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core aplica este valor en todos los filtros globales.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public DbSet<KPIVenta> KPIsVentas => Set<KPIVenta>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<HistorialEnvio> HistorialEnvios => Set<HistorialEnvio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Llave compuesta para KPIVenta: (tenant_id, sucursal_id, fecha)
        modelBuilder.Entity<KPIVenta>()
            .HasKey(k => new { k.TenantId, k.SucursalId, k.Fecha });

        // Filtros Globales (Global Query Filters) para Aislamiento Multi-Tenant (RN-01 / RNF-01)
        modelBuilder.Entity<KPIVenta>().HasQueryFilter(k => k.TenantId == CurrentTenantId);
        modelBuilder.Entity<Alerta>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        modelBuilder.Entity<HistorialEnvio>().HasQueryFilter(h => h.TenantId == CurrentTenantId);

        // Indices para aislamiento Multi-Tenant y consultas frecuentes (RNF-01)
        modelBuilder.Entity<KPIVenta>()
            .HasIndex(k => k.TenantId)
            .HasDatabaseName("idx_kpi_ventas_tenant");

        modelBuilder.Entity<KPIVenta>()
            .HasIndex(k => new { k.TenantId, k.Fecha })
            .HasDatabaseName("idx_kpi_ventas_tenant_fecha");

        modelBuilder.Entity<KPIVenta>()
            .HasIndex(k => new { k.TenantId, k.SucursalId })
            .HasDatabaseName("idx_kpi_ventas_tenant_sucursal");

        modelBuilder.Entity<Alerta>()
            .HasIndex(a => a.TenantId)
            .HasDatabaseName("idx_alertas_tenant");

        modelBuilder.Entity<Alerta>()
            .HasIndex(a => new { a.TenantId, a.Estado })
            .HasDatabaseName("idx_alertas_tenant_estado");

        modelBuilder.Entity<HistorialEnvio>()
            .HasIndex(h => h.TenantId)
            .HasDatabaseName("idx_historial_envios_tenant");

        modelBuilder.Entity<HistorialEnvio>()
            .HasIndex(h => new { h.TenantId, h.Timestamp })
            .HasDatabaseName("idx_historial_envios_tenant_timestamp");
    }
}
