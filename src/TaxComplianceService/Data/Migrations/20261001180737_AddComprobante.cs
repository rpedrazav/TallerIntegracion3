using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxComplianceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroCorrelativo = table.Column<long>(type: "bigint", nullable: false),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CajeroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Items = table.Column<string>(type: "jsonb", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_TenantId_NumeroCorrelativo",
                table: "Comprobantes",
                columns: new[] { "TenantId", "NumeroCorrelativo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comprobantes");
        }
    }
}
