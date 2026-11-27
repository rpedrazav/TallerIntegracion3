using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantIdentityService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Tenants existentes: el demo recibe "demo"; el resto conserva su GUID como código para no perder el login.
            migrationBuilder.Sql("""
                UPDATE "Tenants" SET "Codigo" = CASE
                    WHEN "Id" = 'aaaaaaaa-0000-0000-0000-000000000001' THEN 'demo'
                    ELSE "Id"::text END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Codigo",
                table: "Tenants",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Codigo",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Tenants");
        }
    }
}
