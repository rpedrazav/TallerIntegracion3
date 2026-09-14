#!/usr/bin/env bash
# ==============================================================================
# Script de Inicialización de Base de Datos PostgreSQL
# Proyecto: GlobalMart OS (INTEGRA3)
# Tarea: TI3-116
#
# Este script se ejecuta automáticamente la PRIMERA VEZ que se crea el volumen
# de datos de cada contenedor PostgreSQL (via /docker-entrypoint-initdb.d/).
#
# Si el volumen ya existe con datos, PostgreSQL NO ejecuta este script.
# Para forzar la re-ejecución: docker compose down -v && docker compose up -d
# ==============================================================================
set -euo pipefail

DB_NAME="${POSTGRES_DB:-postgres}"

echo "============================================================"
echo " [Init DB] Inicializando base de datos: $DB_NAME"
echo "============================================================"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$DB_NAME" <<-EOSQL

    -- =========================================================================
    -- 1. Extensiones requeridas por el proyecto
    -- =========================================================================
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    CREATE EXTENSION IF NOT EXISTS "pgcrypto";

    -- =========================================================================
    -- 2. Tabla de tracking de migraciones (usada por EF Core como referencia)
    -- =========================================================================
    CREATE TABLE IF NOT EXISTS __migrations_history (
        migration_id    VARCHAR(150)    NOT NULL PRIMARY KEY,
        product_version VARCHAR(32)     NOT NULL,
        applied_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
    );

    COMMENT ON TABLE __migrations_history IS 'Registro de migraciones aplicadas por EF Core u otras herramientas';

    -- =========================================================================
    -- 3. Tabla de auditoría base (log inmutable de operaciones críticas)
    -- =========================================================================
    CREATE TABLE IF NOT EXISTS audit_log (
        id              UUID            NOT NULL DEFAULT uuid_generate_v4() PRIMARY KEY,
        tenant_id       UUID            NOT NULL,
        user_id         UUID,
        active_role     VARCHAR(50),
        action          VARCHAR(100)    NOT NULL,
        entity_type     VARCHAR(100),
        entity_id       VARCHAR(100),
        old_values      JSONB,
        new_values      JSONB,
        ip_address      VARCHAR(45),
        user_agent      TEXT,
        created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
    );

    COMMENT ON TABLE audit_log IS 'Log inmutable de auditoría - RNF-05';

    -- Índices para consultas frecuentes de auditoría
    CREATE INDEX IF NOT EXISTS idx_audit_log_tenant    ON audit_log (tenant_id);
    CREATE INDEX IF NOT EXISTS idx_audit_log_user      ON audit_log (user_id);
    CREATE INDEX IF NOT EXISTS idx_audit_log_action    ON audit_log (action);
    CREATE INDEX IF NOT EXISTS idx_audit_log_created   ON audit_log (created_at);
    CREATE INDEX IF NOT EXISTS idx_audit_log_entity    ON audit_log (entity_type, entity_id);

EOSQL

echo "============================================================"
echo " [Init DB] Base de datos '$DB_NAME' inicializada con éxito."
echo "   - Extensiones: uuid-ossp, pgcrypto"
echo "   - Tablas: __migrations_history, audit_log"
echo "============================================================"
