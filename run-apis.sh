#!/usr/bin/env bash

# Asegurar que dotnet esté en el PATH
export PATH="$PATH:$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"

echo "=========================================="
echo "  Iniciando las 4 APIs de GlobalMart OS  "
echo "=========================================="

# Trap para matar los procesos hijos al presionar Ctrl+C
trap 'echo -e "\nDeteniendo APIs..."; kill $(jobs -p) 2>/dev/null; exit' SIGINT SIGTERM EXIT

(cd src/TenantIdentityService && dotnet run) &
(cd src/TaxComplianceService && dotnet run) &
(cd src/CatalogPricingService && dotnet run) &
(cd src/POSCartService && dotnet run) &

echo "Las 4 APIs se están ejecutando."
echo "Presiona Ctrl+C en esta terminal para detenerlas."

wait
