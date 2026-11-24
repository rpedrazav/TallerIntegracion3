#!/bin/bash
echo "============================================================"
echo "Iniciando el Núcleo de Microservicios GlobalMart OS (MS-1 a MS-5)..."
echo "============================================================"

export ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
export PasarelaPago__BaseUrl=${PasarelaPago__BaseUrl:-http://localhost:5050}

# Se enlaza en 0.0.0.0 para responder tanto a localhost como a Kong (host.docker.internal)
dotnet run --no-launch-profile --project src/TenantIdentityService --urls "http://0.0.0.0:5124" &
dotnet run --no-launch-profile --project src/TaxComplianceService --urls "http://0.0.0.0:5239" &
dotnet run --no-launch-profile --project src/CatalogPricingService --urls "http://0.0.0.0:5203" &
dotnet run --no-launch-profile --project src/WarehouseInventoryService --urls "http://0.0.0.0:5205" &
dotnet run --no-launch-profile --project src/POSCartService --urls "http://0.0.0.0:5000" &

# Iniciar Simulador de Pagos en host solo si no está corriendo ya en Docker (:5050)
if ! nc -z localhost 5050 2>/dev/null && ! curl -s http://localhost:5050/health >/dev/null 2>&1; then
  node Docker/simulator/server.js &
fi

echo "------------------------------------------------------------"
echo "Microservicios iniciados en segundo plano (0.0.0.0 / localhost + Kong :8000):"
echo "  - MS-1 Tenant & Identity:    http://localhost:5124"
echo "  - MS-2 Tax & Compliance:     http://localhost:5239"
echo "  - MS-3 Catalog & Pricing:    http://localhost:5203"
echo "  - MS-4 Warehouse & Inventory:http://localhost:5205"
echo "  - MS-5 POS & Cart:           http://localhost:5000"
echo "  - Simulador de Pagos:        http://localhost:5050"
echo "  - Kong API Gateway:          http://localhost:8000"
echo "------------------------------------------------------------"
echo "Para detenerlos puedes usar: pkill -f 'dotnet run --no-launch-profile' && pkill -f 'node Docker/simulator/server.js'"
echo "============================================================"
