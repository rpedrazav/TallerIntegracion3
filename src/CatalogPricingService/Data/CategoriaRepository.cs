using Microsoft.EntityFrameworkCore;
using CatalogPricingService.Models;

namespace CatalogPricingService.Data
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly CatalogDbContext _context;

        public CategoriaRepository(CatalogDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Categoria>> GetAllAsync(Guid tenantId)
        {
            // Aislamiento estricto multi-tenant: siempre filtramos por el tenant activo.
            // El DbContext tiene un Global Query Filter para Categoria (c => c.TenantId == CurrentTenantId),
            // pero aqui filtramos explicitamente tambien por coherencia y seguridad defensiva.
            return await _context.Categorias
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Categoria> CreateAsync(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return categoria;
        }
    }
}
