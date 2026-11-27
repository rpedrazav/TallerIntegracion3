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

// 1. Base de Datos: PostgreSQL con EF Core
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Autenticación JWT
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

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ElectronApp", policy =>
    {
        // El frontend Electron no tiene un origen HTTP fijo; en desarrollo permitimos localhost
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "GlobalMart OS - Tenant & Identity Service (MS-1)", 
        Version = "v1",
        Description = "Microservicio encargado de la autenticacion de usuarios, gestion de tenants, roles y sucursales."
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "bearer", 
        In = Microsoft.OpenApi.Models.ParameterLocation.Header, 
        Name = "Authorization",
        BearerFormat = "JWT",
        Description = "Ingresa el JWT token generado en el endpoint de Login."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// 5. Health Checks
builder.Services.AddHealthChecks();

// 6. Servicios de Autenticación (Tarea Rodrigo W2)
// Scoped: una instancia por request HTTP (correcto para servicios con DbContext)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ISucursalRepository, SucursalRepository>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IJwtService,     JwtService>();

// FluentValidation: registra automáticamente todos los validators del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
{
    await AdminBootstrap.RunAsync(app.Services);
    return;
}

// Pipeline de Middlewares

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.SerializeAsV2 = true);
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-1 Tenant & Identity v1"));
}

app.UseHttpsRedirection();
app.UseCors("ElectronApp");
app.UseAuthentication();   // PRIMERO: valida el JWT
app.UseAuthorization();    // DESPUÉS: verifica permisos
app.UseTenantMiddleware(); // ÚLTIMO en auth chain: extrae tenant_id y lo inyecta en DbContext

app.MapControllers();
app.MapHealthChecks("/health");

// Auto-migración y seed de desarrollo (minimarkets demo y donpepe, ver Data/SeedData.cs)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

app.Run();

public partial class Program { }



