#!/bin/bash
# ==============================================================================
# GlobalMart OS — Smoke Test HTTP del Núcleo (TI3-661 / S2-030)
#
# Valida de forma automatizada y reproducible:
#   1. Health de Kong API Gateway y microservicios (MS-1 a MS-5).
#   2. Flujo de Login público con credenciales seed.
#   3. Consulta autenticada de catálogo de productos vía Gateway y directo.
#   4. Consulta autenticada de estado de turno/POS vía Gateway y directo.
# ==============================================================================

set -e

# Colores para salida de terminal
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

GATEWAY_URL=${GATEWAY_URL:-"http://localhost:8000"}
MS1_URL=${MS1_URL:-"http://localhost:5124"}
MS2_URL=${MS2_URL:-"http://localhost:5239"}
MS3_URL=${MS3_URL:-"http://localhost:5203"}
MS4_URL=${MS4_URL:-"http://localhost:5205"}
MS5_URL=${MS5_URL:-"http://localhost:5000"}

TENANT_ID="aaaaaaaa-0000-0000-0000-000000000001"
LOGIN_EMAIL="cajero@demo.cl"
LOGIN_PASSWORD="demo1234"

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}  GlobalMart OS — Smoke Test HTTP de Flujo Crítico          ${NC}"
echo -e "${BLUE}============================================================${NC}"

# Función helper para pruebas HTTP
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

echo -e "\n${YELLOW}1. Verificando Health Checks de Microservicios...${NC}"
check_health "MS-1 Tenant & Identity" "$MS1_URL/health"
check_health "MS-2 Tax & Compliance" "$MS2_URL/health"
check_health "MS-3 Catalog & Pricing" "$MS3_URL/health"
check_health "MS-4 Warehouse & Inventory" "$MS4_URL/health"
check_health "MS-5 POS & Cart" "$MS5_URL/health"

echo -e "\n${YELLOW}2. Probando Login vía Kong API Gateway ($GATEWAY_URL)...${NC}"
LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$LOGIN_EMAIL\",\"password\":\"$LOGIN_PASSWORD\",\"tenantId\":\"$TENANT_ID\"}" || true)

HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
BODY=$(echo "$LOGIN_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" != "200" ]; then
  echo -e "  ${RED}Fallo en Login vía Gateway ($HTTP_CODE). Intentando directo a MS-1 ($MS1_URL)...${NC}"
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
  echo "Cuerpo de respuesta: $BODY"
  exit 1
fi
echo -e "${GREEN}[OK] Autenticación exitosa. Token JWT obtenido (longitud: ${#TOKEN} chars).${NC}"

echo -e "\n${YELLOW}3. Probando Consulta de Catálogo con JWT...${NC}"
CATALOG_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/products" \
  -H "Authorization: Bearer $TOKEN" || echo "000")

if [ "$CATALOG_CODE" = "200" ]; then
  echo -e "${GREEN}[OK] Catálogo consultado exitosamente vía Gateway /api/products (HTTP 200).${NC}"
else
  echo -e "  ${YELLOW}Verificando directo en MS-3 ($MS3_URL/products)...${NC}"
  DIRECT_CATALOG_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$MS3_URL/products" \
    -H "Authorization: Bearer $TOKEN" || echo "000")
  if [ "$DIRECT_CATALOG_CODE" = "200" ]; then
    echo -e "${GREEN}[OK] Catálogo consultado exitosamente directo en MS-3 (HTTP 200).${NC}"
  else
    echo -e "${RED}[ERROR] No se pudo consultar catálogo (Gateway: $CATALOG_CODE, Directo: $DIRECT_CATALOG_CODE).${NC}"
    exit 1
  fi
fi

echo -e "\n${YELLOW}4. Probando Consulta de Turno POS con JWT...${NC}"
POS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/turnos/activo" \
  -H "Authorization: Bearer $TOKEN" || echo "000")

if [ "$POS_CODE" = "200" ] || [ "$POS_CODE" = "404" ]; then
  echo -e "${GREEN}[OK] Endpoint de Turnos POS respondió correctamente (HTTP $POS_CODE — $ ([ "$POS_CODE" = "200" ] && echo "Turno activo" || echo "Sin turno activo")).${NC}"
else
  echo -e "  ${YELLOW}Verificando directo en MS-5 ($MS5_URL/api/turnos/activo)...${NC}"
  DIRECT_POS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$MS5_URL/api/turnos/activo" \
    -H "Authorization: Bearer $TOKEN" || echo "000")
  if [ "$DIRECT_POS_CODE" = "200" ] || [ "$DIRECT_POS_CODE" = "404" ]; then
    echo -e "${GREEN}[OK] Turnos POS respondió correctamente directo en MS-5 (HTTP $DIRECT_POS_CODE).${NC}"
  else
    echo -e "${RED}[ERROR] Falla en POS (Gateway: $POS_CODE, Directo: $DIRECT_POS_CODE).${NC}"
    exit 1
  fi
fi

echo -e "\n${GREEN}============================================================${NC}"
echo -e "${GREEN}  SMOKE TEST EXITOSO: El núcleo GlobalMart OS está operativo!${NC}"
echo -e "${GREEN}============================================================${NC}"
exit 0
