namespace TenantIdentityService.Exceptions;

/// <summary>Ya existe un minimarket con ese código o nombre. El controller la traduce a 409.</summary>
public class DuplicateTenantException(string codigo)
    : Exception($"Ya existe un minimarket con el código '{codigo}' o con ese nombre.");
