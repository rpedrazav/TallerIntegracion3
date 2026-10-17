#!/bin/bash
echo "============================================================"
echo "Iniciando el Núcleo de Microservicios GlobalMart OS (MS-1 a MS-5)..."
echo "============================================================"

dotnet run --project src/TenantIdentityService &
dotnet run --project src/TaxComplianceService &
dotnet run --project src/CatalogPricingService &
dotnet run --project src/WarehouseInventoryService &
dotnet run --project src/POSCartService &

echo "------------------------------------------------------------"
echo "Microservicios iniciados en segundo plano:"
echo "  - MS-1 Tenant & Identity:    http://localhost:5124"
echo "  - MS-2 Tax & Compliance:     http://localhost:5239"
echo "  - MS-3 Catalog & Pricing:    http://localhost:5203"
echo "  - MS-4 Warehouse & Inventory:http://localhost:5205"
echo "  - MS-5 POS & Cart:           http://localhost:5000"
echo "------------------------------------------------------------"
echo "Para detenerlos puedes usar: pkill -f dotnet"
echo "============================================================"
