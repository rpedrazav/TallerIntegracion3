# S3-032 — Regresión del POS tras partir Pos.tsx

Fecha: 2026-11-20. Meta: Q — Calidad y regresión. Estado: **línea base registrada; falta la corrida después de la partición**.

## Objetivo y criterio de aceptación

- Objetivo: ejecutar los E2E de carrito, pesaje y tarjeta después de extraer componentes de `Pos.tsx`.
- Aceptación: los E2E del POS pasan sin cambios en los specs.

## Estado del prerrequisito

La partición todavía no está integrada. En `dev` (`b3ed867`), `globalmart-frontend/src/pages/Pos.tsx` tiene 1220 líneas y su último cambio es `19230e1` (S2-109). Tampoco hay una rama remota con la extracción. Las tareas «Extraer el carrito de Pos.tsx» y «Extraer cobro y recuperación de venta de Pos.tsx» siguen pendientes.

Por eso esta ejecución es la **línea base previa a la partición**. Cuando se integre la extracción, se repite el mismo comando y se compara contra esta tabla.

## Qué se verificó

| Spec | Cubre | Resultado |
|---|---|---|
| `cart.spec.ts` | agregar, cantidad, eliminar, recarga; IVA y totales de MS-5; aislamiento por tenant | 3/3 |
| `weight-cart.spec.ts` | pesaje estable, respaldo manual, producto unitario sin balanza | 3/3 |
| `card-search.spec.ts` | búsqueda/escaneo, tarjeta aprobada, rechazo, timeout y single-flight | 5/5 |
| `cash-payment.spec.ts` | efectivo, vuelto y monto insuficiente | 2/2 |
| `comprobante-seguro.spec.ts` | comprobante de tarjeta y de efectivo | 2/2 |
| `pos-recovery.spec.ts` | recuperación tras recarga y cancelación segura en timeout | 2/2 |
| `demo-mostrador.spec.ts` | flujo completo de mostrador | 1/1 |

**Total: 18/18 aprobadas, exit code 0, 36,9 s.** Log: [`e2e-pos-linea-base.txt`](e2e-pos-linea-base.txt). `npm run typecheck` también pasó.

## Cómo reproducir

Desde `globalmart-frontend`:

```bash
npm run typecheck
npx playwright test cart weight-cart card-search cash-payment pos-recovery comprobante-seguro demo-mostrador
```

En esta máquina, Playwright 1.64.0 no tenía navegadores descargados (`chromium_headless_shell-1248`). Por eso la corrida usó una config temporal fuera del repositorio que extiende `playwright.config.ts` y solo agrega `use.channel: 'chrome'` (Chrome instalado, como pide `e2e/README.md`). No se modificaron specs ni la configuración del repo. La alternativa es `npx playwright install chromium`.

## Límites

- La suite monta `Pos` en el navegador con el puente Electron y las respuestas HTTP interceptadas (ver `e2e/README.md`). No valida backend real, JWT, IPC ni PostgreSQL.
- `demo-mostrador.spec.ts` reescribe las capturas versionadas de `docs/qa/S2-110/capturas/`. Se restauraron con `git checkout` para no mezclarlas con esta evidencia.
- La aceptación de S3-032 **no está cumplida todavía**: queda pendiente repetir la corrida sobre el código partido.
