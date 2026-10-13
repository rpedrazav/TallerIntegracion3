extern alias POSCartServiceAlias;

using Xunit;
using DTOs = POSCartServiceAlias::POSCartService.DTOs;
using Validators = POSCartServiceAlias::POSCartService.Validators;

namespace GlobalMart.IntegrationTests;

public class CrearVentaRequestValidatorTests
{
    private readonly Validators.CrearVentaRequestValidator _validator = new();

    [Fact]
    public void Validador_VentaUnitariaSinPrecioCliente_DebeSerValida()
    {
        // En S2-001, ningún precio o impuesto aportado por el renderer es autoridad.
        // El cliente solo envía producto_id y cantidad; precio_unitario es omitido o 0 por defecto.
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "EFECTIVO",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    Cantidad = 2m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validador_VentaAPesoConPesoKg_DebeSerValida()
    {
        // Venta a peso con balanza: el cliente envía peso_kg
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "TARJETA",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    PesoKg = 1.450m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validador_VentaMixta_DebeSerValida()
    {
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "MIXTO",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    Cantidad = 3m
                },
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    PesoKg = 0.825m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validador_PrecioNegativo_DebeFallar()
    {
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "EFECTIVO",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    Cantidad = 1m,
                    PrecioUnitario = -100m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("no puede ser negativo"));
    }

    [Fact]
    public void Validador_PesoNegativo_DebeFallar()
    {
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "EFECTIVO",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    PesoKg = -0.5m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("peso en kg debe ser mayor a 0"));
    }

    [Fact]
    public void Validador_ItemsVacios_DebeFallar()
    {
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "EFECTIVO",
            Items = []
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DTOs.CrearVentaRequest.Items));
    }

    [Fact]
    public void Validador_MetodoPagoInvalido_DebeFallar()
    {
        var request = new DTOs.CrearVentaRequest
        {
            MetodoPago = "CRYPTO",
            Items =
            [
                new DTOs.ItemVentaRequest
                {
                    ProductoId = Guid.NewGuid(),
                    Cantidad = 1m
                }
            ]
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DTOs.CrearVentaRequest.MetodoPago));
    }
}
