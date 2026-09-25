using Microsoft.EntityFrameworkCore;
using CatalogPricingService.Models;

namespace CatalogPricingService.Data
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly CatalogDbContext _context;

        public ProductoRepository(CatalogDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Producto> Productos, int TotalCount)> GetAllByTenantAsync(Guid tenantId, int page, int pageSize)
        {
            // Aislamiento estricto: Siempre filtramos por el tenant activo
            var query = _context.Productos.Where(p => p.TenantId == tenantId);
            
            var totalCount = await query.CountAsync();
            
            var productos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (productos, totalCount);
        }

        public async Task<(IEnumerable<Producto> Productos, int TotalCount)> SearchByNameAsync(Guid tenantId, string query, int page, int pageSize)
        {
            var q = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Nombre.Contains(query));

            var totalCount = await q.CountAsync();

            var productos = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (productos, totalCount);
        }

        public async Task<Producto?> GetByIdAsync(Guid id, Guid tenantId)
        {
            // Verificamos el ID y que además pertenezca al minimarket correcto
            return await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        }

        public async Task<Producto> CreateAsync(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        public async Task UpdateAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
        }
    }
}