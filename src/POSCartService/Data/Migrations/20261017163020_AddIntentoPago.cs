using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSCartService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntentoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntentosPago",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorizationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeclineCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentosPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntentosPago_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPago_TenantId_IdempotencyKey",
                table: "IntentosPago",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPago_TenantId_VentaId",
                table: "IntentosPago",
                columns: new[] { "TenantId", "VentaId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntentosPago_VentaId",
                table: "IntentosPago",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntentosPago");
        }
    }
}
