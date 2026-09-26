using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using TenantIdentityService.Models;

namespace TenantIdentityService.Services;

/// <summary>
/// Genera tokens JWT firmados con HMAC-SHA256.
///
/// Claims incluidos en el token (accesibles desde cualquier microservicio que comparta la misma key):
/// <list type="bullet">
///   <item><description><b>sub</b>        — user_id (Guid del usuario)</description></item>
///   <item><description><b>tenant_id</b>  — Guid del tenant al que pertenece el usuario</description></item>
///   <item><description><b>email</b>      — Email del usuario</description></item>
///   <item><description><b>nombre</b>     — Nombre completo del usuario</description></item>
///   <item><description><b>roles</b>      — Array JSON de todos los roles asignados (ej: ["CAJERO","REPONEDOR"])</description></item>
///   <item><description><b>active_role</b>— Rol de mayor prioridad, usado por el frontend para decidir la pantalla inicial</description></item>
///   <item><description><b>sucursal_id</b>— Primera sucursal asignada al usuario (vacío si no tiene ninguna)</description></item>
///   <item><description><b>exp</b>        — Expiración en horas configurada en appsettings.json (por defecto 8h)</description></item>
/// </list>
/// </summary>
public class JwtService : IJwtService
{
    // Orden de prioridad para determinar el active_role.
    // El rol con índice más bajo tiene mayor prioridad.
    private static readonly string[] PrioridadRoles =
    [
        "SUPER_ADMIN",
        "ADMIN",
        "CAJERO",
        "REPONEDOR",
        "CLIENTE_AFILIADO"
    ];

    private readonly IConfiguration _config;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration config, ILogger<JwtService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateToken(Usuario usuario)
    {
        // ── 1. Leer configuración desde appsettings.json ──────────────────────
        var jwtKey    = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado en appsettings.json");
        var issuer    = _config["Jwt:Issuer"]    ?? "GlobalMartOS";
        var audience  = _config["Jwt:Audience"]  ?? "GlobalMartOS_Clients";
        var horasExp  = _config.GetValue<int>("Jwt:ExpirationHours", 8);

        // ── 2. Construir la lista de roles del usuario ────────────────────────
        var roles = usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .ToList();

        // ── 3. Determinar el active_role (rol de mayor prioridad) ─────────────
        //    Si el usuario no tiene roles asignados, se asigna "CAJERO" como fallback seguro.
        var activeRole = PrioridadRoles
            .FirstOrDefault(r => roles.Contains(r)) ?? "CAJERO";

        // ── 4. Obtener la primera sucursal asignada al usuario ─────────────────
        var sucursalId = usuario.UsuarioSucursales.FirstOrDefault()?.SucursalId.ToString()
                         ?? string.Empty;

        // ── 5. Construir los claims del JWT ───────────────────────────────────
        var claims = new List<Claim>
        {
            // Claim estándar: subject = identificador único del usuario
            new(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),  // ID único del token
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),

            // Claims custom del dominio GlobalMart OS
            new("tenant_id",   usuario.TenantId.ToString()),
            new("email",       usuario.Email),
            new("nombre",      usuario.Nombre),
            new("active_role", activeRole),
            new("sucursal_id", sucursalId),

            // roles[] como array JSON para que cualquier microservicio pueda deserializarlo
            new("roles", JsonSerializer.Serialize(roles)),
        };

        // Claims individuales requeridos por [Authorize(Roles = "ADMIN")].
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // ── 6. Firmar y construir el token ─────────────────────────────────────
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiracion  = DateTime.UtcNow.AddHours(horasExp);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiracion,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug(
            "JWT generado para usuario {UsuarioId} | active_role: {ActiveRole} | roles: {Roles} | exp: {Exp}",
            usuario.Id, activeRole, string.Join(",", roles), expiracion);

        return tokenString;
    }
}
