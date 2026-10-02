using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using WarehouseInventoryService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Inyección de Controladores y Auto-Validación
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

// Inyección de Base de Datos PostgreSQL
builder.Services.AddDbContext<WarehouseInventoryService.Data.WarehouseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<WarehouseInventoryService.Messaging.ISaleEventProcessor, WarehouseInventoryService.Messaging.SaleEventProcessor>();
builder.Services.AddHostedService<WarehouseInventoryService.Messaging.KafkaConsumerService>();

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GlobalMartOS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GlobalMartOS_Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? builder.Configuration["Jwt:SecretKey"] ?? "GlobalMartOS_SuperSecretKey_ChangeInProduction_Min32Chars!!"))
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

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WarehouseInventoryService.Data.WarehouseDbContext>();
    db.Database.Migrate();
    WarehouseInventoryService.Data.SeedData.Initialize(db);
}

app.UseAuthentication(); // 1. Verifica la firma del token
app.UseAuthorization();  // 2. Verifica los roles del usuario
app.UseMiddleware<WarehouseInventoryService.Middleware.TenantMiddleware>();
app.MapControllers();

app.Run();
