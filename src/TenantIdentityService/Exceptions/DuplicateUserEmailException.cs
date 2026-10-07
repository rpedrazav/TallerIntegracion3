namespace TenantIdentityService.Exceptions;

/// <summary>
/// Se lanza cuando ya existe un usuario con el mismo email en el tenant.
/// El controller la traduce a HTTP 409 Conflict.
/// </summary>
public class DuplicateUserEmailException : Exception
{
    public string EmailDuplicado { get; }

    public DuplicateUserEmailException(string emailDuplicado)
        : base($"Ya existe un usuario con el email '{emailDuplicado}' en este tenant.")
    {
        EmailDuplicado = emailDuplicado;
    }
}
