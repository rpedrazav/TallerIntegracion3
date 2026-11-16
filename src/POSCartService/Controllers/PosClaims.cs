using System.Security.Claims;

namespace POSCartService.Controllers;

internal static class PosClaims
{
    public static bool TryGetClaims(this ClaimsPrincipal user, out Guid cajeroId, out Guid tenantId)
    {
        var cajeroClaim = user.FindFirst("cajero_id")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        tenantId = default;

        return Guid.TryParse(cajeroClaim, out cajeroId)
            && Guid.TryParse(user.FindFirst("tenant_id")?.Value, out tenantId);
    }
}
