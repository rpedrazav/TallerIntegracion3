using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxComplianceService.Data.Migrations
{
    /// <summary>
    /// Agrega la función <c>obtener_correlativo_comprobante(uuid)</c>, que crea la secuencia
    /// por tenant si no existe y retorna el siguiente valor con <c>nextval</c>.
    /// <c>nextval</c> es atómico a nivel de PostgreSQL, por lo que garantiza unicidad del
    /// correlativo incluso con requests concurrentes.
    /// Complementa a <c>crear_secuencia_comprobante(uuid)</c>, que se conserva sin cambios.
    /// </summary>
    public partial class AddComprobanteCorrelativeFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION obtener_correlativo_comprobante(p_tenant_id uuid)
                RETURNS bigint
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    seq_name text := 'comprobante_seq_' || p_tenant_id::text;
                BEGIN
                    EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %I START 1', seq_name);
                    RETURN nextval(seq_name::regclass);
                END;
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS obtener_correlativo_comprobante(uuid);
            ");
        }
    }
}
