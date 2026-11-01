extern alias TaxComplianceServiceAlias;
using TaxCalculator = TaxComplianceServiceAlias::TaxComplianceService.Services.TaxCalculatorService;
using TaxItem = TaxComplianceServiceAlias::TaxComplianceService.Services.TaxItem;
using Xunit;

namespace GlobalMart.IntegrationTests;

public class PesableTaxRoundingTests
{
    [Fact]
    [Trait("Plan", "S2-063")]
    public void CanastaFraccionaria_SumaLineasRedondeadas_NoRedondeaSoloElAgregado()
    {
        var result = TaxCalculator.Calculate(new[] { new TaxItem(1m, .135m), new TaxItem(1m, .135m) }, 19m);
        Assert.All(result.Items, i => { Assert.Equal(.14m, i.Subtotal); Assert.Equal(.03m, i.Iva); });
        Assert.Equal(.28m, result.Subtotal);
        Assert.Equal(.06m, result.Iva);
        Assert.Equal(.34m, result.Total);
    }

    [Fact]
    [Trait("Plan", "S2-063")]
    public void CanastaMixta_RespetaExencionYTasaPorItem()
    {
        var result = TaxCalculator.Calculate(new[]
        {
            new TaxItem(2000m, .750m, "Exento", true),
            new TaxItem(12.3456m, .500m) { PorcentajeIva = 21m },
            new TaxItem(100m, 2m)
        }, 19m);
        Assert.Equal(1706.17m, result.Subtotal);
        Assert.Equal(39.30m, result.Iva);
        Assert.Equal(1745.47m, result.Total);
        Assert.Equal(0m, result.Items[0].Iva);
    }

    [Fact]
    [Trait("Plan", "S2-063")]
    public void Iva_EmpatesSeRedondeanAlPar()
    {
        var result = TaxCalculator.Calculate(new[] { new TaxItem(.50m, 1m), new TaxItem(1.50m, 1m) }, 1m);
        Assert.Equal(0m, result.Items[0].Iva);
        Assert.Equal(.02m, result.Items[1].Iva);
        Assert.Equal(.02m, result.Iva);
    }
}
