using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxComplianceService.Data.Migrations
{
    /// <summary>
    /// Corrige la carrera de creación de la secuencia por tenant en
    /// <c>obtener_correlativo_comprobante(uuid)</c>.
    ///
    /// <para>
    /// <c>CREATE SEQUENCE IF NOT EXISTS</c> no es atómico: evalúa la existencia y luego crea, de
    /// modo que varias transacciones concurrentes pueden pasar el chequeo y competir por el
    /// índice <c>pg_class_relname_nsp_index</c>. La perdedora recibía
    /// <c>23505 duplicate key value</c> y el endpoint devolvía <c>500</c>.
    /// </para>
    ///
    /// <para>
    /// Se envuelve la creación en un bloque <c>EXCEPTION</c> que absorbe
    /// <c>unique_violation</c> y <c>duplicate_table</c>: si otra transacción ganó la carrera, la
    /// secuencia ya existe y se puede seguir con <c>nextval</c>. El <c>nextval</c> en sí siempre
    /// fue atómico y no se modifica.
    /// </para>
    ///
    /// <para>
    /// Solo afectaba al primer comprobante de un tenant nuevo: con la secuencia ya creada el
    /// <c>CREATE</c> nunca se ejecutaba, por eso el defecto no aparecía en tenants existentes.
    /// Reproducido con 5 POST simultáneos contra un tenant sin secuencia: 1× 201 y 4× 500.
    /// Verificado tras el fix: 5× 201 con correlativos 1..5.
    /// </para>
    /// </summary>
    public partial class FixComprobanteCorrelativeSequenceRace : Migration
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
                    -- CREATE SEQUENCE IF NOT EXISTS es TOCTOU: si dos peticiones llegan juntas
                    -- para un tenant sin secuencia, ambas pasan el chequeo de existencia y la
                    -- segunda choca con pg_class_relname_nsp_index (23505). Se captura esa
                    -- excepcion porque significa que la otra transaccion ya la creo.
                    BEGIN
                        EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %I START 1', seq_name);
                    EXCEPTION
                        WHEN unique_violation OR duplicate_table THEN
                            NULL; -- la gano una transaccion concurrente, seguimos con nextval
                    END;

                    -- nextval es atomico en PostgreSQL: garantiza correlativos unicos
                    -- aunque varias transacciones lo invoquen al mismo tiempo.
                    RETURN nextval(seq_name::regclass);
                END;
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaura la version con la carrera TOCTOU.
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
    }
}
