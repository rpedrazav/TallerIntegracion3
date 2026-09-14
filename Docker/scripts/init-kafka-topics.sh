#!/usr/bin/env bash
# ==============================================================================
# Script de Inicialización y Verificación de Topics en Apache Kafka
# Proyecto: GlobalMart OS (INTEGRA3)
# ==============================================================================
set -euo pipefail

KAFKA_BROKER="${KAFKA_BROKER:-kafka:29092}"
KAFKA_TOPIC_PARTITIONS="${KAFKA_TOPIC_PARTITIONS:-1}"
KAFKA_TOPIC_REPLICATION_FACTOR="${KAFKA_TOPIC_REPLICATION_FACTOR:-1}"

# Lista de topics por defecto si no está definida en el entorno
DEFAULT_TOPICS="sale.completed,stock.alert,expiry.alert,sale.reversed,stock.updated,purchase.received,fx.rate.updated,points.updated,tenant.created,tenant.updated,user.registered,product.created,product.price_updated,shift.closed,stock.low,product.expiring_soon,purchase_order.received"
KAFKA_INIT_TOPICS="${KAFKA_INIT_TOPICS:-$DEFAULT_TOPICS}"

echo "============================================================"
echo " [Kafka Init] Conectando a broker: $KAFKA_BROKER"
echo " [Kafka Init] Particiones por topic: $KAFKA_TOPIC_PARTITIONS"
echo " [Kafka Init] Factor de replicación: $KAFKA_TOPIC_REPLICATION_FACTOR"
echo "============================================================"

# Esperar a que el broker esté disponible
if command -v cub >/dev/null 2>&1; then
  echo " [Kafka Init] Verificando readiness del broker con cub..."
  cub kafka-ready 1 30 -b "$KAFKA_BROKER"
else
  echo " [Kafka Init] Esperando respuesta del broker con kafka-topics..."
  max_retries=30
  retry=0
  until kafka-topics --bootstrap-server "$KAFKA_BROKER" --list >/dev/null 2>&1 || [ $retry -ge $max_retries ]; do
    retry=$((retry + 1))
    echo " [Kafka Init] Intento $retry/$max_retries: Esperando a Kafka en $KAFKA_BROKER..."
    sleep 2
  done

  if [ $retry -ge $max_retries ]; then
    echo "❌ Error: Broker de Kafka no respondió en $KAFKA_BROKER tras $max_retries intentos."
    exit 1
  fi
fi

echo " [Kafka Init] Broker operativo. Aprovisionando topics..."

IFS=',' read -ra TOPICS <<< "$KAFKA_INIT_TOPICS"
created_count=0

for topic in "${TOPICS[@]}"; do
  clean_topic="$(echo "$topic" | tr -d '[:space:]')"
  if [ -n "$clean_topic" ]; then
    kafka-topics --bootstrap-server "$KAFKA_BROKER" \
      --create --if-not-exists \
      --topic "$clean_topic" \
      --partitions "$KAFKA_TOPIC_PARTITIONS" \
      --replication-factor "$KAFKA_TOPIC_REPLICATION_FACTOR" >/dev/null 2>&1
    echo "  ✔ Topic verificado/creado: $clean_topic"
    created_count=$((created_count + 1))
  fi
done

echo "============================================================"
echo " [Kafka Init] Total topics procesados: $created_count"
echo " [Kafka Init] Listado oficial de topics en el cluster:"
echo "------------------------------------------------------------"
kafka-topics --bootstrap-server "$KAFKA_BROKER" --list
echo "============================================================"
echo " [Kafka Init] Inicialización finalizada con éxito."
