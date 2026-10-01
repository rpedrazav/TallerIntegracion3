namespace TenantIdentityService.Exceptions;

/// <summary>
/// Se lanza cuando ya existe una sucursal con el mismo nombre en el tenant.
/// La comparación es case-insensitive: "Centro" y "centro" son el mismo nombre.
/// El índice único <c>IX_Sucursales_TenantId_NombreLower</c> es la garantía real
/// contra condiciones de carrera; esta excepción cubre tanto el chequeo previo
/// (caso común, mensaje claro) como la violación del índice (carrera).
/// El controller la traduce a HTTP 409 Conflict.
/// </summary>
public class DuplicateSucursalNameException : Exception
{
    public string NombreDuplicado { get; }

    public DuplicateSucursalNameException(string nombreDuplicado)
        : base($"Ya existe una sucursal con el nombre '{nombreDuplicado}' en este tenant.")
    {
        NombreDuplicado = nombreDuplicado;
    }
}
