using FluentAssertions;
using TaxComplianceService.Services;
using Xunit;

namespace GlobalMart.Tests.MS2_TaxCompliance;

/// <summary>
/// Tests unitarios del TaxCalculatorService (cálculo de IVA).
/// Cubre TI3-210, TI3-211 y TI3-212.
///
/// No requiere mocks ni base de datos: TaxCalculatorService es lógica pura
/// sin dependencias externas.
/// </summary>
public class TaxCalculatorService_Tests
{
    // ════════════════════════════════════════════════════════════════════════
    //  TI3-210: Calcular IVA 19% con items
    //    [{precio: 5000, cantidad: 2}, {precio: 3000, cantidad: 1}]
    //    → subtotal 13.000, IVA 2.470, total 15.470
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_IVA19_ConDosItems_SubtotalCorrecto()
    {
        // Arrange — Dos items: (5000 × 2) + (3000 × 1) = 13.000
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 2),
            new(precio: 3000, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 19);

        // Assert — Subtotal
        result.Subtotal.Should().Be(13_000m);
    }

    [Fact]
    public void Calculate_IVA19_ConDosItems_IvaCorrecto()
    {
        // Arrange
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 2),
            new(precio: 3000, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 19);

        // Assert — IVA: 13.000 × 0.19 = 2.470
        result.Iva.Should().Be(2_470m);
    }

    [Fact]
    public void Calculate_IVA19_ConDosItems_TotalCorrecto()
    {
        // Arrange
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 2),
            new(precio: 3000, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 19);

        // Assert — Total: 13.000 + 2.470 = 15.470
        result.Total.Should().Be(15_470m);
    }

    [Fact]
    public void Calculate_IVA19_ConDosItems_DesglosePorItem()
    {
        // Arrange
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 2, nombre: "Producto A"),
            new(precio: 3000, cantidad: 1, nombre: "Producto B")
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 19);

        // Assert — Desglose item por item
        result.Items.Should().HaveCount(2);

        // Item A: 5000 × 2 = 10.000, IVA = 1.900
        result.Items[0].Subtotal.Should().Be(10_000m);
        result.Items[0].Iva.Should().Be(1_900m);
        result.Items[0].Total.Should().Be(11_900m);

        // Item B: 3000 × 1 = 3.000, IVA = 570
        result.Items[1].Subtotal.Should().Be(3_000m);
        result.Items[1].Iva.Should().Be(570m);
        result.Items[1].Total.Should().Be(3_570m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-211: Calcular IVA 21% con subtotal 10.000
    //    → IVA 2.100, total 12.100
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_IVA21_Subtotal10000_IvaCorrecto()
    {
        // Arrange — Un solo item de 10.000 × 1
        var items = new List<TaxItem>
        {
            new(precio: 10_000, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 21);

        // Assert — Subtotal
        result.Subtotal.Should().Be(10_000m);

        // Assert — IVA: 10.000 × 0.21 = 2.100
        result.Iva.Should().Be(2_100m);

        // Assert — Total: 10.000 + 2.100 = 12.100
        result.Total.Should().Be(12_100m);
    }

    [Fact]
    public void Calculate_IVA21_ConMultiplesItems_TotalCorrecto()
    {
        // Arrange — Múltiples items que suman subtotal 10.000
        // 4000 × 2 = 8.000 + 2000 × 1 = 2.000 → subtotal = 10.000
        var items = new List<TaxItem>
        {
            new(precio: 4000, cantidad: 2),
            new(precio: 2000, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 21);

        // Assert
        result.Subtotal.Should().Be(10_000m);
        result.Iva.Should().Be(2_100m);
        result.Total.Should().Be(12_100m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TI3-212: Calcular IVA 0% (producto exento)
    //    → subtotal = total, IVA = 0
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_IVA0_ProductoExento_IvaEsCero()
    {
        // Arrange — IVA general = 0 (todo exento)
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 3)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 0);

        // Assert
        result.Subtotal.Should().Be(15_000m);
        result.Iva.Should().Be(0m);
        result.Total.Should().Be(15_000m);
        result.Total.Should().Be(result.Subtotal); // subtotal == total
    }

    [Fact]
    public void Calculate_IVA0_MultipleItems_SubtotalIgualTotal()
    {
        // Arrange
        var items = new List<TaxItem>
        {
            new(precio: 2000, cantidad: 2),
            new(precio: 3500, cantidad: 1)
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 0);

        // Assert — subtotal = total, IVA = 0
        result.Subtotal.Should().Be(7_500m);
        result.Iva.Should().Be(0m);
        result.Total.Should().Be(result.Subtotal);
    }

    [Fact]
    public void Calculate_ItemConFlagExento_IvaEsCeroParaEseItem()
    {
        // Arrange — Un item exento y uno gravado, tasa general 19%
        var items = new List<TaxItem>
        {
            new(precio: 5000, cantidad: 1, nombre: "Pan (exento)", exento: true),
            new(precio: 3000, cantidad: 1, nombre: "Bebida (gravada)")
        };

        // Act
        var result = TaxCalculatorService.Calculate(items, porcentajeIva: 19);

        // Assert — El item exento no genera IVA
        result.Items[0].Iva.Should().Be(0m);
        result.Items[0].Exento.Should().BeTrue();
        result.Items[0].Total.Should().Be(5_000m); // subtotal = total

        // El item gravado sí genera IVA
        result.Items[1].Iva.Should().Be(570m); // 3000 × 0.19
        result.Items[1].Exento.Should().BeFalse();

        // Totales globales: subtotal = 8000, IVA = 570 (solo del gravado)
        result.Subtotal.Should().Be(8_000m);
        result.Iva.Should().Be(570m);
        result.Total.Should().Be(8_570m);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Tests adicionales de robustez
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_ConListaVacia_RetornaCeros()
    {
        // Act
        var result = TaxCalculatorService.Calculate(new List<TaxItem>(), porcentajeIva: 19);

        // Assert
        result.Subtotal.Should().Be(0m);
        result.Iva.Should().Be(0m);
        result.Total.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ConItemsNull_LanzaArgumentNullException()
    {
        // Act & Assert
        var act = () => TaxCalculatorService.Calculate(null!, porcentajeIva: 19);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calculate_ConPorcentajeNegativo_LanzaArgumentOutOfRangeException()
    {
        var items = new List<TaxItem> { new(precio: 1000, cantidad: 1) };

        // Act & Assert
        var act = () => TaxCalculatorService.Calculate(items, porcentajeIva: -5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
