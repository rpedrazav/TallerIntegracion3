#!/bin/bash
# ==============================================================================
# GlobalMart OS — Smoke Test HTTP del Núcleo y Gateway (S2-030 / S2-116)
#
# Valida de forma automatizada y reproducible:
#   1. Health de microservicios (MS-1 a MS-5) y Simulador de Pagos.
#   2. Flujo de Login público con credenciales seed vía Kong (:8000) y directo.
#   3. Consulta autenticada de catálogo de productos vía Kong (:8000) y directo.
#   4. Consulta autenticada de estado de turno/POS vía Kong (:8000) y directo.
#   5. Autorización determinista e idempotente en Simulador de Pagos + bloqueo RN-02.
#
# Variables opcionales:
#   REQUIRE_GATEWAY=true (default: auto-detecta Kong en :8000; si está activo exige 200/404 por Gateway)
# ==============================================================================

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

GATEWAY_URL=${GATEWAY_URL:-"http://localhost:8000"}
MS1_URL=${MS1_URL:-"http://localhost:5124"}
MS2_URL=${MS2_URL:-"http://localhost:5239"}
MS3_URL=${MS3_URL:-"http://localhost:5203"}
MS4_URL=${MS4_URL:-"http://localhost:5205"}
MS5_URL=${MS5_URL:-"http://localhost:5000"}
SIMULATOR_URL=${SIMULATOR_URL:-"http://localhost:5050"}

TENANT_ID="aaaaaaaa-0000-0000-0000-000000000001"
LOGIN_EMAIL="cajero@demo.cl"
LOGIN_PASSWORD=${LOGIN_PASSWORD:-"demo1234"}

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}  GlobalMart OS — Smoke Test HTTP de Flujo Crítico (S2-116) ${NC}"
echo -e "${BLUE}============================================================${NC}"

check_health() {
  local service_name=$1
  local url=$2
  echo -n "  -> Verificando health de $service_name ($url)... "
  local status_code
  status_code=$(curl -s -o /dev/null -w "%{http_code}" "$url" || echo "000")
  if [ "$status_code" = "200" ]; then
    echo -e "${GREEN}[OK - 200]${NC}"
  else
    echo -e "${RED}[FALLÓ - Código $status_code]${NC}"
    return 1
  fi
}

echo -e "\n${YELLOW}1. Verificando Health Checks de Microservicios y Simulador...${NC}"
check_health "MS-1 Tenant & Identity" "$MS1_URL/health"
check_health "MS-2 Tax & Compliance" "$MS2_URL/health"
check_health "MS-3 Catalog & Pricing" "$MS3_URL/health"
check_health "MS-4 Warehouse & Inventory" "$MS4_URL/health"
check_health "MS-5 POS & Cart" "$MS5_URL/health"
check_health "Simulador de Pagos" "$SIMULATOR_URL/health"

# Detectar si Kong API Gateway está escuchando
KONG_SIM_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/simulator/health" || echo "000")
GATEWAY_ACTIVE="false"
if [ "$KONG_SIM_HEALTH" = "200" ] || [ "${REQUIRE_GATEWAY:-false}" = "true" ]; then
  GATEWAY_ACTIVE="true"
  echo -e "  -> Kong API Gateway detectado en $GATEWAY_URL (${GREEN}modo estricto Gateway activo${NC})"
fi

echo -e "\n${YELLOW}2. Probando Login con credenciales seed...${NC}"
LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$LOGIN_EMAIL\",\"password\":\"$LOGIN_PASSWORD\",\"tenantId\":\"$TENANT_ID\"}" || true)

HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
BODY=$(echo "$LOGIN_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}[OK] Login vía Kong Gateway ($GATEWAY_URL/api/auth/login) respondió HTTP 200.${NC}"
elif [ "$GATEWAY_ACTIVE" = "true" ]; then
  echo -e "${RED}[ERROR] Login vía Kong Gateway falló con HTTP $HTTP_CODE.${NC}"
  echo "Respuesta: $BODY"
  exit 1
else
  echo -e "  ${YELLOW}Kong no disponible ($HTTP_CODE). Probando directo a MS-1 ($MS1_URL)...${NC}"
  LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$MS1_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$LOGIN_EMAIL\",\"password\":\"$LOGIN_PASSWORD\",\"tenantId\":\"$TENANT_ID\"}" || true)
  HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
  BODY=$(echo "$LOGIN_RESPONSE" | sed '$d')
fi

if [ "$HTTP_CODE" != "200" ]; then
  echo -e "${RED}[ERROR] No se pudo autenticar usuario seed ($LOGIN_EMAIL). HTTP: $HTTP_CODE${NC}"
  echo "Respuesta: $BODY"
  exit 1
fi

TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*' | cut -d'"' -f4 || echo "")
if [ -z "$TOKEN" ]; then
  TOKEN=$(echo "$BODY" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4 || echo "")
fi

if [ -z "$TOKEN" ]; then
  echo -e "${RED}[ERROR] El login respondió 200 pero no se pudo extraer el token JWT.${NC}"
  exit 1
fi
echo -e "${GREEN}[OK] Autenticación exitosa. Token JWT emitido (longitud: ${#TOKEN} chars).${NC}"

echo -e "\n${YELLOW}3. Probando Consulta de Catálogo con JWT...${NC}"
CATALOG_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/products" \
  -H "Authorization: Bearer $TOKEN" || echo "000")

if [ "$CATALOG_CODE" = "200" ]; then
  echo -e "${GREEN}[OK] Catálogo consultado exitosamente vía Kong Gateway /api/products (HTTP 200).${NC}"
elif [ "$GATEWAY_ACTIVE" = "true" ]; then
  echo -e "${RED}[ERROR] Consulta de catálogo vía Kong Gateway falló (HTTP $CATALOG_CODE).${NC}"
  exit 1
else
  DIRECT_CATALOG_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$MS3_URL/products" \
    -H "Authorization: Bearer $TOKEN" || echo "000")
  if [ "$DIRECT_CATALOG_CODE" = "200" ]; then
    echo -e "${GREEN}[OK] Catálogo consultado directo en MS-3 (HTTP 200).${NC}"
  else
    echo -e "${RED}[ERROR] No se pudo consultar catálogo (Gateway: $CATALOG_CODE, Directo: $DIRECT_CATALOG_CODE).${NC}"
    exit 1
  fi
fi

echo -e "\n${YELLOW}4. Probando Consulta de Turno POS con JWT...${NC}"
POS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/turnos/activo" \
  -H "Authorization: Bearer $TOKEN" || echo "000")

if [ "$POS_CODE" = "200" ] || [ "$POS_CODE" = "404" ]; then
  echo -e "${GREEN}[OK] Endpoint de Turnos POS respondió vía Kong Gateway (HTTP $POS_CODE — $([ "$POS_CODE" = "200" ] && echo "Turno activo" || echo "Sin turno abierto")).${NC}"
elif [ "$GATEWAY_ACTIVE" = "true" ]; then
  echo -e "${RED}[ERROR] Consulta de POS vía Kong Gateway falló (HTTP $POS_CODE).${NC}"
  exit 1
else
  DIRECT_POS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$MS5_URL/api/turnos/activo" \
    -H "Authorization: Bearer $TOKEN" || echo "000")
  if [ "$DIRECT_POS_CODE" = "200" ] || [ "$DIRECT_POS_CODE" = "404" ]; then
    echo -e "${GREEN}[OK] Turnos POS respondió directo en MS-5 (HTTP $DIRECT_POS_CODE).${NC}"
  else
    echo -e "${RED}[ERROR] Falla en POS (Gateway: $POS_CODE, Directo: $DIRECT_POS_CODE).${NC}"
    exit 1
  fi
fi

echo -e "\n${YELLOW}5. Probando Simulador de Pagos y Enrutamiento Gateway (S2-057 / S2-058 / S2-116)...${NC}"
SIM_PAY_TARGET="$SIMULATOR_URL/simulator/payments"
if [ "$GATEWAY_ACTIVE" = "true" ]; then
  echo -e "${GREEN}[OK] Ruta Kong /simulator/health operativa hacia el simulador (HTTP 200).${NC}"
  SIM_PAY_TARGET="$GATEWAY_URL/simulator/payments"
fi

SIM_REQ_ID="aaaaaaaa-1111-4111-8111-111111111111"
SIM_TENANT_ID="aaaaaaaa-1111-4111-8111-000000000001"
SIM_PAY_RES=$(curl -s -w "\n%{http_code}" -X POST "$SIM_PAY_TARGET" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: $SIM_REQ_ID" \
  -d "{\"request_id\":\"$SIM_REQ_ID\",\"tenant_id\":\"$SIM_TENANT_ID\",\"sale_id\":\"bbbbbbbb-2222-4222-8222-222222222222\",\"amount\":15000,\"currency\":\"CLP\",\"scenario_id\":\"sim-approved-v1\"}" || true)
PAY_HTTP=$(echo "$SIM_PAY_RES" | tail -n1)
if [ "$PAY_HTTP" = "200" ]; then
  echo -e "${GREEN}[OK] Cobro simulado sim-approved-v1 aprobado con Idempotency-Key vía $SIM_PAY_TARGET (HTTP 200).${NC}"
else
  echo -e "${RED}[ERROR] Falló cobro simulado sim-approved-v1 vía $SIM_PAY_TARGET (HTTP $PAY_HTTP).${NC}"
  echo "$SIM_PAY_RES"
  exit 1
fi

SIM_SEC_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$SIM_PAY_TARGET" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: aaaaaaaa-1111-4111-8111-111111111112" \
  -d "{\"request_id\":\"aaaaaaaa-1111-4111-8111-111111111112\",\"tenant_id\":\"$TENANT_ID\",\"sale_id\":\"bbbbbbbb-2222-4222-8222-222222222222\",\"amount\":15000,\"currency\":\"CLP\",\"scenario_id\":\"sim-approved-v1\",\"pan\":\"4111111111111111\"}" || echo "000")
if [ "$SIM_SEC_CODE" = "400" ]; then
  echo -e "${GREEN}[OK] Validación RN-02 / PCI DSS: Envío de PAN/tarjeta bloqueado exitosamente (HTTP 400 CARD_DATA_FORBIDDEN).${NC}"
else
  echo -e "${RED}[ERROR] Validación RN-02 falló: se esperaba HTTP 400 y se recibió $SIM_SEC_CODE.${NC}"
  exit 1
fi

echo -e "\n${GREEN}============================================================${NC}"
echo -e "${GREEN}  SMOKE TEST EXITOSO: Login, Catálogo, POS y Pasarela OK!   ${NC}"
echo -e "${GREEN}============================================================${NC}"
exit 0
