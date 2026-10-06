---
id: pantallas
tipo: frontend
titulo: Pantallas del Frontend — GlobalMart OS
estado: parcial
fuentes: [globalmart-frontend/src/pages/, globalmart-frontend/src/components/]
verificado_contra_codigo: true
ultima_revision: 2026-10-06
depende_de: [estructura, ms1-identity, ms5-pos, guia-estilo]
publica: []
consume: []
reglas: []
---
# Pantallas del Frontend — GlobalMart OS

> Estado real de las pantallas implementadas en el frontend Electron + React.

## Login.tsx [IMPLEMENTADO]

```
Pantalla de inicio de sesión con 3 campos:
  - Tenant ID (GUID)
  - Email
  - Contraseña

Al enviar:
  → POST http://127.0.0.1:5124/auth/login (directo a MS-1, sin Kong)
  → Si exitoso: await login(token) → navigate('/pos')
  → Si error: muestra mensaje de error

Valores pre-llenados para desarrollo:
  - Email: cajero@demo.cl
  - Password: (ver seed en [[como-ejecutar]])
  - Tenant ID: aaaaaaaa-0000-0000-0000-000000000001
```

**Problema:** URL hardcodeada a puerto de desarrollo. En producción debería apuntar a Kong `:8000`.

## Pos.tsx [PARCIAL]

```
Carrito de compras con:
  - BarcodeInput (búsqueda con debounce)
  - Lista de CartItem (ítems del carrito)
  - Total con IVA (calculado localmente al 19% fijo)
  - Botón "Cobrar" (muestra alert de "no implementado")
  - Botón "Cancelar Venta" (limpia el carrito con confirm)

Datos hardcodeados al iniciar:
  - Coca Cola 2L: $2.500 × 2
  - Pan de Molde Castaño: $1.800 × 1

IVA calculado localmente (tasaIva = 0.19 fijo, sin llamar MS-2)
NO integra la API de ventas de MS-5
NO abre turno antes de vender
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
//   - GET /api/turnos/activo: detecta si el cajero ya tiene un turno abierto
//   - POST /api/turnos/abrir: registra apertura con sucursal_id y monto_fondo_inicial
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
//   - Arqueo y Cuadre: POST /api/turnos/cuadre con monto_declarado (compara efectivo esperado vs declarado y calcula diferencia)
//   - Cierre de turno: POST /api/turnos/cerrar con confirmación y opciones de navegación
```

## Admin.tsx [PARCIAL]

Ruta `/admin`. Lista usuarios (`GET /users` en MS-1, con alias `/api/v1/users` y `/api/users`) mediante el componente `UsuariosList` (`components/admin/UsuariosList.tsx`, mostrando nombre, correo, roles con badges/chips y estado activo/inactivo), botón de refresco y abre `components/admin/CrearUsuarioModal.tsx` (nombre, correo, contraseña ≥ 8 con confirmación, rol CAJERO/REPONEDOR/ADMIN; validación en frontend alineada a `CrearUsuarioDto`). Al enviar llama `POST /users` y luego `POST /users/{id}/roles`. Usa los tokens de color de la [[guia-estilo]] definidos en `index.css`. No hay edición ni desactivación de usuarios en UI.

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
- Gestión de Productos (CRUD admin)
- Dashboard de ventas
- Gestión de inventario
- Programa de lealtad
- Configuración de tenant

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
