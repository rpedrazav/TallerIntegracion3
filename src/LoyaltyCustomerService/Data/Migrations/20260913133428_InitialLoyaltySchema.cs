using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoyaltyCustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLoyaltySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tiers_membresia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    puntos_minimos = table.Column<int>(type: "integer", nullable: false),
                    beneficios_json = table.Column<string>(type: "jsonb", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tiers_membresia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes_afiliados",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    qr_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes_afiliados", x => x.id);
                    table.ForeignKey(
                        name: "FK_clientes_afiliados_tiers_membresia_tier_id",
                        column: x => x.tier_id,
                        principalTable: "tiers_membresia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_puntos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    puntos = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_puntos", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_puntos_clientes_afiliados_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes_afiliados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saldos_puntos",
                columns: table => new
                {
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntos_disponibles = table.Column<int>(type: "integer", nullable: false),
                    puntos_historicos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saldos_puntos", x => x.cliente_id);
                    table.ForeignKey(
                        name: "FK_saldos_puntos_clientes_afiliados_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes_afiliados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_clientes_afiliados_tenant",
                table: "clientes_afiliados",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_clientes_afiliados_tenant_email",
                table: "clientes_afiliados",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_clientes_afiliados_tenant_qr",
                table: "clientes_afiliados",
                columns: new[] { "tenant_id", "qr_code" });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_afiliados_tier_id",
                table: "clientes_afiliados",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "idx_movimientos_puntos_cliente",
                table: "movimientos_puntos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "idx_movimientos_puntos_cliente_timestamp",
                table: "movimientos_puntos",
                columns: new[] { "cliente_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "idx_tiers_membresia_tenant",
                table: "tiers_membresia",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos_puntos");

            migrationBuilder.DropTable(
                name: "saldos_puntos");

            migrationBuilder.DropTable(
                name: "clientes_afiliados");

            migrationBuilder.DropTable(
                name: "tiers_membresia");
        }
    }
}
