extern alias POSCartServiceAlias;

using Microsoft.EntityFrameworkCore;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-032")]
public sealed class IntentoPagoPersistenceTests
{
    private static Pos.Data.PosCartDbContext CreateDbContext(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<Pos.Data.PosCartDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy_test;Username=dummy")
            .Options;

        return new Pos.Data.PosCartDbContext(options)
        {
            CurrentTenantId = tenantId
        };
    }

    [Fact]
    public void IntentoPago_ModeloDefineMapeoEIndicesCorrectamente()
    {
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(Pos.Models.IntentoPago));

        Assert.NotNull(entityType);

        // Tabla mapeada
        Assert.Equal("IntentosPago", entityType!.GetTableName());

        // Clave primaria
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal("Id", Assert.Single(primaryKey!.Properties).Name);

        // RN-01 / S2-032: Índice único compuesto (TenantId, IdempotencyKey)
        var indiceUnicoIdempotencia = entityType.GetIndexes()
            .FirstOrDefault(idx => idx.IsUnique &&
                idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "IdempotencyKey" }));

        Assert.NotNull(indiceUnicoIdempotencia);
        Assert.True(indiceUnicoIdempotencia!.IsUnique);

        // Índice compuesto para consultas rápidas por tenant y venta (TenantId, VentaId)
        var indiceTenantVenta = entityType.GetIndexes()
            .FirstOrDefault(idx =>
                idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "VentaId" }));

        Assert.NotNull(indiceTenantVenta);

        // Relación con Venta (FK OnDelete Cascade)
        var fk = entityType.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Pos.Models.Venta));
        Assert.NotNull(fk);
        Assert.Equal(DeleteBehavior.Cascade, fk!.DeleteBehavior);
        Assert.Equal("VentaId", Assert.Single(fk.Properties).Name);

        // Precisión de Monto (18, 2)
        var montoProp = entityType.FindProperty(nameof(Pos.Models.IntentoPago.Monto));
        Assert.NotNull(montoProp);
        Assert.Equal(18, montoProp!.GetPrecision());
        Assert.Equal(2, montoProp.GetScale());

        // Longitudes máximas configuradas
        Assert.Equal(100, entityType.FindProperty(nameof(Pos.Models.IntentoPago.IdempotencyKey))?.GetMaxLength());
        Assert.Equal(3, entityType.FindProperty(nameof(Pos.Models.IntentoPago.Moneda))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(Pos.Models.IntentoPago.Referencia))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(Pos.Models.IntentoPago.AuthorizationCode))?.GetMaxLength());
        Assert.Equal(100, entityType.FindProperty(nameof(Pos.Models.IntentoPago.DeclineCode))?.GetMaxLength());
        Assert.Equal(500, entityType.FindProperty(nameof(Pos.Models.IntentoPago.Mensaje))?.GetMaxLength());
    }

    [Fact]
    public void IntentoPago_NoContieneCamposDeTarjeta_CumpleRN02()
    {
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(Pos.Models.IntentoPago));
        Assert.NotNull(entityType);

        var propiedades = entityType!.GetProperties().Select(p => p.Name.ToLowerInvariant()).ToArray();
        var palabrasProhibidas = new[] { "card", "pan", "cvv", "cvc", "pin", "expiration", "vencimiento" };

        foreach (var prop in propiedades)
        {
            foreach (var prohibida in palabrasProhibidas)
            {
                Assert.DoesNotContain(prohibida, prop);
            }
        }
    }

    [Fact]
    public void Venta_ContieneNavegacionIntentosPago()
    {
        using var db = CreateDbContext();
        var ventaType = db.Model.FindEntityType(typeof(Pos.Models.Venta));
        Assert.NotNull(ventaType);

        var navigation = ventaType!.FindNavigation(nameof(Pos.Models.Venta.IntentosPago));
        Assert.NotNull(navigation);
        Assert.True(navigation!.IsCollection);
        Assert.Equal(typeof(Pos.Models.IntentoPago), navigation.TargetEntityType.ClrType);
    }

    [Fact]
    public void IntentoPago_QueryFilter_ConfiguradoConTenantId()
    {
        using var db = CreateDbContext(Guid.NewGuid());
        var entityType = db.Model.FindEntityType(typeof(Pos.Models.IntentoPago));
        Assert.NotNull(entityType);

        var queryFilter = entityType!.GetQueryFilter();
        Assert.NotNull(queryFilter);
        Assert.Contains("CurrentTenantId", queryFilter!.ToString());
        Assert.Contains("TenantId", queryFilter.ToString());
    }
}
