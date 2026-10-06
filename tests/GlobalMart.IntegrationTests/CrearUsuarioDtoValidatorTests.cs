extern alias TenantIdentityServiceAlias;

using Xunit;
using Dtos = TenantIdentityServiceAlias::TenantIdentityService.DTOs;
using Validators = TenantIdentityServiceAlias::TenantIdentityService.Validators;

namespace GlobalMart.IntegrationTests;

public class CrearUsuarioDtoValidatorTests
{
    private readonly Validators.CrearUsuarioDtoValidator _validator = new();

    private static Dtos.CrearUsuarioDto CreateValidDto() => new()
    {
        Nombre = "Juan Pérez",
        Email = "juan.perez@minimarket.cl",
        Password = "Password123!"
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
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] // Menos de 2 caracteres
    [InlineData("Juan123")] // No permite dígitos en nombre
    [InlineData("Juan@Perez")] // Carácter no permitido
    public void Validador_ConNombreInvalido_DebeFallar(string nombreInvalido)
    {
        var dto = CreateValidDto();
        dto.Nombre = nombreInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.CrearUsuarioDto.Nombre));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("juan@")]
    [InlineData("@dominio.com")]
    public void Validador_ConEmailInvalido_DebeFallar(string emailInvalido)
    {
        var dto = CreateValidDto();
        dto.Email = emailInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Dtos.CrearUsuarioDto.Email));
    }

    [Theory]
    [InlineData("", "La contraseña es obligatoria.")]
    [InlineData("Pass1!", "La contraseña debe tener al menos 8 caracteres.")] // longitud < 8
    [InlineData("password123!", "La contraseña debe contener al menos una letra mayúscula.")] // sin mayúscula
    [InlineData("PASSWORD123!", "La contraseña debe contener al menos una letra minúscula.")] // sin minúscula
    [InlineData("Password!", "La contraseña debe contener al menos un dígito.")] // sin dígito
    [InlineData("Password123", "La contraseña debe contener al menos un carácter especial")] // sin carácter especial
    public void Validador_ConPasswordDebil_DebeFallarConMensajeApropiado(string passwordInvalido, string mensajeEsperado)
    {
        var dto = CreateValidDto();
        dto.Password = passwordInvalido;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(Dtos.CrearUsuarioDto.Password) &&
            e.ErrorMessage.Contains(mensajeEsperado));
    }
}
