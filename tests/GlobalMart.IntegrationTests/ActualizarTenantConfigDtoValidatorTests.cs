extern alias TenantIdentityServiceAlias;

using Xunit;
using Dtos = TenantIdentityServiceAlias::TenantIdentityService.DTOs;
using Validators = TenantIdentityServiceAlias::TenantIdentityService.Validators;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// TI3-459: Pruebas unitarias para ActualizarTenantConfigDtoValidator.
/// Valida campos Moneda, Idioma, ZonaHoraria, PorcentajeIva y Pais.
/// </summary>
public class ActualizarTenantConfigDtoValidatorTests
{
    private readonly Validators.ActualizarTenantConfigDtoValidator _validator = new();

    private static Dtos.ActualizarTenantConfigDto CreateValidDto() => new()
    {
        Pais = "CL",
        Moneda = "CLP",
        Idioma = "es",
        PorcentajeIva = 19m,
        ZonaHoraria = "America/Santiago"
    };

    [Fact]
    public void Validador_ConDatosValidos_DebeSerValido()
    {
        var dto = CreateValidDto();

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("CLP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("ARS")]
    [InlineData("PEN")]
    [InlineData("COP")]
    [InlineData("MXN")]
    [InlineData("BRL")]
    [InlineData("UYU")]
    public void Validador_ConMonedasSoportadas_DebeAceptar(string moneda)
    {
        var dto = CreateValidDto();
        dto.Moneda = moneda;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")] // Longitud != 3
    [InlineData("USDD")] // Longitud != 3
    [InlineData("XYZ")] // Moneda no soportada
    public void Validador_ConMonedaInvalida_DebeFallar(string monedaInvalida)
    {
        var dto = CreateValidDto();
        dto.Moneda = monedaInvalida;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.ActualizarTenantConfigDto.Moneda));
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    [InlineData("pt")]
    [InlineData("ES")] // Caso mayúsculas (debe normalizarse/aceptarse)
    public void Validador_ConIdiomasSoportados_DebeAceptar(string idioma)
    {
        var dto = CreateValidDto();
        dto.Idioma = idioma;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("fr")] // No soportado
    [InlineData("de")] // No soportado
    [InlineData("es-419-extra")] // Más de 5 caracteres
    public void Validador_ConIdiomaInvalido_DebeFallar(string idiomaInvalido)
    {
        var dto = CreateValidDto();
        dto.Idioma = idiomaInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.ActualizarTenantConfigDto.Idioma));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(19)]
    [InlineData(21)]
    [InlineData(50)]
    public void Validador_ConIvaValido_DebeAceptar(decimal iva)
    {
        var dto = CreateValidDto();
        dto.PorcentajeIva = iva;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(50.01)]
    [InlineData(100)]
    public void Validador_ConIvaInvalido_DebeFallar(decimal ivaInvalido)
    {
        var dto = CreateValidDto();
        dto.PorcentajeIva = ivaInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.ActualizarTenantConfigDto.PorcentajeIva));
    }

    [Theory]
    [InlineData("America/Santiago")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("America/Lima")]
    [InlineData("America/Bogota")]
    [InlineData("UTC")]
    public void Validador_ConZonaHorariaValida_DebeAceptar(string zonaHoraria)
    {
        var dto = CreateValidDto();
        dto.ZonaHoraria = zonaHoraria;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Invalida/Zona_Horaria_Falsa")]
    [InlineData("Santiago")]
    public void Validador_ConZonaHorariaInvalida_DebeFallar(string zonaInvalida)
    {
        var dto = CreateValidDto();
        dto.ZonaHoraria = zonaInvalida;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.ActualizarTenantConfigDto.ZonaHoraria));
    }

    [Theory]
    [InlineData("CL")]
    [InlineData("AR")]
    [InlineData("US")]
    [InlineData("PE")]
    [InlineData("CO")]
    [InlineData("MX")]
    [InlineData("BR")]
    [InlineData("cl")] // Minuscula que debe ser aceptada
    public void Validador_ConPaisesSoportados_DebeAceptar(string pais)
    {
        var dto = CreateValidDto();
        dto.Pais = pais;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("C")] // 1 char
    [InlineData("CHL")] // 3 chars
    [InlineData("ZZ")] // No soportado
    public void Validador_ConPaisInvalido_DebeFallar(string paisInvalido)
    {
        var dto = CreateValidDto();
        dto.Pais = paisInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.ActualizarTenantConfigDto.Pais));
    }
}
