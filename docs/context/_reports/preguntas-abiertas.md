---
id: preguntas-abiertas
tipo: reporte
titulo: Preguntas Abiertas y Respuestas del Equipo
fuentes: [src/, GlobalMart_ContextMaster.md, Equipo de Desarrollo UCT]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Preguntas Abiertas y Respuestas del Equipo

> **Contexto General:** Este es un proyecto académico para la carrera de Ingeniería Civil Informática en la Universidad Católica de Temuco (UCT). Todo componente, certificado o integración que no pueda obtenerse legalmente (certificados digitales reales del SII, terminales de pago bancario físicos) será simulado mediante software/sandboxes.

---

## 1. Preguntas sobre Implementación

| # | Pregunta | Estado | Respuesta / Verificación |
|---|---|---|---|
| **PA-01** | ¿El `sucursal_id` se valida en algún lugar del sistema? | **RESUELTO** | Se implementará en las siguientes semanas junto con el desarrollo de la API. Actualmente no hay validación de entidad en BD. |
| **PA-02** | ¿`VentasController.cs` tiene `POST /ventas` y `POST /cobrar`? | **RESUELTO** | [`POST /ventas`](src/POSCartService/Controllers/VentasController.cs) **sí existe** y crea la venta. Sin embargo, `cobrar` y `anular` solo existen como métodos en `VentaService.cs`; no están expuestos en el controlador todavía. |
| **PA-03** | ¿`Admin.tsx` tiene contenido real o es un placeholder? | **RESUELTO** | Es un placeholder temporal (`<h2>Administración</h2>`). Se acordó dejar la ruta directa para facilitar pruebas de desarrollo del POS sin alternar credenciales constantemente. |
| **PA-04** | ¿Hay un `RoleSwitcher` en algún archivo del frontend? | **RESUELTO** | **No**. El frontend comenzó a desarrollarse en la semana 3 y se centraliza en `globalmart-frontend/`. |
| **PA-05** | ¿`preload.ts` expone APIs de hardware (SerialPort, cajón)? | **RESUELTO** | **No**. Verificado en código: `preload.ts` solo expone métodos de sesión (`ping`, `getToken`, `setToken`, `logout`). |
| **PA-06** | ¿Los puertos reales de MS-6, MS-7 y MS-8 son 6001, 7001, 8001? | **RESUELTO** | No, usan el rango 5124 y asociados según la configuración de los microservicios. |
| **PA-07** | ¿`LoyaltyCustomerService.Program.cs` tiene Kafka handlers o solo registro en DI? | **RESUELTO** | Verificado en código: `Program.cs` registra el Consumer y Producer de Kafka como Singletons en DI, pero no tiene implementado ningún `IHostedService` ni loop de consumo de eventos. |
| **PA-08** | ¿Existe la migración del índice trigram en CatalogPricingService? | **RESUELTO** | **Sí**, confirmada en `src/CatalogPricingService/Data/Migrations/20260927164215_AddProductoNombreTrgmIndex.cs`. |

---

## 2. Preguntas sobre Diseño

| # | Pregunta | Estado | Respuesta / Verificación |
|---|---|---|---|
| **PA-09** | ¿Cuál es la `JwtExpirationHours` configurada por defecto? | **RESUELTO** | Verificado en `src/TenantIdentityService/appsettings.json`: fijada en **8 horas**. |
| **PA-10** | ¿El seed de dev en MS-3 crea productos de ejemplo? | **RESUELTO** | No hay seed de productos configurado en MS-3 (solo la creación de tablas). |
| **PA-11** | ¿Existe un `env.example` con documentación de todas las variables? | **RESUELTO** | Sí, existe en `Docker/env.example`, aunque los puertos presentan discrepancias con el entorno local que se discutirán en equipo. |
| **PA-12** | ¿Los diagramas en `diagramas-casos-uso/nuevo/` están actualizados? | **RESUELTO** | **Sí**, los diagramas Mermaid `.mmd` y su código están actualizados. |
| **PA-13** | ¿`docs/informe.pdf` tiene información base adicional? | **RESUELTO** | **Sí**, `informe.pdf` contiene el marco conceptual completo y la especificación de lo que se pretende lograr en el proyecto. |

---

## 3. Preguntas sobre el Equipo y el Proceso

| # | Pregunta | Estado | Respuesta / Verificación |
|---|---|---|---|
| **PA-14** | ¿Cuál es el sprint actual y duración? | **RESUELTO** | Es el **Sprint 1**. Los sprints duran **4 semanas** (de miércoles a miércoles). Al 28-29 de septiembre de 2026 el equipo se encuentra en el penúltimo día de la Semana 3. |
| **PA-15** | ¿Decisión sobre pasarela de pago (Transbank vs Stripe)? | **RESUELTO** | Se definirá a futuro; se proyecta usar una pasarela externa o máquinas tipo Mercado Pago en modo simulado/sandbox. |
| **PA-16** | ¿Certificado digital del SII? | **RESUELTO** | Será obtenido/implementado a futuro de manera **simulada**. |
| **PA-17** | ¿Hay sucursales de prueba en seed? | **RESUELTO** | No hay sucursales configuradas en ningún seed. |
| **PA-18** | ¿Auto-update configurado con `electron-updater`? | **RESUELTO** | Verificado en `package.json`: **No** está instalado ni configurado `electron-updater`. |
