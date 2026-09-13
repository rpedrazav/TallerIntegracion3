using AnalyticsNotificationService.Data;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Base de Datos: PostgreSQL con EF Core ───────────────────────────────
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 2. Kafka Consumer ───────────────────────────────────────────────────────
// MS-7 consume los siguientes topics según la arquitectura event-driven (D-6):
//   · sale.completed  → actualiza KPIs de ventas (kpi_ventas)
//   · sale.reversed   → corrige KPIs
//   · stock.alert     → genera Alerta de bajo stock y envía historial_envios
//   · expiry.alert    → genera Alerta de caducidad
//   · stock.updated   → actualiza dashboard de inventario
//   · points.updated  → registra métricas de lealtad (publicado por MS-8)
//   · fx.rate.updated → genera alerta de variación FX
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var kafkaGroupId   = builder.Configuration["Kafka:GroupId"]          ?? "analytics-notification-service";

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

// ─── 3. HttpClient para integraciones externas ──────────────────────────────
// MS-7 puede necesitar llamar a proveedores de mensajería (email/SMS) para
// enviar notificaciones registradas en historial_envios.
builder.Services.AddHttpClient("NotificacionesExternas", client =>
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
    c.SwaggerDoc("v1", new() { Title = "GlobalMart OS — Analytics & Notification Service (MS-7)", Version = "v1" });
});

// ─── 6. Health Checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ─── Pipeline de Middlewares ──────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-7 Analytics & Notification v1"));
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
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    db.Database.Migrate();
}

app.Run();
