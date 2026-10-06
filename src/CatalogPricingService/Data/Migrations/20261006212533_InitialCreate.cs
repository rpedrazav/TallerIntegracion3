using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogPricingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_categorias_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    codigo_barras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    codigo_qr_url = table.Column<string>(type: "text", nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uom_base_id = table.Column<Guid>(type: "uuid", nullable: false),
                    precio_base = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    es_peso_variable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.id);
                    table.ForeignKey(
                        name: "FK_productos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "precios",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    precio_local = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    moneda_fx = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    precio_fx = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_precios", x => new { x.producto_id, x.sucursal_id });
                    table.ForeignKey(
                        name: "FK_precios_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_categorias_tenant",
                table: "categorias",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_parent_id",
                table: "categorias",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_precios_tenant",
                table: "precios",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_productos_tenant",
                table: "productos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_productos_tenant_barcode",
                table: "productos",
                columns: new[] { "tenant_id", "codigo_barras" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_categoria_id",
                table: "productos",
                column: "categoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "precios");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
