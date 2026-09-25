using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using CatalogPricingService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Inyección de Controladores y Auto-Validación
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

// Inyección de Base de Datos PostgreSQL
builder.Services.AddDbContext<CatalogPricingService.Data.CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyección del Repositorio
builder.Services.AddScoped<CatalogPricingService.Data.IProductoRepository, CatalogPricingService.Data.ProductoRepository>();
builder.Services.AddScoped<CatalogPricingService.Data.ICategoriaRepository, CatalogPricingService.Data.CategoriaRepository>();

// Inyección del Servicio de Productos
builder.Services.AddScoped<CatalogPricingService.Services.IProductoService, CatalogPricingService.Services.ProductoService>();

// Inyección del Servicio de Categorias
builder.Services.AddScoped<CatalogPricingService.Services.ICategoriaService, CatalogPricingService.Services.CategoriaService>();

// Inyección del Validador de Productos
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.CreateProductoDto>, CatalogPricingService.Validators.CreateProductoDtoValidator>();

// Inyección del Validador de Actualización de Productos (NUEVO - TI3-132)
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.UpdateProductoDto>, CatalogPricingService.Validators.UpdateProductoDtoValidator>();

// Inyección del Validador de Categorias
builder.Services.AddScoped<FluentValidation.IValidator<CatalogPricingService.DTOs.CreateCategoriaDto>, CatalogPricingService.Validators.CreateCategoriaDtoValidator>();

// Inyección de Autenticación JWT Stateless
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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // 1. Verifica la firma del token
app.UseAuthorization();  // 2. Verifica los roles del usuario
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

app.Run();
