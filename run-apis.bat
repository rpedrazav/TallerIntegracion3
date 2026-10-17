@echo off
echo ============================================================
echo Iniciando el Nucleo de Microservicios GlobalMart OS (MS-1 a MS-5)...
echo ============================================================

start "MS-1 Tenant Identity Service" cmd /k "cd src\TenantIdentityService && dotnet run"
start "MS-2 Tax Compliance Service" cmd /k "cd src\TaxComplianceService && dotnet run"
start "MS-3 Catalog Pricing Service" cmd /k "cd src\CatalogPricingService && dotnet run"
start "MS-4 Warehouse Inventory Service" cmd /k "cd src\WarehouseInventoryService && dotnet run"
start "MS-5 POS Cart Service" cmd /k "cd src\POSCartService && dotnet run"
start "Payment Simulator" cmd /k "node Docker\simulator\server.js"

echo ------------------------------------------------------------
echo APIs iniciadas en consolas independientes:
echo   - MS-1 Tenant & Identity:    http://localhost:5124
echo   - MS-2 Tax & Compliance:     http://localhost:5239
echo   - MS-3 Catalog & Pricing:    http://localhost:5203
echo   - MS-4 Warehouse & Inventory:http://localhost:5205
echo   - MS-5 POS & Cart:           http://localhost:5000
echo   - Simulador de Pagos:        http://localhost:5050
echo ============================================================
