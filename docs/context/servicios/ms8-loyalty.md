---
id: ms8-loyalty
tipo: microservicio
titulo: MS-8 · Loyalty & Customer Service
estado: planificado
fuentes: [src/LoyaltyCustomerService/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [ms1-identity]
publica: [points.updated]
consume: [sale.completed]
reglas: [RN-15, RN-17, RF-23, LC-01]
---
# MS-8 · Loyalty & Customer Service

> Gestiona el programa de lealtad: clientes afiliados, puntos, tiers y cupones. Tiene los modelos de datos implementados y Kafka registrado en DI, pero sin controllers ni handlers de eventos. Sprint 7 según roadmap.

## Estado actual

**PLANIFICADO** — Sprint 7.

Lo que existe:
- Modelos (ClienteAfiliado, MovimientoPuntos, SaldoPuntos, TierMembresia)
- DbContext + migración inicial
- Kafka Consumer y Producer registrados como singletons en Program.cs (sin handlers)

Lo que NO existe: controllers, endpoints, lógica de puntos/tiers/cupones, handlers Kafka.

## Modelo de datos (implementado)

```
ClienteAfiliado
  id          GUID PK
  tenant_id   GUID
  nombre      string
  email       string?
  dni         string?
  codigo_qr   string
  activo      bool

SaldoPuntos
  id              GUID PK
  tenant_id       GUID
  cliente_id      GUID FK
  puntos_actuales decimal
  puntos_historicos decimal
  ultimo_movimiento DateTime

MovimientoPuntos
  id          GUID PK
  tenant_id   GUID
  cliente_id  GUID FK
  tipo        string ("acumulacion"|"canje"|"expiracion")
  puntos      decimal
  referencia  string? (VentaId o CuponId)
  creado_en   DateTime

TierMembresia
  id           GUID PK
  tenant_id    GUID
  nombre       string (ej: "Bronce", "Plata", "Oro")
  puntos_minimos decimal
  descuento_pct decimal
```

## Endpoints diseñados (PLANIFICADOS)

- `POST /clientes` — registrar cliente afiliado
- `GET /clientes/{id}` — perfil y saldo
- `GET /clientes/{id}/historial` — historial de compras
- `POST /clientes/{id}/identificar` — identificar en POS
- `POST /clientes/{id}/canjear` — canjear puntos
- `GET/POST /programas` — configurar programa de lealtad
- `POST /cupones/validar` — validar cupón en POS

## Lógica de puntos diseñada (PLANIFICADA)

```
puntos_acumulados = monto_venta × tasa_base × multiplicador_categoria
```
- Multiplicadores por categoría: configurables por tenant (RN-17)
- Expiración: 12 meses de inactividad (RN-15)
- Tiers: suben automáticamente al superar umbral de puntos históricos

## Casos de uso cubiertos (todos PLANIFICADOS)

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| LC-01 | Registrar Cliente Afiliado | [PLANIFICADO] |
| LC-02 | Verificar Identidad del Cliente | [PLANIFICADO] |
| LC-03 | Identificar Cliente en POS | [PLANIFICADO] |
| LC-04 | Consultar Perfil y Saldo de Puntos | [PLANIFICADO] |
| LC-05 | Ver Historial de Compras | [PLANIFICADO] |
| LC-06 | Acumular Puntos por Compra | [PLANIFICADO] |
| LC-07 | Canjear Puntos por Descuento | [PLANIFICADO] |
| LC-08 | Canjear Puntos por Producto Gratis | [PLANIFICADO] |
| LC-09 | Aplicar Tier / Nivel Membresía | [PLANIFICADO] |
| LC-10 | Emitir Cupón de Descuento | [PLANIFICADO] |
| LC-11 | Validar Cupón en POS | [PLANIFICADO] |
| LC-12 | Configurar Programa de Lealtad | [PLANIFICADO] |
| LC-13 | Definir Multiplicadores de Puntos | [PLANIFICADO] |
| LC-14 | Definir Beneficios por Nivel | [PLANIFICADO] |
| LC-15 | Expirar Puntos Inactivos | [PLANIFICADO] |
| LC-16 | Enviar Estado de Cuenta al Cliente | [PLANIFICADO] |
| LC-17 | Publicar points.updated | [PLANIFICADO] |

## Conexiones
- Consumiría: `sale.completed` ← [[ms5-pos]] (para acumular puntos)
- Publicaría: `points.updated` → [[ms7-analytics]]
- Reglas: [[reglas-negocio]] (RN-15, RN-17)

## Fuentes
- `src/LoyaltyCustomerService/Models/ClienteAfiliado.cs`
- `src/LoyaltyCustomerService/Program.cs`
