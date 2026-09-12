using Microsoft.EntityFrameworkCore;

namespace AnalyticsNotificationService.Data;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    // DbSet<Entidad> se agregarán aquí a medida que se definan los modelos
}
