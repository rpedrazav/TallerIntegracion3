---
id: pantallas
tipo: frontend
titulo: Pantallas del Frontend — GlobalMart OS
estado: parcial
fuentes: [globalmart-frontend/src/pages/, globalmart-frontend/src/components/, globalmart-frontend/src/api/client.ts]
verificado_contra_codigo: true
ultima_revision: 2026-10-15
depende_de: [estructura, ms1-identity, ms5-pos, guia-estilo]
publica: []
consume: []
reglas: []
---
# Pantallas del Frontend — GlobalMart OS

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
  - Totales locales antes de iniciar venta; luego subtotal, impuestos y total devueltos por MS-5
  - Botón "Iniciar venta" que envía producto_id y cantidad; después muestra "Cobrar" (cobro aún no implementado)
  - Botón "Cancelar borrador" antes de crear venta; en venta pendiente permite agregar, cambiar cantidad y eliminar ítems

Productos obtenidos de MS-3 mediante `catalogApi`; solo el borrador anterior a la venta se guarda en `localStorage`
Al entrar a /pos, consulta `posApi.activeShift`. Mientras espera muestra carga; ante 404 redirige a /abrir-turno. Si falla la conexión, bloquea el POS y ofrece reintentar. El catálogo se consulta solo después de confirmar el turno activo [IMPLEMENTADO].

POST /ventas envía items [{producto_id, cantidad}] y metodo_pago EFECTIVO; no envía precio ni impuesto.
GET /ventas/turno/{turnoId} identifica la última venta PENDIENTE del turno activo al abrir POS y antes de crear otra; GET /ventas/{id} recupera su estado completo antes de mostrar el carrito. Si falla la recuperación, POS muestra un error y ofrece reintentar sin usar el borrador local como autoridad.
El ID, los ítems y los totales de la venta pendiente vienen de MS-5; el borrador local deja de ser autoridad.
En venta pendiente, búsqueda y escaneo agregan ítems con `POST /ventas/{id}/items` o aumentan cantidad con `PUT /ventas/{id}/items/{itemId}`. Editar la cantidad de una fila usa el ID del ítem de MS-5. Tras cada operación consulta `GET /ventas/{id}` para renovar carrito, subtotal, impuestos y total; si no puede confirmar el estado, bloquea nuevas ediciones hasta reintentar la consulta [IMPLEMENTADO].
Eliminar una fila de la venta pendiente llama `DELETE /ventas/{id}/items/{itemId}` y vuelve a consultar `GET /ventas/{id}`. El ítem eliminado no se repone desde `localStorage` [IMPLEMENTADO].
Errores de inicio visibles; ante respuesta perdida, vuelve a consultar el turno antes de permitir otro intento [IMPLEMENTADO].
NO abre turno desde esta pantalla; dirige a /abrir-turno cuando falta
```

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
- **Usuarios:** Lista usuarios (`GET /users` en MS-1, con alias `/api/v1/users` y `/api/users`) mediante el componente `UsuariosList` (`components/admin/UsuariosList.tsx`, mostrando nombre, correo, roles con badges/chips y estado activo/inactivo), botón de refresco y formulario modal `CrearUsuarioModal` (`components/admin/CrearUsuarioModal.tsx`, nombre, correo, contraseña ≥ 8 con confirmación, rol CAJERO/REPONEDOR/ADMIN; validación en frontend alineada a `CrearUsuarioDto`). Al enviar llama `POST /users` y luego `POST /users/{id}/roles` con extracción de errores de validación/conflicto.
- **Configuración del Tenant:** Componente `TenantConfig` (`components/admin/TenantConfig.tsx`) que conecta con `GET /tenants/{id}/config` y `PUT /tenants/{id}/config` en MS-1. Permite ver y actualizar parámetros regionales y fiscales: país, moneda, idioma, zona horaria IANA y porcentaje de IVA. TI3-645 alinea Zod 4, `zodResolver` y React Hook Form mediante tipos separados de entrada/salida; `z.coerce.number()` convierte el IVA y `onSubmit` recibe un `number`, sin casts inseguros. Maneja feedback de carga, éxito (`var(--color-success)`), advertencia en modo lectura para no administradores y errores.

Usa los tokens de color de la [[guia-estilo]] definidos en `index.css`. No hay edición ni desactivación individual de usuarios en UI todavía.

## ProductosPage.tsx — baja lógica TI3-642 / S2-011 [IMPLEMENTADO]

Ruta `/admin/productos`. La pantalla usa Axios directamente contra MS-3 y muestra productos activos e inactivos. La acción visible **Desactivar** confirma que el producto será **DESACTIVADO** y se conservará en el catálogo; ya no anuncia eliminación permanente.

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
