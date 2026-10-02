using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CatalogPricingService.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260927164215_AddProductoNombreTrgmIndex")]
    public partial class AddProductoNombreTrgmIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Habilitar extensiones necesarias para indices GIN multi-columna con UUID y trigramas
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gin;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Indice GIN con trigramas sobre (tenant_id, nombre) para busquedas LIKE eficientes.
            // Un indice GIN/trgm soporta patron '%texto%' (cualquier posicion), a diferencia
            // de un B-tree que solo optimiza prefijos 'texto%'.
            migrationBuilder.Sql(@"
                CREATE INDEX idx_productos_nombre_trgm
                ON productos
                USING GIN (tenant_id, nombre gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_productos_nombre_trgm;");
        }
    }
}
