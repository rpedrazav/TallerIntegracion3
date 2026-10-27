---
id: pantallas
tipo: frontend
titulo: Pantallas del Frontend — GlobalMart OS
estado: parcial
fuentes: [globalmart-frontend/src/pages/, globalmart-frontend/src/components/, globalmart-frontend/src/api/client.ts, globalmart-frontend/src/hardware/scale-cart.ts, globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts, globalmart-frontend/src/payments/terminal-ipc.ts, globalmart-frontend/src/payments/synchronizeCardPayment.ts, globalmart-frontend/src/preload.ts, globalmart-frontend/tests/scale-main-preload.integration.test.cjs, globalmart-frontend/e2e/weight-cart.spec.ts, globalmart-frontend/tests/terminal-port.test.cjs, globalmart-frontend/tests/terminal-ipc.test.cjs, globalmart-frontend/tests/synchronized-card-payment.test.cjs]
verificado_contra_codigo: true
ultima_revision: 2026-10-27
depende_de: [estructura, ms1-identity, ms5-pos, guia-estilo]
publica: []
consume: []
reglas: []
---
# Pantallas del Frontend — GlobalMart OS

## Panel administrativo (2026-10-27)

`AdminLayout` separa administración de caja. `/admin/usuarios` crea cuentas con correo, rol, sucursal opcional y contraseña inicial editable; tras crear ofrece los datos de acceso para copiar. `/admin/roles` modifica asignaciones. `/admin/sucursales` crea y lista sucursales. `/admin/inventario` consulta stock y registra ajustes con motivo contra MS-4. `/admin/productos` y `/admin/configuracion` reutilizan las funciones existentes. Funcionamiento remoto de inventario [NO VERIFICADO] hasta desplegar MS-4.

> Estado real de las pantallas implementadas en el frontend Electron + React.

Los clientes tipados en `src/api/client.ts` cubren login (MS-1), catálogo del POS (MS-3) y turnos (MS-5). Centralizan las URL, el JWT y el timeout de 15 s. En peticiones protegidas, un 401 cierra la sesión y redirige al login [IMPLEMENTADO].

## Login.tsx [IMPLEMENTADO]

```
Pantalla de inicio de sesión con 3 campos:
  - Tenant ID (GUID)
  - Email
  - Contraseña

Al enviar:
  → POST /auth/login mediante identityApi.login
  → Si exitoso: await window.api.setToken(token) → navigate('/pos')
  → Si error: muestra mensaje de error

Valores pre-llenados para desarrollo:
  - Email: cajero@demo.cl
  - Password: (ver seed en [[como-ejecutar]])
  - Tenant ID: aaaaaaaa-0000-0000-0000-000000000001
```

La URL base de MS-1 está en `src/api/client.ts`.

## Pos.tsx [PARCIAL]

```
Carrito de compras con:
  - BarcodeInput (búsqueda con debounce)
  - Lista de CartItem (ítems del carrito)
  - Subtotal estimado antes de iniciar venta; luego subtotal, impuestos y total devueltos por MS-5
  - Botón "Iniciar venta" que envía producto_id y cantidad; después muestra "Cobrar" y abre el modal de efectivo
  - Botón "Cancelar borrador" antes de crear venta; en venta pendiente permite agregar, cambiar cantidad y eliminar ítems

Productos obtenidos de MS-3 mediante `catalogApi`; solo el borrador anterior a la venta se guarda en `localStorage`
Al entrar a /pos, consulta `posApi.activeShift`. Mientras espera muestra carga; ante 404 redirige a /abrir-turno. Si falla la conexión, bloquea el POS y ofrece reintentar. El catálogo se consulta solo después de confirmar el turno activo [IMPLEMENTADO].

POST /ventas envía `producto_id`, `cantidad` y, solo para productos pesables, `peso_kg`; nunca envía precio ni impuesto. El método puede ser EFECTIVO o TARJETA.
GET /ventas/turno/{turnoId} identifica la última venta PENDIENTE del turno activo al abrir POS y antes de crear otra; GET /ventas/{id} recupera su estado completo antes de mostrar el carrito. Si falla la recuperación, POS muestra un error y ofrece reintentar sin usar el borrador local como autoridad.
El ID, los ítems y los totales de la venta pendiente vienen de MS-5; el borrador local deja de ser autoridad.
En venta pendiente, búsqueda y escaneo agregan ítems con `POST /ventas/{id}/items` o aumentan cantidad con `PUT /ventas/{id}/items/{itemId}`. Editar la cantidad de una fila usa el ID del ítem de MS-5. Tras cada operación consulta `GET /ventas/{id}` para renovar carrito, subtotal, impuestos y total; si no puede confirmar el estado, bloquea nuevas ediciones hasta reintentar la consulta [IMPLEMENTADO].
Eliminar una fila de la venta pendiente llama `DELETE /ventas/{id}/items/{itemId}` y vuelve a consultar `GET /ventas/{id}`. El ítem eliminado no se repone desde `localStorage` [IMPLEMENTADO].
Errores de inicio visibles; ante respuesta perdida, vuelve a consultar el turno antes de permitir otro intento [IMPLEMENTADO].
NO abre turno desde esta pantalla; dirige a /abrir-turno cuando falta
```

### Balanza simulada — S2-070 / TI3-701 [IMPLEMENTADO CON SIMULADOR]

S2-076: `PesoProducto` muestra el producto pesable seleccionado, origen simulado, unidad kg y estados de lectura, peso estable y error. Los controles del simulador aparecen solo durante la captura de un pesable; los productos unitarios no presentan controles de balanza.

S2-077: la selección de un pesable solicita lectura estable por preload y muestra el peso antes de agregar. Solo `Confirmar peso` envía la cantidad a MS-5 o al borrador; cancelar y errores no modifican el carrito. Reintentar vuelve a consultar el estado de la balanza.

S2-078: ante fallo o desconexión se ofrece ingreso manual identificado. La misma validación exige al menos 0,010 kg y pasos de 0,001 kg; texto no numérico, cero o negativo no habilita confirmar ni envía ítems.

S2-079: cada línea pesable identifica kg y precio por kg. Antes de iniciar venta se muestra solo un subtotal estimado; impuestos y total esperan la respuesta de MS-5. Después de crear o mutar la venta, subtotal de línea, subtotal general, impuestos y total se muestran desde `VentaPendiente`, sin tasa fija de IVA en la interfaz.

S2-080: tres pruebas E2E controladas recorren lectura estable y confirmación, modificación acumulada en una sola línea, recuperación tras recarga, desconexión, respaldo manual y producto unitario. Verifican que valores inválidos no crean líneas ni peticiones, que los controles de balanza no aparecen para unitarios y que el total mostrado coincide con la respuesta simulada de MS-5 (IVA de prueba 20 %).

Al seleccionar un producto con `esPesoVariable`, el POS llama exclusivamente a `readStableWeightForCart(window.api.scale)`. Mientras espera bloquea otra lectura y muestra estado; solo ofrece confirmar cuando recibe un peso estable, positivo, en kg, de al menos 10 g y con precisión de 1 g. Un dato corrupto, error o desconexión deja el carrito sin cambios y presenta un mensaje. El intento siguiente consulta el estado y reconecta antes de leer cuando corresponde.

La cantidad de una línea pesable se muestra con tres decimales en kg y no puede editarse manualmente. Crear, agregar y modificar envían `cantidad = peso_kg` y `peso_kg`; los totales posteriores continúan proviniendo de MS-5. La prueba integrada de simulador/main/preload confirma la secuencia válida, corrupta, desconectada y recuperada; hardware real [NO VERIFICADO].

### Modal de cobro — contrato visual S2-046 [IMPLEMENTADO]

`PaymentModal` presenta el total devuelto por MS-5 y permite ver las vistas de efectivo y tarjeta. En efectivo muestra monto recibido y diferencia estimada; tras un cobro aprobado muestra el vuelto devuelto por MS-5. En tarjeta muestra solo el monto y, si la pasarela ya respondió, su referencia opaca. No pide ni conserva número de tarjeta, CVV, PIN ni vencimiento. El método seleccionado debe coincidir con `metodo_pago` de la venta para habilitar la confirmación.

| Estado | Mensaje visible | Acción permitida |
|---|---|---|
| Disponible | Método, total y detalle de efectivo o tarjeta | Confirmar solo con método compatible, monto válido en efectivo y manejador de pago conectado |
| Procesando | Esperando respuesta; evitar otro cobro | Sin cerrar ni repetir |
| Aprobado | Venta confirmada; referencia opaca si es tarjeta | Cerrar |
| Rechazado | Venta pendiente; motivo del rechazo | Cambiar método o cerrar |
| Resultado incierto | Verificar venta antes de intentar otro cobro | Verificar, cuando exista manejador; o cerrar |
| Cancelado | Intento cancelado | Cerrar; MS-5 todavía no soporta esta transición remotamente |

El botón **Cobrar** del POS abre el modal en estado Disponible. En una venta `EFECTIVO`, confirmar un monto positivo llama `POST /ventas/{id}/cobrar` con `monto_recibido`; Procesando evita un segundo envío, Aprobado usa la venta `COMPLETADA` y el `vuelto` de la respuesta, y un 400 `Monto insuficiente` conserva la venta `PENDIENTE` y muestra `diferencia` devuelta por MS-5 [IMPLEMENTADO]. Ante un resultado de red incierto, bloquea otra confirmación y permite consultar `GET /ventas/{id}`; la consulta puede confirmar el estado de la venta, pero no reconstruye el vuelto si se perdió la respuesta del cobro. Antes de iniciar una venta se elige `EFECTIVO` o `TARJETA`. La tarjeta llama `POST /ventas/{id}/cobrar-tarjeta` con una clave UUID nueva, muestra aprobación y referencia opaca o rechazo, y permite cambiar el método de una venta pendiente mediante `PATCH /ventas/{id}/metodo-pago`. Un resultado incierto exige verificar la venta antes de otro cobro [IMPLEMENTADO]. Pruebas de interfaz controladas 4/4 aprobadas el 2026-10-18; integración E2E con la pasarela desplegada [NO VERIFICADA].

S2-049: ante timeout o error de tarjeta, el modal muestra Resultado incierto y solo ofrece verificar. `GET /ventas/{id}/intento-tarjeta` recupera el último intento al recargar POS y consulta la pasarela; `UNCERTAIN` mantiene el cobro bloqueado, `APPROVED` cierra la venta y `DECLINED` permite un nuevo intento manual con otra clave o cambiar método. No hay reenvío automático del POST. Prueba dirigida de interfaz con doble clic, timeout, recarga y recuperación aprobada el 2026-10-18; E2E con PostgreSQL y pasarela desplegada [NO VERIFICADA].

S2-071 / TI3-702 [IMPLEMENTADO]: el flujo de tarjeta consume `TerminalPort` mediante `Ms5TerminalAdapter`. El puerto separa `iniciarCobro`, `consultarEstado` y `cancelarIntento`, usando solo `ventaId`, monto, clave/referencia del intento, estado y referencia opaca; no define PAN, número de tarjeta, CVV ni vencimiento. `terminalStateMachine` centraliza `available`, `processing`, `approved`, `declined`, `uncertain` y `cancelled` con transiciones explícitas. MS-5 respalda inicio y consulta con los endpoints existentes; como no expone cancelación, el adaptador devuelve `CANCELACION_NO_SOPORTADA` sin llamada HTTP. Pruebas puras **4/4** y TypeScript aprobados el 2026-10-25.

S2-072 / TI3-703 [IMPLEMENTADO]: `createTerminalPort()` selecciona por `DEVICE_TERMINAL_MODE`: `mock` crea `SimulatedTerminalAdapter` y `real` conserva `Ms5TerminalAdapter`. El simulador implementa `TerminalPort` y reproduce de forma determinista `waiting`, `approved`, `declined`, `cancelled`, `timeout` y `disconnected`; lectura/inicio significa que `iniciarCobro` recibe los datos del intento y pasa a `processing`. `timeout` espera el `MOCK_TERMINAL_DELAY_MS` configurado y termina en el estado existente `uncertain`, conservando venta, monto y referencia del mismo intento; nunca se convierte en `declined` ni crea otro intento. El adaptador genera una referencia opaca estable sin `Math.random()` ni `Date.now()`. `Pos.tsx` desconoce los detalles del adaptador. No solicita ni almacena PAN/CVV y permanece separado del `MockTerminalAdapter`/`ITerminalAdapter` legado. TI3-703 no agregó IPC; el bridge incorporado después se documenta en TI3-704. Pruebas enfocadas **12/12** y TypeScript aprobados el 2026-10-26.

S2-073 / TI3-704 [IMPLEMENTADO]: `Pos.tsx` inicia y consulta intentos exclusivamente mediante la API tipada `window.api.terminal`, expuesta por `contextBridge` con los canales fijos `devices:terminal:iniciarCobro` y `devices:terminal:consultarEstado`. El preload no expone `send`, `invoke`, selección libre de canal, cancelación ni comandos de hardware en esta API. Main conserva un único `TerminalPort` simulado por proceso y valida antes de invocarlo objetos planos con listas exactas de propiedades: inicio acepta solo `ventaId`, `monto` y `claveIdempotencia`; consulta acepta solo `ventaId`, `monto` y `referenciaIntento`. UUID, monto positivo finito dentro del máximo y referencias seguras/acotadas son obligatorios; propiedades adicionales se rechazan. La respuesta IPC se reduce a venta, monto, referencia del intento, estado y referencia opaca, sin PAN/CVV/vencimiento. En `DEVICE_TERMINAL_MODE=real`, main falla de forma controlada y explícita porque el transporte MS-5 desde main permanece no soportado; no aplica fallback a `mock`. Los IPC heredados de `ITerminalAdapter` permanecen separados. Pruebas IPC puras **6/6** y TypeScript aprobados el 2026-10-26.

S2-074 / TI3-705 [IMPLEMENTADO]: la aprobación de `TerminalPort` es preliminar y nunca basta para mostrar la venta cobrada. `Pos.tsx` genera o reutiliza una sola clave por intento; `synchronizeCardPayment` la entrega al terminal como `claveIdempotencia`, exige conservarla como `referenciaIntento` y, solo si el terminal aprueba, llama `posApi.chargeCard` con esa misma clave. MS-5 la usa como `request_id` y `Idempotency-Key` ante la pasarela. La UI pasa a `approved` únicamente cuando MS-5 responde `APPROVED` con la misma venta en estado `COMPLETADA`; muestra exclusivamente el `gateway_reference` confirmado por MS-5, no la referencia opaca local del terminal.

Si la respuesta de autorización es ambigua o falla, `cardAttemptStatus` consulta el intento existente sin generar otra clave. La reconciliación solo aprueba cuando `request_id` coincide con la referencia esperada, el estado es `APPROVED` y la venta está `COMPLETADA`; una referencia distinta o un resultado no definitivo deja la UI en `uncertain`. La recuperación inicial y `verifyPayment()` también consultan MS-5 como fuente final y no crean una autorización nueva. Pruebas puras del flujo sincronizado **6/6** y TypeScript aprobados el 2026-10-26.

La búsqueda del POS consulta `GET /products/search` por nombre o código parcial y `GET /products/lookup` por código exacto, sin depender de la primera página del catálogo. Muestra carga, ausencia de resultados y errores; Enter conserva el texto si no se agregó un producto. `/admin/productos` permanece reservado a ADMIN/SUPER_ADMIN para la gestión administrativa; el cajero usa la búsqueda del POS [IMPLEMENTADO].

## AbrirTurnoPage.tsx [IMPLEMENTADO]

```typescript
// Pantalla de apertura de turno de caja (RN-06)
// Ruta: /abrir-turno
// Campos:
//   - "Monto inicial de caja" (input numérico con validación >= 0)
//   - Accesos directos para montos frecuentes ($0, $20.000, $50.000, $100.000)
//   - Botón "Abrir Turno" con estado de carga y validaciones
// Integración API:
//   - posApi.activeShift: GET /api/turnos/activo
//   - posApi.openShift: POST /api/turnos/abrir con sucursal_id y monto_fondo_inicial
//   - Error 409 Conflict: muestra "Ya tienes un turno abierto"
//   - Redirección automática a /pos con mensaje de confirmación en location.state
```

## CerrarTurnoPage.tsx [IMPLEMENTADO]

```typescript
// Pantalla de cierre de turno y cuadre de caja (PC-04, RF-03)
// Ruta: /cerrar-turno
// Componentes y lógica:
//   - Tabla interactiva con denominaciones de billetes ($20.000, $10.000, $5.000, $2.000, $1.000) y monedas ($500, $100, $50, $10)
//   - Input numérico de cantidad por fila con stepper (+ / -) y subtotal calculado en tiempo real
//   - Subtotales por categoría (Billetes / Monedas) y Total Declarado en gaveta
//   - Botón "Limpiar Conteo"
//   - Arqueo y Cuadre: posApi.balanceShift llama POST /api/turnos/cuadre
//   - Cierre de turno: posApi.closeShift llama POST /api/turnos/cerrar
```

## Admin.tsx [PARCIAL]

Ruta `/admin`. Panel de administración con pestañas ("Usuarios" y "Configuración del Tenant"):
- **Usuarios:** Lista usuarios (`GET /users` en MS-1, con alias `/api/v1/users` y `/api/users`) mediante el componente `UsuariosList` (`components/admin/UsuariosList.tsx`, mostrando nombre, correo, roles con badges/chips y estado activo/inactivo), botón de refresco y formulario modal `CrearUsuarioModal` (`components/admin/CrearUsuarioModal.tsx`, nombre, correo, contraseña ≥ 8 con confirmación, rol CAJERO/REPONEDOR/ADMIN; validación en frontend alineada a `CrearUsuarioDto`). Al enviar llama `POST /users` y luego `POST /users/{id}/roles` con extracción de errores de validación/conflicto. TI3-673 agrega una acción **Editar** por fila y `EditarUsuarioModal`, precargado con el email disponible. Valida email requerido, formato y máximo 200 caracteres, ejecuta `PUT /users/{id}` con el mismo ID seleccionado, muestra estados de guardado/éxito/error y recarga la lista mediante `recargar()` sin crear otro usuario [IMPLEMENTADO]. TI3-674 agrega **Desactivar** solo para usuarios activos, exige confirmación y ejecuta `DELETE /users/{id}`. Maneja autodesactivación, sesión, permisos, inexistente/otro tenant y fallos de red [IMPLEMENTADO].
- **Configuración del Tenant:** Componente `TenantConfig` (`components/admin/TenantConfig.tsx`) que conecta con `GET /tenants/{id}/config` y `PUT /tenants/{id}/config` en MS-1. Permite ver y actualizar parámetros regionales y fiscales: país, moneda, idioma, zona horaria IANA y porcentaje de IVA. TI3-645 alinea Zod 4, `zodResolver` y React Hook Form mediante tipos separados de entrada/salida; `z.coerce.number()` convierte el IVA y `onSubmit` recibe un `number`, sin casts inseguros. La prueba Playwright `TenantConfig convierte porcentajeIva a number antes de actualizar` verifica que un IVA introducido en el formulario se envía en el `PUT` como `number` y no como `string` [IMPLEMENTADO]. Maneja feedback de carga, éxito (`var(--color-success)`), advertencia en modo lectura para no administradores y errores. TI3-675 conserva sin cambios la edición ADMIN/SUPER_ADMIN, el bloqueo durante carga/guardado y el feedback de éxito/error existente.

**Contrato de acceso TI3-672 / TI3-675 [IMPLEMENTADO]:** `AppLayout` y `AdminRoute` consumen `esAdmin` desde `useTenantStore`, que reconoce los claims `active_role`/`roles` con valor `ADMIN` o `SUPER_ADMIN`. La misma guarda protege `/admin/*` y `/admin/productos`. ADMIN/SUPER_ADMIN acceden a las funciones administrativas; CAJERO no recibe su contenido y ve el mensaje **Acceso denegado** después de resolver la sesión, sin falso rechazo durante la carga. MS-1 mantiene `403` como defensa. La prueba Node `ADMIN y SUPER_ADMIN acceden a Administración y CAJERO no` cubre la regla compartida, sin navegador.

La UI expone edición de email y baja lógica; todavía no expone reactivación. MS-1 dispone de PUT y DELETE lógico. Su contrato actual contempla `400` por datos inválidos o autodesactivación según la operación, `401` sin autenticación, `403` para rol no autorizado, `404` para usuario inexistente o de otro tenant y `409` por email duplicado en edición.

**Edición de usuario:** `GET /users` aún no entrega `sucursalId` y el modal de edición muestra solo email. El backend conserva la asignación existente cuando se omite `sucursalId`; la creación sí admite sucursal inicial.

**Baja lógica:** tras un DELETE 204, la pantalla conserva la fila con `activo=false`; `GET /users` ahora incluye inactivos al recargar. No existe vista/API de auditoría consultable.

Usa los tokens de color de la [[guia-estilo]] definidos en `index.css`. No hay reactivación individual de usuarios en UI todavía.

## ProductosPage.tsx — baja lógica TI3-642 / S2-011 [IMPLEMENTADO]

Ruta `/admin/productos`, protegida por `AdminRoute` para ADMIN/SUPER_ADMIN. La pantalla usa Axios directamente contra MS-3 y muestra productos activos e inactivos. La acción visible **Desactivar** confirma que el producto será **DESACTIVADO** y se conservará en el catálogo; ya no anuncia eliminación permanente.

Mantiene `axios.delete` a `DELETE /products/{id}` con Bearer y `fetchDatos()` para recargar tras una respuesta exitosa, incluido 204 sin body. El producto permanece con el indicador **Inactivo**, salvo que el toggle **solo activos** lo oculte. Los errores visibles hablan de desactivación.

El backend exige ADMIN, conserva físicamente la fila mediante `IsActive=false`, retorna 204 idempotente y el mismo 404 para inexistente/otro tenant. La pantalla no agrega una guarda de rol; la autorización se aplica en MS-3. PUT requiere ADMIN para cambiar el estado. TI3-642 no modificó `TenantConfig.tsx`.

Validación 2026-10-14: TypeScript `--noEmit --incremental false --pretty false` aprobado. Contrato backend validado con 11 casos de [DesactivarProductoIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs). Validación visual/E2E específica de la acción: [NO VERIFICADO].

## BarcodeInput.tsx [IMPLEMENTADO]

```typescript
// Componente con debounce para búsqueda de productos
// Recibe: onSearch callback
// El debounce evita múltiples llamadas al escribir rápido
// Actualmente onSearch solo hace console.log (sin llamar MS-3)
```

## CartItem.tsx [IMPLEMENTADO]

```typescript
// Componente que muestra un ítem del carrito
// Props: ProductItem { id, name, price, quantity }
// Permite cambiar cantidad y eliminar ítem (estado local en Pos.tsx)
// Sin integración con API de MS-5
```

## Pantallas diseñadas (PLANIFICADAS)

Según diagramas `diagramas/rodrigo/`:
- R1: Screen Flow (flujo de pantallas)
- R2: Wireframes (diseños de UI)
- R3: Component Tree (árbol de componentes React)
- R4: State Management (gestión de estado)

Pantallas no implementadas:
- Cierre de Turno y Cuadre de Caja (PC-04)
- Dashboard de ventas
- Gestión de inventario
- Programa de lealtad

## Conexiones
- Estructura: [[estructura]]
- Guía visual aprobada: [[guia-estilo]]
- Auth: [[auth-y-roles]]
- Diagramas UI: [[indice-diagramas]]

## Fuentes
- `globalmart-frontend/src/pages/Login.tsx`
- `globalmart-frontend/src/pages/Pos.tsx`
- `globalmart-frontend/src/pages/AbrirTurnoPage.tsx`
- `globalmart-frontend/src/pages/CerrarTurnoPage.tsx`
- `globalmart-frontend/src/components/pos/BarcodeInput.tsx`
- `globalmart-frontend/src/components/pos/CartItem.tsx`

## S2-025 — Regresión de carrito

[PARCIAL] Suite Playwright sobre el renderer real de POS, con catálogo y puente de sesión controlados. Ejecución 2026-10-12: 1 aprobado (agregar, cantidad, eliminar y recargar), 2 fallidos (sin respuesta de ventas MS-5 y persistencia del carrito al cambiar sesión de tenant). Pos usa `pos_cart` sin partición por tenant y calcula IVA local al 19%. Igualdad con totales de MS-5 y aislamiento HTTP de backend [NO VERIFICADO]. No acredita E2E completo ni venta funcional. Reporte y capturas: [S2-025](../../qa/S2-025/reporte.md).
