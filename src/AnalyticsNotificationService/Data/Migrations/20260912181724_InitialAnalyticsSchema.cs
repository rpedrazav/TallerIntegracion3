using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyticsNotificationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAnalyticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alertas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valor_actual = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    umbral = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historial_envios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    destinatario = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_envios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kpi_ventas",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    total_ventas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cant_transacciones = table.Column<int>(type: "integer", nullable: false),
                    ticket_promedio = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_ventas", x => new { x.tenant_id, x.sucursal_id, x.fecha });
                });

            migrationBuilder.CreateIndex(
                name: "idx_alertas_tenant",
                table: "alertas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_alertas_tenant_estado",
                table: "alertas",
                columns: new[] { "tenant_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "idx_historial_envios_tenant",
                table: "historial_envios",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_historial_envios_tenant_timestamp",
                table: "historial_envios",
                columns: new[] { "tenant_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "idx_kpi_ventas_tenant",
                table: "kpi_ventas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_kpi_ventas_tenant_fecha",
                table: "kpi_ventas",
                columns: new[] { "tenant_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "idx_kpi_ventas_tenant_sucursal",
                table: "kpi_ventas",
                columns: new[] { "tenant_id", "sucursal_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas");

            migrationBuilder.DropTable(
                name: "historial_envios");

            migrationBuilder.DropTable(
                name: "kpi_ventas");
        }
    }
}
