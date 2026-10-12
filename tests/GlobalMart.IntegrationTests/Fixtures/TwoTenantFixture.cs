extern alias TenantIdentityServiceAlias;
extern alias CatalogPricingServiceAlias;
extern alias POSCartServiceAlias;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Identity = TenantIdentityServiceAlias::TenantIdentityService;
using Catalog = CatalogPricingServiceAlias::CatalogPricingService;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests.Fixtures;

/// <summary>Datos propios por ejecución; nunca ejecuta seeds ni modifica public.</summary>
public sealed class TwoTenantFixture : IAsyncDisposable
{
    public const string Password = "Fixture-only-2026!";
    public TenantData TenantA { get; } = new();
    public TenantData TenantB { get; } = new();
    private readonly string _connection;
    private readonly string _prefix = "s2022_" + Guid.NewGuid().ToString("N");
    private readonly List<string> _schemas = new();

    public TwoTenantFixture(string connectionString) => _connection = connectionString;

    public Identity.Data.TenantDbContext IdentityDb(TenantData tenant) => new(
        new DbContextOptionsBuilder<Identity.Data.TenantDbContext>().UseNpgsql(Connection("identity")).Options)
        { CurrentTenantId = Owned(tenant) };
    public Catalog.Data.CatalogDbContext CatalogDb(TenantData tenant) => new(
        new DbContextOptionsBuilder<Catalog.Data.CatalogDbContext>().UseNpgsql(Connection("catalog")).Options)
        { CurrentTenantId = Owned(tenant) };
    public Pos.Data.PosCartDbContext PosDb(TenantData tenant) => new(
        new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>().UseNpgsql(Connection("pos")).Options)
        { CurrentTenantId = Owned(tenant) };

    public string Connection(string service)
    {
        if (service is not ("identity" or "catalog" or "pos"))
            throw new ArgumentOutOfRangeException(nameof(service));
        return new NpgsqlConnectionStringBuilder(_connection)
            { SearchPath = _prefix + "_" + service, Pooling = false }.ConnectionString;
    }

    private Guid Owned(TenantData tenant) => ReferenceEquals(tenant, TenantA) || ReferenceEquals(tenant, TenantB)
        ? tenant.TenantId : throw new ArgumentException("Tenant ajeno a esta fixture.", nameof(tenant));

    public async Task InitializeAsync()
    {
        if (_schemas.Count != 0) throw new InvalidOperationException("Fixture ya inicializada.");
        try
        {
            await using var connection = new NpgsqlConnection(_connection);
            await connection.OpenAsync();
            foreach (var service in new[] { "identity", "catalog", "pos" })
            {
                var schema = _prefix + "_" + service;
                await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
                await command.ExecuteNonQueryAsync();
                _schemas.Add(schema);
            }
            await using var identity = IdentityDb(TenantA);
            await using var catalog = CatalogDb(TenantA);
            await using var pos = PosDb(TenantA);
            // CreateTables evita que EnsureCreated omita tablas al encontrar otros esquemas.
            foreach (DbContext db in new DbContext[] { identity, catalog, pos })
                await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            var role = await identity.Roles.SingleAsync(r => r.Nombre == "CAJERO");
            foreach (var tenant in new[] { TenantA, TenantB })
                await SeedAsync(tenant, role.Id);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    private async Task SeedAsync(TenantData data, Guid roleId)
    {
        await using var identity = IdentityDb(data);
        identity.Tenants.Add(new Identity.Models.Tenant
            { Id = data.TenantId, Nombre = "Fixture " + data.TenantId, Pais = "CL", Moneda = "CLP" });
        identity.Sucursales.Add(new Identity.Models.Sucursal
            { Id = data.SucursalId, TenantId = data.TenantId, Nombre = "Sucursal fixture" });
        identity.Usuarios.Add(new Identity.Models.Usuario
        {
            Id = data.CajeroId, TenantId = data.TenantId, Nombre = "Cajero fixture",
            Email = data.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4),
            UsuarioRoles = new[] { new Identity.Models.UsuarioRol { RolId = roleId } },
            UsuarioSucursales = new[] { new Identity.Models.UsuarioSucursal { SucursalId = data.SucursalId } }
        });
        await identity.SaveChangesAsync();
        await using var catalog = CatalogDb(data);
        catalog.Productos.Add(new Catalog.Models.Producto
        {
            Id = data.ProductoId, TenantId = data.TenantId, Nombre = "Producto fixture",
            CodigoBarras = "7790000000001", PrecioBase = 100m, UomBaseId = Guid.NewGuid(),
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await catalog.SaveChangesAsync();
        await using var pos = PosDb(data);
        pos.Turnos.AddRange(
            new Pos.Models.Turno { Id = data.TurnoAbiertoId, TenantId = data.TenantId,
                CajeroId = data.CajeroId, SucursalId = data.SucursalId, MontoApertura = 10000m },
            new Pos.Models.Turno { Id = data.TurnoCerradoId, TenantId = data.TenantId,
                CajeroId = data.CajeroId, SucursalId = data.SucursalId, Estado = Pos.Models.EstadoTurno.CERRADO,
                CerradoAt = DateTime.UtcNow, MontoApertura = 10000m, MontoCierre = 10119m });
        pos.Ventas.AddRange(Sale(data, data.VentaPendienteId, false), Sale(data, data.VentaCompletadaId, true));
        await pos.SaveChangesAsync();
    }

    private static Pos.Models.Venta Sale(TenantData data, Guid id, bool completed) => new()
    {
        Id = id, TenantId = data.TenantId, CajeroId = data.CajeroId, SucursalId = data.SucursalId,
        TurnoId = completed ? data.TurnoCerradoId : data.TurnoAbiertoId,
        Subtotal = 100m, Impuestos = 19m, Total = 119m, MetodoPago = Pos.Models.MetodoPagoVenta.EFECTIVO,
        Estado = completed ? Pos.Models.EstadoVenta.COMPLETADA : Pos.Models.EstadoVenta.PENDIENTE,
        Items = new[] { new Pos.Models.ItemVenta { ProductoId = data.ProductoId,
            NombreProducto = "Producto fixture", Cantidad = 1m, PrecioUnitario = 100m, Subtotal = 100m } },
        Pagos = completed ? new[] { new Pos.Models.Pago { Metodo = Pos.Models.MetodoPago.EFECTIVO, Monto = 119m } }
            : Array.Empty<Pos.Models.Pago>()
    };

    /// <summary>Idempotente y restringida a uno de los dos tenants de esta ejecución.</summary>
    public async Task CleanTenantAsync(TenantData tenant)
    {
        var id = Owned(tenant);
        await using var pos = PosDb(tenant);
        await using (var transaction = await pos.Database.BeginTransactionAsync())
        {
            await pos.Ventas.Where(v => v.TenantId == id).ExecuteDeleteAsync();
            await pos.Turnos.Where(t => t.TenantId == id).ExecuteDeleteAsync();
            await transaction.CommitAsync();
        }
        await using var catalog = CatalogDb(tenant);
        await catalog.Precios.Where(p => p.TenantId == id).ExecuteDeleteAsync();
        await catalog.Productos.Where(p => p.TenantId == id).ExecuteDeleteAsync();
        await using var identity = IdentityDb(tenant);
        await using var identityTransaction = await identity.Database.BeginTransactionAsync();
        await identity.Usuarios.Where(u => u.TenantId == id).ExecuteDeleteAsync();
        await identity.Sucursales.Where(s => s.TenantId == id).ExecuteDeleteAsync();
        await identity.Tenants.Where(t => t.Id == id).ExecuteDeleteAsync();
        await identityTransaction.CommitAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_schemas.Count == 0) return;
        await using var connection = new NpgsqlConnection(_connection);
        await connection.OpenAsync();
        foreach (var schema in _schemas.ToArray())
        {
            // Solo identificadores generados por esta instancia; nunca DROP DATABASE/public.
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", connection);
            await command.ExecuteNonQueryAsync();
            _schemas.Remove(schema);
        }
    }
}

public sealed class TenantData
{
    public Guid TenantId { get; } = Guid.NewGuid();
    public Guid CajeroId { get; } = Guid.NewGuid();
    public Guid SucursalId { get; } = Guid.NewGuid();
    public Guid ProductoId { get; } = Guid.NewGuid();
    public Guid TurnoAbiertoId { get; } = Guid.NewGuid();
    public Guid TurnoCerradoId { get; } = Guid.NewGuid();
    public Guid VentaPendienteId { get; } = Guid.NewGuid();
    public Guid VentaCompletadaId { get; } = Guid.NewGuid();
    public string Email => $"cajero-{CajeroId:N}@example.test";
}
