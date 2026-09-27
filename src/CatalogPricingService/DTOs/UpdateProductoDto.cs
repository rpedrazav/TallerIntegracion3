using System;

namespace CatalogPricingService.DTOs
{
    public class UpdateProductoDto
    {
        public Guid Id { get; set; } // Se asignará desde la URL
        public Guid TenantId { get; set; } // Se asignará desde el JWT

        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? CodigoBarras { get; set; }
        public string? CodigoQrUrl { get; set; }
        public Guid? CategoriaId { get; set; }
        public Guid UomBaseId { get; set; }
        public decimal PrecioBase { get; set; }
        public bool EsPesoVariable { get; set; }
        public bool IsActive { get; set; }
    }
}
