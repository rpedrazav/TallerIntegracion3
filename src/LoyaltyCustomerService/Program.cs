using Confluent.Kafka;
using LoyaltyCustomerService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Base de Datos: PostgreSQL con EF Core ───────────────────────────────
builder.Services.AddDbContext<LoyaltyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 2. Kafka Consumer & Producer ──────────────────────────────────────────
// MS-8 consume los siguientes topics según la arquitectura event-driven (D-6):
//   · sale.completed → acumula puntos al ClienteAfiliado, crea MovimientoPuntos
//     y actualiza SaldoPuntos. Una vez procesado, publica points.updated.
//   · sale.reversed  → revierte puntos acumulados en caso de anulación de venta.
//
// MS-8 produce el siguiente topic:
//   · points.updated → consumido por MS-7 (Analytics) para métricas de lealtad
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var kafkaGroupId   = builder.Configuration["Kafka:GroupId"]          ?? "loyalty-customer-service";

builder.Services.AddSingleton<IConsumer<string, string>>(_ =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaBootstrap,
        GroupId          = kafkaGroupId,
        AutoOffsetReset  = AutoOffsetReset.Earliest,
        EnableAutoCommit = false   // Commit manual para garantizar at-least-once
    };
    return new ConsumerBuilder<string, string>(config).Build();
});

builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = kafkaBootstrap,
        Acks             = Acks.All   // Garantía de durabilidad: todos los ISR confirman
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// ─── 3. HttpClient para integraciones externas ──────────────────────────────
// MS-8 puede consultar servicios externos para:
//   · Enviar estados de cuenta por email al ClienteAfiliado
//   · Validar QR codes con un servicio externo
builder.Services.AddHttpClient("ServiciosMensajeria", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ─── 4. CORS ─────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ElectronApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── 5. Controllers + Swagger ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GlobalMart OS — Loyalty & Customer Service (MS-8)", Version = "v1" });
});

// ─── 6. Health Checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ─── Pipeline de Middlewares ──────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-8 Loyalty & Customer v1"));
}

app.UseHttpsRedirection();
app.UseCors("ElectronApp");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ─── Auto-migración en desarrollo ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LoyaltyDbContext>();
    db.Database.Migrate();
}

app.Run();
