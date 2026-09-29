# GlobalMart OS — Briefing para IA

**Sistema:** POS + ERP multi-tenant para minimarkets  
**Contexto:** Proyecto académico de Ingeniería Civil Informática (Universidad Católica de Temuco). Integraciones externas no disponibles legalmente son simuladas.  
**Stack:** 8 microservicios ASP.NET Core 8 · PostgreSQL por servicio · Kafka · Kong · Electron + React + TypeScript  

---

## Cómo usar el Grafo de Conocimiento

Antes de realizar cualquier tarea, consulta el **Grafo de Contexto**:
1. Leer [`docs/context/estado-actual.md`](docs/context/estado-actual.md) para conocer el estado real (implementado vs planificado).
2. Leer el nodo de servicio en [`docs/context/servicios/`](docs/context/servicios/) correspondiente a la tarea.
3. Consultar [`docs/context/index.md`](docs/context/index.md) para ubicar nodos de dominio, eventos Kafka o infraestructura.
4. **Regla de mantenimiento:** Si modificas código o arquitectura, debes actualizar el nodo `.md` correspondiente y su fecha `ultima_revision`.

---

## Reglas de Dominio Obligatorias

| # | Regla | Descripción |
|---|---|---|
| 1 | **Aislamiento Multi-Tenant (RN-01)** | Toda consulta o mutación debe filtrar estrictamente por `tenant_id`. |
| 2 | **Pagos por Pasarela (RN-02)** | Pagos con tarjeta NUNCA se procesan localmente; siempre pasan por pasarela/simulador. |
| 3 | **Turno de Caja Abierto (RN-06)** | Solo se pueden crear ventas si el cajero en sesión tiene un turno ABIERTO. |
| 4 | **Autenticación JWT (RN-07)** | Todo endpoint protegido debe exigir JWT válido con claims de usuario y tenant. |
| 5 | **Sin datos inventados** | Si un requerimiento o dato no está confirmado en el código, documentarlo como `[NO VERIFICADO]`. |

---

## Comandos Principales (Build / Test / Run)

```bash
# Levantar infraestructura (PostgreSQL x8, Kafka, Kong, Grafana)
docker compose -f Docker/docker-compose.yml up -d

# Compilar la solución completa (.NET 8)
dotnet build GlobalMartOS.sln

# Ejecutar tests de la solución
dotnet test GlobalMartOS.sln

# Ejecutar frontend de escritorio (Electron)
cd globalmart-frontend && npm install && npm start
```
*Para detalles de configuración y seed local de desarrollo, ver [`docs/context/proyecto/como-ejecutar.md`](docs/context/proyecto/como-ejecutar.md).*

---

## Sprint Actual

Sprint 1 (Semana 3 de 4) — Ver [`docs/context/planificacion/sprint-actual.md`](docs/context/planificacion/sprint-actual.md)
