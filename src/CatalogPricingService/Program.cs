using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using CatalogPricingService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// InyecciÃ³n de Controladores y Auto-ValidaciÃ³n
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

// InyecciÃ³n de Base de Datos PostgreSQL
builder.Services.AddDbContext<CatalogPricingService.Data.CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// InyecciÃ³n del Repositorio
builder.Services.AddScoped<CatalogPricingService.Data.IProductoRepository, CatalogPricingService.Data.ProductoRepository>();
builder.Services.AddScoped<CatalogPricingService.Data.ICategoriaRepository, CatalogPricingService.Data.CategoriaRepository>();

// InyecciÃ³n del Servicio de Productos
builder.Services.AddScoped<CatalogPricingService.Services.IProductoService, CatalogPricingService.Services.ProductoService>();

// InyecciÃ³n del Servicio de Categorias
builder.Services.AddScoped<CatalogPricingService.Services.ICategoriaService, CatalogPricingService.Services.CategoriaService>();

// InyecciÃ³n del Validador de Productos
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.CreateProductoDto>, CatalogPricingService.Validators.CreateProductoDtoValidator>();

// InyecciÃ³n del Validador de ActualizaciÃ³n de Productos (NUEVO - TI3-132)
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.UpdateProductoDto>, CatalogPricingService.Validators.UpdateProductoDtoValidator>();

// InyecciÃ³n del Validador de Categorias
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.CreateCategoriaDto>, CatalogPricingService.Validators.CreateCategoriaDtoValidator>();

// InyecciÃ³n de AutenticaciÃ³n JWT Stateless
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GlobalMart",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GlobalMartUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "TuSuperSecretoDeDesarrollo1234567890!"))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Pipeline de Middlewares (Orden estricto)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.SerializeAsV2 = true);
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogPricingService.Data.CatalogDbContext>();
    db.Database.Migrate();
    CatalogPricingService.Data.SeedData.Initialize(db);
}

app.UseAuthentication(); // 1. Verifica la firma del token
app.UseAuthorization();  // 2. Verifica los roles del usuario
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }

