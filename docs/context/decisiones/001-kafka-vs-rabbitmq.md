---
id: 001-kafka-vs-rabbitmq
tipo: decision
titulo: "ADR-001: Kafka como bus de eventos sobre RabbitMQ"
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec19, Docker/docker-compose.yml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: []
---
# ADR-001: Kafka como bus de eventos sobre RabbitMQ

**Fecha:** Septiembre 2026  
**Estado:** APROBADA

## Contexto

Se necesita un bus de mensajes para comunicación asíncrona entre microservicios (stock descuento, analytics, loyalty, notificaciones).

## Opciones evaluadas

- **Kafka** (Apache Kafka 3.x / Confluent)
- **RabbitMQ** (AMQP)
- **Azure Service Bus** (cloud)

## Decisión

**Kafka**, usando imagen `confluentinc/cp-kafka:7.6.1`.

## Justificación

| Criterio | Kafka | RabbitMQ |
|----------|-------|----------|
| Retención de mensajes | ✅ Permanente (log) | ❌ Hasta consumo |
| Throughput | ✅ Millones msg/s | ❌ Decenas de miles |
| Replay de eventos | ✅ Desde offset | ❌ No |
| Consumer groups | ✅ Nativo | ⚠️ Manual con topics |
| Complejidad operacional | ❌ Alta (Zookeeper) | ✅ Baja |

La retención de mensajes es crítica para que MS-7 (Analytics) pueda reprocesar histórico y MS-4 (Warehouse) pueda garantizar idempotencia.

## Consecuencias

- Se requiere Zookeeper en el stack (overhead operacional)
- Los mensajes tienen garantía exactly-once con tabla de idempotencia (implementado en MS-4)
- Se agrega Kafdrop como herramienta de UI para monitorear topics

## Conexiones
- Implementación: [[kafka-topics]], [[ms4-inventory]]
- Docker: [[docker-compose]]

## Fuentes
- `GlobalMart_ContextMaster.md` §19
