using Microsoft.EntityFrameworkCore;
using TenantIdentityService.Models;

namespace TenantIdentityService.Data;

/// <summary>
/// DbContext de MS-1: Tenant &amp; Identity Service.
/// 
/// Implementa el patrón Multi-Tenant mediante QueryFilters globales:
/// cada query que toque una tabla con tenant_id se filtra automáticamente
/// por el CurrentTenantId inyectado por el TenantMiddleware desde el JWT.
/// </summary>
public class TenantDbContext : DbContext
{
    /// <summary>
    /// El tenant_id del request actual, establecido por TenantMiddleware.
    /// EF Core aplica este valor en todos los filtros globales.
    /// </summary>
    public Guid? CurrentTenantId { get; set; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    // DbSets (tablas)
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<UsuarioSucursal> UsuarioSucursales => Set<UsuarioSucursal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Tenant ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Nombre).IsRequired().HasMaxLength(200);
            e.Property(t => t.Pais).IsRequired().HasMaxLength(2);
            e.Property(t => t.Moneda).IsRequired().HasMaxLength(3);
            e.Property(t => t.Idioma).HasMaxLength(5).HasDefaultValue("es");
            e.Property(t => t.PorcentajeIva).HasPrecision(5, 2);
            e.HasIndex(t => t.Nombre).IsUnique();
        });

        // ── Rol ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Rol>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Nombre).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Nombre).IsUnique();
        });

        // ── Usuario ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.Nombre).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

            // FILTRO GLOBAL MULTI-TENANT: cada query de Usuario filtra por el tenant actual
            e.HasQueryFilter(u => u.TenantId == CurrentTenantId);

            e.HasOne(u => u.Tenant)
             .WithMany(t => t.Usuarios)
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UsuarioRol (M:N) ────────────────────────────────────────────────
        modelBuilder.Entity<UsuarioRol>(e =>
        {
            e.HasKey(ur => new { ur.UsuarioId, ur.RolId });

            e.HasOne(ur => ur.Usuario)
             .WithMany(u => u.UsuarioRoles)
             .HasForeignKey(ur => ur.UsuarioId);

            e.HasOne(ur => ur.Rol)
             .WithMany(r => r.UsuarioRoles)
             .HasForeignKey(ur => ur.RolId);
        });

        // ── Sucursal ────────────────────────────────────────────────────────
        modelBuilder.Entity<Sucursal>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Nombre).IsRequired().HasMaxLength(200);

            // FILTRO GLOBAL MULTI-TENANT
            e.HasQueryFilter(s => s.TenantId == CurrentTenantId);

            e.HasOne(s => s.Tenant)
             .WithMany(t => t.Sucursales)
             .HasForeignKey(s => s.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UsuarioSucursal (M:N) ───────────────────────────────────────────
        modelBuilder.Entity<UsuarioSucursal>(e =>
        {
            e.HasKey(us => new { us.UsuarioId, us.SucursalId });

            e.HasOne(us => us.Usuario)
             .WithMany(u => u.UsuarioSucursales)
             .HasForeignKey(us => us.UsuarioId);

            e.HasOne(us => us.Sucursal)
             .WithMany(s => s.UsuarioSucursales)
             .HasForeignKey(us => us.SucursalId);
        });

        // Seed de roles base del sistema
        SeedRoles(modelBuilder);
    }

    /// <summary>
    /// Datos iniciales: los 5 roles del sistema se pre-cargan en la migración inicial.
    /// </summary>
    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Nombre = "CAJERO",           Descripcion = "Opera el POS en mostrador" },
            new Rol { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Nombre = "REPONEDOR",        Descripcion = "Gestiona el inventario físico" },
            new Rol { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Nombre = "ADMIN",            Descripcion = "Gestiona el tenant completo" },
            new Rol { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Nombre = "SUPER_ADMIN",      Descripcion = "Dueño del sistema global" },
            new Rol { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Nombre = "CLIENTE_AFILIADO", Descripcion = "Cliente con membresía activa" }
        );
    }
}
