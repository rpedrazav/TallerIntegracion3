# S2-084 — Pesables entre servicios

Desde la raíz del repositorio, con PostgreSQL de pruebas disponible:

```powershell
$env:S2084_TEST_CONNECTION = 'Host=127.0.0.1;Port=55484;Database=s2084;Username=postgres;Password=s2084_test'
& ./tests/GlobalMart.IntegrationTests/s2084-run.ps1
```

No existe conexión predeterminada: la variable es obligatoria para los casos HTTP/BD. `TwoTenantFixture` crea tres schemas aleatorios por caso y los elimina al terminar; nunca utiliza public como destino. El servidor indicado debe ser exclusivamente de pruebas.

Filtro ejecutado: `FullyQualifiedName~PesableEntreServiciosIntegrationTests`. Resultados: `TestResults/S2084/S2084.trx`. Todos los bin/obj se redirigen bajo tests mediante `S2082.Build.props`.

Siete cifras independientes cubren 2000 × 0.750 = 1500, IVA 285, total 1785; precio con cuatro decimales; empates de subtotal e IVA hacia par; y dos líneas cuyo IVA debe sumarse después del redondeo por línea.

- `RealMs2Calculator_AndProductionRenderer`: calculador productivo MS2; declaraciones productivas de totales de Pos.tsx ejecutadas mediante AST; CartItem productivo renderizado con ReactDOMServer. No monta Electron ni DOM de navegador.
- `HttpMs3ToMs5_PersistedPriceAndTotalsAgree`: TestServer HTTP real de MS3/MS5, JWT firmado, PostgreSQL real y comparación de líneas persistidas. MS5 usa un adaptador ITaxClient que llama al calculador productivo MS2; no prueba HTTP, autenticación ni persistencia de MS2. Verifica rechazos por JWT, tenant ajeno y turno cerrado.

No completa ventas ni realiza cobros (RN-02 no aplica). Una ejecución con cero pruebas o con casos HTTP/BD fallidos no constituye aceptación completa.

## Resultado verificado — 2026-10-26

Ejecución del filtro indicado: **14 aprobadas, 0 fallidas, 0 omitidas**, duración aproximada 20 segundos. Evidencia: `TestResults/S2084/S2084.trx`.

Los siete casos de `HttpMs3ToMs5_PersistedPriceAndTotalsAgree` ejecutan una llamada al calculador fiscal después de crear la venta. Los rechazos de POST sin JWT, producto de otro tenant y turno cerrado mantienen ese contador sin llamadas adicionales. Cada rechazo compara snapshots de las ventas de ambos tenants (identificadores, estado, subtotal, IVA, total e ítems con precio, cantidad, peso y subtotal), comprobando ausencia de mutaciones.

Los pesos son cifras deterministas de fixtures: esta suite no ejercita el simulador de balanza. MS2 se ejecuta mediante su calculador productivo dentro de un adaptador, sin HTTP MS2. La comprobación del renderer ejecuta las declaraciones productivas de totales de Pos.tsx y renderiza CartItem con ReactDOMServer; no abre Electron.
