---
id: 002-kong-vs-yarp
tipo: decision
titulo: "ADR-002: Kong como API Gateway sobre YARP"
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec19, Docker/config/kong.yaml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: []
---
# ADR-002: Kong como API Gateway sobre YARP

**Fecha:** Septiembre 2026  
**Estado:** APROBADA

## Contexto

Se necesita un API Gateway para: routing entre microservicios, validación JWT centralizada, rate-limiting y CORS.

## Opciones evaluadas

- **Kong 3.6** (OSS)
- **YARP** (Microsoft, .NET reverse proxy)
- **Nginx** (con configuración manual)

## Decisión

**Kong 3.6 en modo DB-less** (configuración declarativa via `kong.yaml`).

## Justificación

| Criterio | Kong | YARP |
|----------|------|------|
| Plugin JWT out-of-the-box | ✅ | ❌ Manual |
| Plugin rate-limiting | ✅ | ❌ Manual |
| Plugin CORS | ✅ | ⚠️ Via ASP.NET |
| GUI de administración | ✅ Kong Manager | ❌ |
| Configuración declarativa | ✅ DB-less YAML | ⚠️ appsettings.json |
| Tecnología del equipo | ⚠️ Nueva | ✅ Familiar (.NET) |

Kong en modo DB-less elimina la necesidad de una base de datos adicional para Kong.

## Consecuencias

- Plugins: JWT validation, rate-limiting (100 req/min por IP), CORS centralizados
- No hay `kong.db` — toda la configuración está en `Docker/config/kong.yaml`
- Actualizar rutas = editar `kong.yaml` y reiniciar Kong

## Estado de implementación

Kong está configurado y funcionando. Solo tiene rutas para MS-1, MS-3 y MS-5. MS-4, MS-6, MS-7, MS-8 sin rutas aún.

## Conexiones
- Configuración: [[kong]]
- Docker: [[docker-compose]]

## Fuentes
- `GlobalMart_ContextMaster.md` §19
- `Docker/config/kong.yaml`
