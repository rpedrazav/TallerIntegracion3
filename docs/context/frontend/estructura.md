---
id: estructura
tipo: frontend
titulo: Frontend — Estructura del Proyecto Electron + React
estado: parcial
fuentes: [globalmart-frontend/src/, globalmart-frontend/package.json, globalmart-frontend/forge.config.ts]
verificado_contra_codigo: true
ultima_revision: 2026-10-25
depende_de: [stack, ms1-identity, guia-estilo]
publica: []
consume: []
reglas: [RNF-06]
---
# Frontend — Estructura del Proyecto Electron + React

> App de escritorio multiplataforma construida con Electron + React + TypeScript. Tiene páginas de Login, POS, turnos y administración; la integración del carrito con ventas de MS-5 sigue pendiente.

## Sistema visual

La aplicación debe seguir la [[guia-estilo]], aprobada el 2026-10-06. El HTML fuente contiene los tokens CSS, escalas tipográficas, colores de estado, modo oscuro y reglas de accesibilidad. La guía define la apariencia objetivo; no implica que todas las pantallas actuales ya estén adaptadas.

## Árbol de archivos

```
globalmart-frontend/
├── src/
│   ├── index.ts          Electron main process (punto de entrada)
│   ├── preload.ts        Preload script (bridge main ↔ renderer)
│   ├── renderer.tsx      Punto de entrada del renderer (React)
│   ├── App.tsx           Router principal
│   ├── api/
│   │   └── client.ts     Clientes tipados MS-1, MS-3 y MS-5
│   ├── pages/
│   │   ├── Login.tsx     Pantalla de login
│   │   ├── Pos.tsx       Pantalla POS / carrito
│   │   ├── AbrirTurnoPage.tsx
│   │   ├── CerrarTurnoPage.tsx
│   │   ├── ProductosPage.tsx
│   │   └── Admin.tsx     Pantalla administración
│   ├── components/
│   │   ├── AppLayout.tsx Layout general de la app
│   │   └── pos/
│   │       ├── BarcodeInput.tsx  Input con debounce para búsqueda
│   │       └── CartItem.tsx      Componente de ítem en carrito
│   ├── hooks/
│   │   └── useAuth.ts    Hook de autenticación con electron-store
│   ├── hardware/
│   │   ├── scale-contract.ts  Contrato tipado de balanza física/simulada
│   │   ├── simulated-scale-adapter.ts  Escenarios deterministas sin hardware
│   │   ├── serial-scale-adapter.ts  Transporte/parser inyectables, sin driver real
│   │   ├── scale-adapter-factory.ts  Selección de driver por configuración
│   │   └── scale-ipc.ts  Whitelist, validación y handlers IPC
│   └── types/
│       └── global.d.ts   Tipos globales TypeScript
├── package.json          Dependencias (Electron, React, axios, etc.)
├── forge.config.ts       Configuración Electron Forge (empaquetado)
├── tsconfig.json         Configuración TypeScript
├── webpack.main.config.ts    Webpack para main process
├── webpack.renderer.config.ts Webpack para renderer process
└── Dockerfile            Imagen Docker del frontend
```

## Rutas React Router

```tsx
// App.tsx
/ → Login
/pos → Pos (carrito de compras)
/admin → Admin (panel de administración)
```

## Tecnologías

- **Electron Forge** para empaquetado y distribución
- **webpack** para bundle del main y renderer
- **axios** para llamadas HTTP; `src/api/client.ts` centraliza URL, JWT, timeout de 15 s y manejo de 401 en MS-1, MS-3 y MS-5.
- **electron-store** para persistencia local del JWT
- **React Router** para navegación entre pantallas

## Estado actual por pantalla

| Pantalla | Integración API | Estado |
|---------|-----------------|--------|
| Login | POST /auth/login mediante `identityApi` | [IMPLEMENTADO] |
| Pos | Catálogo MS-3 mediante `catalogApi`; carrito local | [PARCIAL] |
| Admin | `GET /users` de MS-1 + creación/asignación de roles | [PARCIAL] |

## Hallazgos importantes

1. **URL del login:** `identityApi` usa el host MS-1 configurado en `src/api/client.ts`.
2. **POS y catálogo:** Pos.tsx obtiene productos de MS-3 y conserva el carrito local.
3. **Cobro no implementado:** `handleCheckout()` muestra `alert('Funcionalidad de cobro se implementará en el futuro.')`.
4. **Sin RoleSwitcher:** No hay componente para cambiar el rol activo.
5. **Turnos:** Las páginas de apertura y cierre usan MS-5; el carrito POS aún no crea ventas.
6. **Hardware parcial:** Existen contrato, simulador, estructura serial intercambiable, fábrica e IPC mínimo. No hay dependencia `serialport`, parser de fabricante, verificación física ni integración visual en React.

## Conexiones
- Autenticación: [[auth-y-roles]]
- Pantallas detalle: [[pantallas]]
- Guía visual: [[guia-estilo]]
- IPC Electron: [[electron-ipc]]
- API que consume: [[ms1-identity]], [[ms5-pos]], [[ms3-catalog]]

## Fuentes
- `globalmart-frontend/src/App.tsx`
- `globalmart-frontend/src/pages/Login.tsx`
- `globalmart-frontend/src/pages/Pos.tsx`
- `globalmart-frontend/src/hardware/scale-contract.ts`
- `globalmart-frontend/src/hardware/simulated-scale-adapter.ts`
- `globalmart-frontend/src/hardware/serial-scale-adapter.ts`
- `globalmart-frontend/src/hardware/scale-adapter-factory.ts`
- `globalmart-frontend/src/hardware/scale-ipc.ts`
- `globalmart-frontend/package.json`
