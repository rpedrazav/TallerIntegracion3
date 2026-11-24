# CI-256 — Timeout en TenantConfig

Fecha: 2026-11-23. Rama: dev.

## Causa y cambio

Se reprodujo el timeout de 20 segundos de `e2e/tenant-config.spec.ts`. La captura de accesibilidad muestra configuración cargada (CL, CLP, IVA 19), campos deshabilitados y botón «Solo Administradores»: [contexto original](fallo-original.md).

El JWT de `e2e/renderer.tsx` incluía `roles: ['ADMIN']` pero carecía de `active_role`. `useTenantStore` determina permisos exclusivamente por el rol activo. Por tanto el test intentaba editar un campo deshabilitado. Se añadió `active_role: 'ADMIN'` a la fixture; no se modificó la autorización productiva.

La explicación anterior sobre el prefijo `/api` era incorrecta: el glob `**/tenants/tenant-a/config` ya podía coincidir con esa URL. Se conserva el patrón más explícito incorporado anteriormente, pero no fue la solución del timeout.

El spec ahora verifica carga del país, campo IVA habilitado, botón habilitado y confirmación visible de guardado. Verificar el país evita confundir el IVA predeterminado 19 con una carga HTTP completada. Se revisaron los otros tokens E2E: las fixtures administrativas equivalentes ya contienen `active_role`; los casos que lo omiten deliberadamente comprueban denegación.

El job frontend de `.github/workflows/ci.yml` guarda `playwright-report` y `test-results` como artefacto `frontend-regression-failure` si falla, con siete días de retención. Facilita recuperar capturas, contexto y trazas sin depender solo del resumen del log.

## Verificación

- Reproducción inicial: 1/1 fallida, exit code 1, timeout y formulario en modo lectura.
- Suite renderer completa: **83/83 aprobadas**, exit code 0, 1.3 minutos. Incluye TenantConfig, administración, anulación, tarjeta y pesaje.
- Pruebas de módulos: **17/17 archivos aprobados**, exit code 0 (el runner local informa archivos, no el número de assertions internas).
- `npm run typecheck`: exit code 0.
- `git diff --check`: aprobado.
- Evidencia: [Playwright](playwright.log), [JSON](resultados.json), [módulos](modulos.log), [TypeScript](typecheck.log).

## Reproducción

Desde `globalmart-frontend` con dependencias instaladas:

```bash
npm run typecheck
node --test tests/*.test.cjs
PLAYWRIGHT_JSON_OUTPUT_FILE=../docs/qa/CI-256/resultados.json npx playwright test --reporter=list,json
```

## Límites

Ejecución local en Node 26.13.1 y Chromium con APIs simuladas; el workflow usa Node 20. Faltaba localmente `ts-node/register/transpile-only`, dependencia ya declarada del proyecto: se instaló ts-node 10.9.2 en `/tmp/globalmart-ci256-deps` y se ejecutaron los módulos con `NODE_PATH=/tmp/globalmart-ci256-deps/node_modules`. No se alteraron package.json ni lockfile. El primer intento de módulos sin esa dependencia falló antes de ejecutar dos archivos; la corrida final pasó completa.

Las cinco capturas históricas S2-110 regeneradas por la suite fueron restauradas; estaban limpias antes de ejecutar. No se tocó clúster ni se reejecutó .NET: el usuario aportó evidencia de .NET y Electron aprobados en Actions. El siguiente workflow remoto y la descarga del nuevo artefacto están [NO VERIFICADOS]. Las pruebas locales no garantizan ausencia de fallos futuros.
