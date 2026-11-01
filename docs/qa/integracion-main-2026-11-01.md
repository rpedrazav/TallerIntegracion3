# Integración de ramas a main — 2026-11-01

Se integran dev (70f1b4e), local (9b7cb4b), Diego (e2dbb26), copilot/fix-github-actions-job-failure (70f5c7f). docs/context-graph y feat/kong-routes-ms1 ya son ancestros de dev. Se conserva el historial sin force push ni eliminación de ramas.

Conflictos resueltos conservando la administración actual, las migraciones vigentes y las suites de ambas ramas. Se corrigen claves JWT duplicadas, configuración de puerto MS-2, registro del procesador Kafka y contratos de pruebas. Se restringe POST /seed a ADMIN del tenant demo en Development y se aísla el borrador POS por tenant y usuario.

Validación local: build Release sin errores, 47/47 pruebas unitarias, 47/47 pruebas Node, typecheck, empaquetado Electron Windows y 32 escenarios Playwright verificados (31 aprobados en la ejecución conjunta y el restante aprobado tras corregir el selector de Impuestos). La suite dirigida de balanza/cálculo pesable aprobó 62/62 tras adaptar los harness al flujo actual de confirmación. La ejecución conjunta final de backend se registra al terminar.

Docker Desktop no habilitó el motor local durante la validación. Se usa PostgreSQL 18 temporal en puerto 5436; no se usan bases personales. El caso StockConsumerIntegrationTests que requiere Kafka se reserva para CI con Docker; entrega al broker local [NO VERIFICADO]. Las pruebas Electron con pasarela real simulada son opt-in y no están acreditadas por los fixtures del renderer.
