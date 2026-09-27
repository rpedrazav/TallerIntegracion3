using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogPricingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoNombreTrgmIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Habilitar la extension pg_trgm necesaria para indices GIN de busqueda LIKE
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
