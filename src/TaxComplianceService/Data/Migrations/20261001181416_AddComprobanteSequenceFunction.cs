using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxComplianceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobanteSequenceFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION crear_secuencia_comprobante(p_tenant_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    seq_name text := 'comprobante_seq_' || p_tenant_id::text;
                BEGIN
                    EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %I START 1', seq_name);
                END;
                $$;
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS crear_secuencia_comprobante(uuid);
            ");

        }
    }
}
