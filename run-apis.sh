#!/bin/bash
echo "Iniciando las APIs de GlobalMart en segundo plano..."

mkdir -p logs
nohup dotnet run --project src/TenantIdentityService > logs/identity.log 2>&1 &
nohup dotnet run --project src/TaxComplianceService > logs/tax.log 2>&1 &
nohup dotnet run --project src/CatalogPricingService > logs/catalog.log 2>&1 &
nohup dotnet run --project src/WarehouseInventoryService > logs/warehouse.log 2>&1 &
nohup dotnet run --project src/POSCartService > logs/pos.log 2>&1 &

echo "Las 5 APIs principales han sido iniciadas en segundo plano."
echo "Logs disponibles en carpeta logs/ (identity.log, tax.log, catalog.log, warehouse.log, pos.log)"
echo "Para detenerlas puedes usar: pkill -f dotnet"
