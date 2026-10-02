using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInventoryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventosKafkaProcesados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_kafka_procesados",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procesado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_kafka_procesados", x => x.event_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_kafka_procesados");
        }
    }
}
