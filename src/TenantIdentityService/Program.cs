using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TenantIdentityService.Data;
using TenantIdentityService.Middleware;
using TenantIdentityService.Repositories;
using TenantIdentityService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Base de Datos: PostgreSQL con EF Core ───────────────────────────────
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 2. Autenticación JWT ────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero  // Sin margen de tolerancia en expiración
        };
    });

builder.Services.AddAuthorization();

// ─── 3. CORS ─────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ElectronApp", policy =>
    {
        // El frontend Electron no tiene un origen HTTP fijo; en desarrollo permitimos localhost
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── 4. Controllers + Swagger ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GlobalMart OS — Tenant & Identity Service (MS-1)", Version = "v1" });

    // Botón de "Authorize" en Swagger para enviar el JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Ingresa el JWT token (sin 'Bearer ' al inicio)"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── 5. Health Checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─── 6. Servicios de Autenticación (Tarea Rodrigo W2) ────────────────────────
// Scoped: una instancia por request HTTP (correcto para servicios con DbContext)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IJwtService,     JwtService>();

// FluentValidation: registra automáticamente todos los validators del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// ─── Pipeline de Middlewares ──────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-1 Tenant & Identity v1"));
}

app.UseHttpsRedirection();
app.UseCors("ElectronApp");
app.UseAuthentication();   // PRIMERO: valida el JWT
app.UseAuthorization();    // DESPUÉS: verifica permisos
app.UseTenantMiddleware(); // ÚLTIMO en auth chain: extrae tenant_id y lo inyecta en DbContext

app.MapControllers();
app.MapHealthChecks("/health");

// ─── Auto-migración y seed de desarrollo ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    db.Database.Migrate(); // Aplica migraciones pendientes al iniciar

    // Seed mínimo: crea un tenant y un usuario CAJERO de prueba si no existen.
    // Credenciales de prueba → email: cajero@demo.cl | password: demo1234
    // TenantId fijo para facilitar las pruebas con Postman.
    var tenantIdDemo = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    if (!db.Tenants.IgnoreQueryFilters().Any(t => t.Id == tenantIdDemo))
    {
        var tenant = new TenantIdentityService.Models.Tenant
        {
            Id           = tenantIdDemo,
            Nombre       = "Minimarket Demo",
            Pais         = "CL",
            Moneda       = "CLP",
            Idioma       = "es",
            ZonaHoraria  = "America/Santiago",
            PorcentajeIva = 19
        };

        var rolCajeroId = Guid.Parse("11111111-0000-0000-0000-000000000001"); // Seed de roles en DbContext

        var usuario = new TenantIdentityService.Models.Usuario
        {
            Id           = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
            TenantId     = tenantIdDemo,
            Nombre       = "Cajero Demo",
            Email        = "cajero@demo.cl",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo1234"),
            Activo       = true
        };

        var usuarioRol = new TenantIdentityService.Models.UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId     = rolCajeroId
        };

        db.Tenants.Add(tenant);
        db.Usuarios.Add(usuario);
        db.UsuarioRoles.Add(usuarioRol);
        db.SaveChanges();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "Seed de desarrollo aplicado. Login de prueba → email: cajero@demo.cl | password: demo1234 | tenantId: {TenantId}",
            tenantIdDemo);
    }
}

app.Run();

