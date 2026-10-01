using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantIdentityService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursalUniqueNombreIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Unicidad del nombre de sucursal por tenant, case-insensitive.
            //
            // Se crea con SQL crudo a propósito: EF Core 8 no modela índices de expresión, y un
            // índice normal sobre ("TenantId", "Nombre") sería case-SENSITIVE. Con lower() el
            // motor resuelve "Centro" == "centro" y es la garantía real contra la carrera de
            // dos POST simultáneos con el mismo nombre (SucursalRepository.CreateAsync).
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_Sucursales_TenantId_NombreLower"
                    ON "Sucursales" ("TenantId", lower("Nombre"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Sucursales_TenantId_NombreLower\";");
        }
    }
}
