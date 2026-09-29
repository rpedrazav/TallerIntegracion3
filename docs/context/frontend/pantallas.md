---
id: pantallas
tipo: frontend
titulo: Pantallas del Frontend — GlobalMart OS
estado: parcial
fuentes: [globalmart-frontend/src/pages/, globalmart-frontend/src/components/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [estructura, ms1-identity, ms5-pos]
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
  - Password: demo1234
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

## Admin.tsx [NO VERIFICADO]

Pantalla de administración cuyo contenido completo no fue verificado. Existe en el router como `/admin`.

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
- Gestión de Turnos (abrir/cerrar turno)
- Gestión de Productos (CRUD admin)
- Dashboard de ventas
- Gestión de inventario
- Programa de lealtad
- Configuración de tenant

## Conexiones
- Estructura: [[estructura]]
- Auth: [[auth-y-roles]]
- Diagramas UI: [[indice-diagramas]]

## Fuentes
- `globalmart-frontend/src/pages/Login.tsx`
- `globalmart-frontend/src/pages/Pos.tsx`
- `globalmart-frontend/src/components/pos/BarcodeInput.tsx`
- `globalmart-frontend/src/components/pos/CartItem.tsx`
