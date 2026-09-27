@echo off
echo Iniciando las 4 APIs de GlobalMart...

start "Tenant Identity Service" cmd /k "cd src\TenantIdentityService && dotnet run"
start "Tax Compliance Service" cmd /k "cd src\TaxComplianceService && dotnet run"
start "Catalog Pricing Service" cmd /k "cd src\CatalogPricingService && dotnet run"
start "POS Cart Service" cmd /k "cd src\POSCartService && dotnet run"

echo APIs iniciadas en ventanas separadas.
