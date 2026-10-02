#!/bin/bash
echo "Iniciando las APIs de GlobalMart en segundo plano..."

dotnet run --project src/TenantIdentityService &
dotnet run --project src/TaxComplianceService &
dotnet run --project src/CatalogPricingService &
dotnet run --project src/WarehouseInventoryService &
dotnet run --project src/POSCartService &

echo "Las 5 APIs principales han sido iniciadas en segundo plano."
echo "Para detenerlas puedes usar: pkill -f dotnet"
