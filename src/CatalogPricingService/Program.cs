using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using CatalogPricingService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// InyecciÃ³n de Controladores y Auto-ValidaciÃ³n
builder.Services.AddControllers();

// CORS — permite peticiones desde el renderer de Electron y Swagger UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)   // Electron usa file:// y localhost dinámico
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GlobalMartOS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GlobalMartOS_Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:SecretKey"] ?? builder.Configuration["Jwt:Key"] ?? "GlobalMartOS_SuperSecretKey_ChangeInProduction_Min32Chars!!"))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "GlobalMart OS - Catalog & Pricing Service (MS-3)", 
        Version = "v1",
        Description = "Microservicio encargado de la gestion del catalogo de productos, categorias y listas de precios."
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

var app = builder.Build();

// Pipeline de Middlewares (Orden estricto)
app.UseCors(); // debe ir ANTES de Authentication

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.SerializeAsV2 = true);
    app.UseSwaggerUI();
}

app.UseAuthentication(); // 1. Verifica la firma del token
app.UseAuthorization();  // 2. Verifica los roles del usuario
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }


