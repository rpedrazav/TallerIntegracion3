# Regresión tras simplificar VentaService

Fecha: 2026-11-24. Base: commit 3903464 (TI3-760); `VentaService` no tiene cambios de cálculo de totales (solo asigna los recibidos).

- MS-5 integración (selección en [filtro.txt](filtro.txt): ventas, pesables, redondeo, cobro efectivo/tarjeta/seguro, turnos, cierre): **205 aprobadas, 0 fallidas**, PostgreSQL real. [actual.trx](actual.trx)
- CobroCuadre: exit 0, todo PASS. [CobroCuadreTest.log](CobroCuadreTest.log)
- AgregarItem: exit 0, todo PASS. [AgregarItemTest.log](AgregarItemTest.log)
- E2E POS (cart, cash-payment, weight-cart, ventas-turno): **33 aprobadas, 0 fallidas** (tras instalar chromium-headless-shell). [e2e.log](e2e.log)

Límite: no se re-ejecutó la línea base anterior; la comparación de totales se apoya en que las pruebas con importes fijos pasan sin cambios.
