namespace POSCartService.Services;

public interface ISucursalClient
{
    Task<bool> IsActiveAsync(
        Guid sucursalId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
