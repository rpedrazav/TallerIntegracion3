---
id: vision
tipo: proyecto
titulo: Visión del Proyecto — GlobalMart OS
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec1-2, docs/informe.pdf]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: []
---
# Visión del Proyecto — GlobalMart OS

> GlobalMart OS es un sistema POS + ERP multi-tenant para minimarkets, con arquitectura de microservicios, Kafka y frontend Electron.  
> **Contexto Académico:** Proyecto formativo de la carrera de Ingeniería Civil Informática en la Universidad Católica de Temuco (UCT) para la asignatura Taller de Integración 3 (INTEGRA3).  
> **Política de Simulación:** Aquellos servicios e integraciones que no puedan obtenerse legal u oficialmente (certificados digitales reales del SII, terminales POS físicos de bancos) serán simulados mediante software/sandboxes.

## Qué es

**GlobalMart OS** combina:
- **POS (Point of Sale):** Caja registradora digital con carrito, cobro (efectivo/tarjeta/mixto), comprobantes y cuadre de caja.
- **ERP (Enterprise Resource Planning):** Inventario, catálogo, órdenes de compra, programa de lealtad y analytics.

**Contexto:** Diseñado para minimarkets de Latinoamérica y mercados emergentes que necesitan automatización fiscal, control de inventario FEFO y escalabilidad multi-sucursal.

## Propuesta de valor

| Problema | Solución GlobalMart OS |
|----------|------------------------|
| Cajas sin integración fiscal | POS con DTE electrónico |
| Control de inventario en papel | FEFO automatizado vía Kafka |
| Sin programa de lealtad | MS-8 puntos, tiers, cupones |
| Imposible escalar a nuevas sucursales | Multi-tenant: agregar tenant = nueva sucursal |
| Sin visibilidad en tiempo real | Analytics y dashboards via Kafka |

## Características fundamentales

- **Multi-Tenant:** cada minimarket es un tenant aislado (datos, usuarios, fiscal, moneda)
- **Event-Driven:** comunicación asíncrona entre microservicios via Kafka
- **Desktop-First:** Electron + React para el mostrador del minimarket
- **Global-Ready:** soporte para Chile (SII), Argentina (AFIP), USA (IRS)

## Alcance v1.0

8 áreas operativas cubiertas por 8 microservicios:
1. Autenticación y RBAC multi-rol (MS-1)
2. Punto de Venta con cobros (MS-5)
3. Catálogo de productos (MS-3)
4. Inventario FEFO (MS-4)
5. Cumplimiento fiscal (MS-2)
6. Cadena de suministro con Costo Landed (MS-6)
7. Programa de lealtad (MS-8)
8. Analytics y notificaciones (MS-7)

## Fuera de alcance v1.0

- App móvil para clientes (RF-29)
- Integración con e-commerce / marketplaces (RF-30)
- Gestión de nómina (RF-31)
- Inventario en consignación (RF-32)
- Multi-moneda dentro de una misma transacción (RN-20)

## Conexiones
- Arquitectura: [[arquitectura]]
- Actores: [[actores]]
- Reglas de negocio: [[reglas-negocio]]
- Estado actual: [[estado-actual]]

## Fuentes
- `GlobalMart_ContextMaster.md` §1, §2
