using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogPricingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columnas de jerarquia y auditoria a la tabla categorias
            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                table: "categorias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "level",
                table: "categorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "categorias",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "categorias",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // FK auto-referenciada: parent_id -> categorias.id (restriccion para evitar cascade ciclico)
            migrationBuilder.AddForeignKey(
                name: "FK_categorias_categorias_parent_id",
                table: "categorias",
                column: "parent_id",
                principalTable: "categorias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Indice para acelerar consultas de subcategorias por padre
            migrationBuilder.CreateIndex(
                name: "IX_categorias_parent_id",
                table: "categorias",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categorias_categorias_parent_id",
                table: "categorias");

            migrationBuilder.DropIndex(
                name: "IX_categorias_parent_id",
                table: "categorias");

            migrationBuilder.DropColumn(name: "parent_id",  table: "categorias");
            migrationBuilder.DropColumn(name: "level",      table: "categorias");
            migrationBuilder.DropColumn(name: "is_active",  table: "categorias");
            migrationBuilder.DropColumn(name: "created_at", table: "categorias");
        }
    }
}
