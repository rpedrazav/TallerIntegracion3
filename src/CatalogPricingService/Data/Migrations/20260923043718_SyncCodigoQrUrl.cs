using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogPricingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncCodigoQrUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productos_categorias_categoria_id",
                table: "productos");

            migrationBuilder.RenameColumn(
                name: "precio",
                table: "productos",
                newName: "precio_base");

            migrationBuilder.RenameColumn(
                name: "activo",
                table: "productos",
                newName: "is_active");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                table: "productos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "categoria_id",
                table: "productos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "codigo_qr_url",
                table: "productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "productos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_peso_variable",
                table: "productos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "uom_base_id",
                table: "productos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_productos_categorias_categoria_id",
                table: "productos",
                column: "categoria_id",
                principalTable: "categorias",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productos_categorias_categoria_id",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "codigo_qr_url",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "es_peso_variable",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "uom_base_id",
                table: "productos");

            migrationBuilder.RenameColumn(
                name: "precio_base",
                table: "productos",
                newName: "precio");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "productos",
                newName: "activo");

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                table: "productos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<Guid>(
                name: "categoria_id",
                table: "productos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_productos_categorias_categoria_id",
                table: "productos",
                column: "categoria_id",
                principalTable: "categorias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
