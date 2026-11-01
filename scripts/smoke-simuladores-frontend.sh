#!/usr/bin/env bash
set -e

echo "================================================================="
echo "  GlobalMart OS — Smoke de Simuladores de Hardware (S2-089)"
echo "  Balanza y Terminal POS en Entorno sin Periféricos"
echo "================================================================="
echo ""

cd globalmart-frontend
npm run smoke:simulators

echo ""
echo "Smoke de frontend completado exitosamente."
