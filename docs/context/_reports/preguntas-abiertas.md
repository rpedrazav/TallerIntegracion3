---
id: preguntas-abiertas
tipo: reporte
titulo: Preguntas Abiertas — NO VERIFICADO
fuentes: [src/, GlobalMart_ContextMaster.md]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
---
# Preguntas Abiertas — NO VERIFICADO

> Items que no pudieron verificarse con el código fuente disponible. Se necesita consultar al equipo o verificar archivos no analizados.

## Preguntas sobre Implementación

| # | Pregunta | Por qué no verificado |
|---|---------|----------------------|
| PA-01 | ¿El `sucursal_id` se valida en algún lugar del sistema? | No se encontró endpoint de gestión de sucursales |
| PA-02 | ¿VentasController.cs (inicio) tiene `POST /ventas` y `POST /ventas/{id}/cobrar`? Solo se leyó el final del archivo | El archivo fue truncado en la lectura; VentaService.CrearAsync y CompletarAsync existen pero el endpoint HTTP no fue verificado |
| PA-03 | ¿El Admin.tsx tiene contenido real o es una página vacía? | Solo se leyó el inicio de Pos.tsx; Admin.tsx no fue leído |
| PA-04 | ¿Hay un RoleSwitcher en algún archivo del frontend no listado? | Solo se listaron 12 archivos .tsx/.ts en el frontend |
| PA-05 | ¿El preload.ts expone APIs de hardware (SerialPort, cajón)? | preload.ts no fue leído completamente |
| PA-06 | ¿Los puertos reales de MS-6, MS-7, MS-8 son 6001, 7001, 8001? | launchSettings.json de esos servicios no fueron verificados |
| PA-07 | ¿LoyaltyCustomerService.Program.cs tiene Kafka handlers configurados o solo registrados en DI? | Solo se vio un grep parcial del Program.cs |
| PA-08 | ¿Existe un `migrations/` en CatalogPricingService con el índice trigram? | Se vio mención de la migración en la lista de archivos |

## Preguntas sobre Diseño

| # | Pregunta | Origen |
|---|---------|--------|
| PA-09 | ¿Cuál es la `JwtExpirationHours` configurada por defecto? | No se leyó appsettings.json de MS-1 |
| PA-10 | ¿El seed de dev en MS-3 crea productos de ejemplo? | Program.cs de MS-3 no fue analizado completamente |
| PA-11 | ¿Existe un `env.example` con documentación de todas las variables? | Se mencionó pero no se leyó |
| PA-12 | ¿Los diagramas en `diagramas-casos-uso/nuevo/` están actualizados respecto al código? | Archivos .mmd no fueron leídos |
| PA-13 | ¿`docs/informe.pdf` y `docs/contexto_general.md` tienen información adicional no en el ContextMaster? | Archivos no leídos |

## Preguntas sobre el Equipo y el Proceso

| # | Pregunta |
|---|---------|
| PA-14 | ¿El sprint actual es Sprint 2, 3 o cuál? Las tareas TI3-179..195 ¿son todas del mismo sprint? |
| PA-15 | ¿Hay una decisión sobre qué pasarela de pago usar (Transbank vs Stripe)? |
| PA-16 | ¿El certificado digital del SII ya fue obtenido o es futuro? |
| PA-17 | ¿Hay sucursales de prueba configuradas manualmente en algún seed? |
| PA-18 | ¿El frontend de Electron tiene auto-update configurado con electron-updater? |

## Formato para actualizar este archivo

Al resolver una pregunta, moverla a la sección de discrepancias confirmadas o simplemente eliminarla de este archivo:
```
Estado: RESUELTO el YYYY-MM-DD
Respuesta: [respuesta verificada]
Fuente: [archivo o persona que confirmó]
```
