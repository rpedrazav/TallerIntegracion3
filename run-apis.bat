@echo off
echo ============================================================
echo Iniciando el Nucleo de Microservicios GlobalMart OS (MS-1 a MS-5)...
echo ============================================================

set ASPNETCORE_ENVIRONMENT=Development
if "%PasarelaPago__BaseUrl%"=="" set PasarelaPago__BaseUrl=http://localhost:5050

start "MS-1 Tenant Identity Service" cmd /k "cd src\TenantIdentityService && dotnet run --no-launch-profile --urls http://0.0.0.0:5124"
start "MS-2 Tax Compliance Service" cmd /k "cd src\TaxComplianceService && dotnet run --no-launch-profile --urls http://0.0.0.0:5239"
start "MS-3 Catalog Pricing Service" cmd /k "cd src\CatalogPricingService && dotnet run --no-launch-profile --urls http://0.0.0.0:5203"
start "MS-4 Warehouse Inventory Service" cmd /k "cd src\WarehouseInventoryService && dotnet run --no-launch-profile --urls http://0.0.0.0:5205"
start "MS-5 POS Cart Service" cmd /k "cd src\POSCartService && dotnet run --no-launch-profile --urls http://0.0.0.0:5000"
curl -s http://localhost:5050/health >nul 2>&1 || start "Payment Simulator" cmd /k "node Docker\simulator\server.js"

echo ------------------------------------------------------------
echo APIs iniciadas en consolas independientes:
echo   - MS-1 Tenant ^& Identity:    http://localhost:5124
echo   - MS-2 Tax ^& Compliance:     http://localhost:5239
echo   - MS-3 Catalog ^& Pricing:    http://localhost:5203
echo   - MS-4 Warehouse ^& Inventory:http://localhost:5205
echo   - MS-5 POS ^& Cart:           http://localhost:5000
echo   - Simulador de Pagos:        http://localhost:5050
echo   - Kong API Gateway:          http://localhost:8000
echo ============================================================
