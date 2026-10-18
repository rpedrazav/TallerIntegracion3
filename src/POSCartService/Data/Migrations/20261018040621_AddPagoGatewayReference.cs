using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSCartService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoGatewayReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "Pagos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "Pagos");
        }
    }
}
