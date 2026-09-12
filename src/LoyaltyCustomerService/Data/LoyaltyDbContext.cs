using Microsoft.EntityFrameworkCore;

namespace LoyaltyCustomerService.Data;

public class LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : DbContext(options)
{
    // DbSet<Entidad> se agregarán aquí a medida que se definan los modelos
}
