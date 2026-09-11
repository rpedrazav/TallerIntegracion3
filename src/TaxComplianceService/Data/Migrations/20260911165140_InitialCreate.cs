using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxComplianceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesFiscales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Prefijo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    FolioActual = table.Column<long>(type: "bigint", nullable: false),
                    FolioMaximo = table.Column<long>(type: "bigint", nullable: false),
                    PorcentajeImpuesto = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PaisEntidadFiscal = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesFiscales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosTributarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfiguracionFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE"),
                    ContenidoXml = table.Column<string>(type: "text", nullable: false),
                    CodigoAutorizacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MontoImpuesto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Reintentos = table.Column<int>(type: "integer", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmitidoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosTributarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosTributarios_ConfiguracionesFiscales_Configuracion~",
                        column: x => x.ConfiguracionFiscalId,
                        principalTable: "ConfiguracionesFiscales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesFiscales_TenantId_TipoDocumento",
                table: "ConfiguracionesFiscales",
                columns: new[] { "TenantId", "TipoDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosTributarios_ConfiguracionFiscalId",
                table: "DocumentosTributarios",
                column: "ConfiguracionFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosTributarios_TenantId_Numero",
                table: "DocumentosTributarios",
                columns: new[] { "TenantId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosTributarios");

            migrationBuilder.DropTable(
                name: "ConfiguracionesFiscales");
        }
    }
}
