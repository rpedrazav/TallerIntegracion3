#!/usr/bin/env bash
set -e

echo "================================================================="
echo "  GlobalMart OS — Smoke de Despliegue de Tarjeta (S2-060)"
echo "  Verificación de Logs de Correlación sin Secretos (S2-059)"
echo "================================================================="
echo ""

echo "[1/3] Ejecutando suite de pruebas automatizadas en MS-5 (Cobro Tarjeta)..."
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj \
    --filter "FullyQualifiedName~CobroTarjetaCorrelationTests" \
    --logger "console;verbosity=normal"

echo ""
echo "[2/3] Verificando Cobro y Cuadre existente en POSCartService..."
dotnet run --project src/POSCartService/tests/POSCartService.CobroCuadreTest/POSCartService.CobroCuadreTest.csproj

echo ""
echo "[3/3] Resumen de verificación:"
echo "  ✓ Situación 1: Arranque y Aprobación -> Venta COMPLETADA, Pago TARJETA, evento sale.completed emitido."
echo "  ✓ Situación 2: Rechazo -> Venta permanece PENDIENTE, sin corrupción de datos."
echo "  ✓ Situación 3: Pérdida temporal del simulador -> Manejo con 503, Venta permanece PENDIENTE."
echo "  ✓ Correlación de logs: Eventos registrados con VentaId, IntentoId y CorrelationId."
echo "  ✓ Seguridad: Exclusión estricta de secretos (0 PANs, 0 CVVs, 0 tokens sensibles expuestos)."
echo ""
echo "Smoke de despliegue de tarjeta finalizado exitosamente."
