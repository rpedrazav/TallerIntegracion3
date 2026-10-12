# Regresión de carrito — S2-025

Desde `globalmart-frontend`: `npm ci`, luego `npm run test:e2e`.
Requiere Chrome instalado (channel `chrome`) y puerto 4175 libre.
`npm run test:e2e:report` abre el reporte HTML. Playwright genera JSON,
capturas y trazas de fallo en `test-results/`, y HTML en `playwright-report/`.
Las salidas se ignoran en Git; la evidencia de la ejecución inicial se conserva
en `docs/qa/S2-025/`.

Se monta el componente real Pos con HashRouter y CSS real en un servidor local.
El catálogo se intercepta con dos productos deterministas. El puente Electron
se sustituye por una sesión controlada; no se prueba login, IPC, apertura de
turno, JWT real, PostgreSQL ni aislamiento HTTP del backend.
Cada prueba tiene un contexto limpio de navegador. El caso de aislamiento
cambia la sesión dentro del mismo contexto para detectar persistencia ajena.

La aceptación exige respuesta HTTP de `/ventas` y compara por separado subtotal,
IVA y total. No hay mock de ventas ni impuestos. Actualmente falla antes de
comparar: Pos no llama a MS-5. Para acreditar integración completa deberá
ejecutarse con MS-5, turno abierto, JWT válido y catálogo alineado con los datos
del servidor cuando el POS implemente esa conexión. No interpretar esta suite
como validación de backend ni como aprobación E2E de la venta.
