using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantIdentityService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursalZonaHoraria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                table: "Sucursales",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                table: "Sucursales");
        }
    }
}
