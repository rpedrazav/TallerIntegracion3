---
id: actores
tipo: dominio
titulo: Actores del Sistema — GlobalMart OS
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec5, diagramas-casos-uso/nuevo/D0_Actores_Generalizacion.mmd]
verificado_contra_codigo: false
ultima_revision: 2026-09-28
depende_de: [vision]
publica: []
consume: []
reglas: []
---
# Actores del Sistema — GlobalMart OS

> 13 actores: 5 humanos + 8 sistema/hardware. Un mismo humano puede encarnar múltiples roles (Actor Multi-Rol / Encargado de Tienda).

## Actores Humanos (5)

| Actor | Tipo | Descripción | Capacidades Clave |
|-------|------|-------------|-------------------|
| **Cajero** | Primario | Opera el POS en mostrador | Abrir/cerrar turno, carrito, cobrar, comprobantes |
| **Reponedor** | Primario | Gestiona inventario físico | Recibir mercancía, FEFO, mermas, conteo físico |
| **Administrador** | Primario | Gestiona el tenant completo | Config tenant, usuarios, precios, OC, reportes |
| **Super_Admin** | Secundario | Dueño del sistema global | Gestión técnica: tenants, tokens globales, auditoría |
| **Cliente Afiliado** | Secundario | Cliente con membresía activa | Identificarse en POS, acumular/canjear puntos |

## Actores Sistema / Hardware (8)

| Actor | Tipo | Protocolo |
|-------|------|-----------|
| **Balanza Física** | Hardware | USB / Serial (COM port) |
| **Terminal POS** | Hardware | USB / Bluetooth / LAN |
| **Entidad Fiscal** | Sistema externo | REST / SOAP según país |
| **Pasarela de Pago** | Sistema externo | REST HTTPS |
| **Sistema de Transporte** | Sistema externo | REST API |
| **Proveedor Mensajería** | Sistema externo | REST API |
| **Servicio Tipos de Cambio** | Sistema externo | REST API |
| **Bus de Eventos (Kafka)** | Sistema interno | Apache Kafka Protocol |

## Jerarquía de actores (generalización UML)

```
Usuario GlobalMart (superactor)
   ├── Cajero
   ├── Reponedor
   ├── Administrador
   │      └── Super_Admin (hereda de Administrador)
   └── Cliente Afiliado

Encargado de Tienda (actor compuesto — minimarkets pequeños)
   ├── hereda de Cajero
   ├── hereda de Reponedor
   └── hereda de Administrador
```

El **Encargado de Tienda** es el caso real más común en minimarkets pequeños: una sola persona que hace todas las funciones. El sistema soporta esto via RBAC con múltiples roles: `roles: ["CAJERO", "REPONEDOR", "ADMIN"]`.

## Diagrama de referencia

Ver `diagramas-casos-uso/nuevo/D0_Actores_Generalizacion.mmd` y `diagramas-casos-uso/D0_Actores_Generalizacion.png`.

## Conexiones
- Roles en sistema: [[rbac-multirol]]
- Casos de uso por actor: [[ms1-identity]], [[ms5-pos]], [[ms4-inventory]]
- Hardware: [[hardware]]

## Fuentes
- `GlobalMart_ContextMaster.md` §5
- `diagramas-casos-uso/nuevo/D0_Actores_Generalizacion.mmd`
