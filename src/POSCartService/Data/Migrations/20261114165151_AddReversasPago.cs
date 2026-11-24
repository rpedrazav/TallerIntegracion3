using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSCartService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReversasPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReversasPago",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenciaPago = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    ReferenciaReversa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CodigoRechazo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AutorizadoPor = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReversasPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReversasPago_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReversasPago_TenantId_VentaId",
                table: "ReversasPago",
                columns: new[] { "TenantId", "VentaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReversasPago_VentaId",
                table: "ReversasPago",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReversasPago");
        }
    }
}
