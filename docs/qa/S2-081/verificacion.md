# S2-081 — Verificación documental

Fecha: 2026-10-25. Documento revisado: [plan de validación](plan-validacion.md).

Pruebas persistentes: [ScaleValidationPlanTests.cs](../../../tests/GlobalMart.IntegrationTests/ScaleValidationPlanTests.cs).

Comando ejecutado desde la raíz del repositorio:

```text
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --filter "FullyQualifiedName~ScaleValidationPlanTests" --nologo -v q
```

Resultado final real: salida 0; 3 pruebas aprobadas, 0 fallidas y 0 omitidas; duración informada por el ejecutor: 9 ms (.NET 8).

Se verificaron la separación de filas SIMULADA/FÍSICA y sus columnas de evidencia y límites; los requisitos explícitos de dispositivo prestado, modelo, protocolo e implementación real; la prohibición de aprobar integración física mediante mocks; y la existencia de todos los destinos de enlaces locales del plan. La comprobación de enlaces acredita existencia, no valida el contenido de sus destinos.

La compilación mostró advertencias MSB3539 de rutas intermedias y xUnit2031 en VentaIntegrationTests.cs; no hubo errores. El primer intento terminó sin diagnóstico dentro del entorno restringido; se repitió con acceso ampliado. Se corrigió una importación de Xunit faltante antes del resultado final.

Alcance: aceptación documental exclusivamente. No se ejecutaron suites del simulador, serial, IPC o POS ni se conectó una balanza. Este resultado no declara aprobada ninguna integración funcional simulada o física. La integración física permanece CONDICIONADA / [NO VERIFICADO].

Nodos del Grafo de Contexto sin cambios: esta entrega agrega un plan y su verificación documental; no altera contratos, arquitectura, comportamiento ni estado funcional del producto.
