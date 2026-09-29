---
id: estructura
tipo: frontend
titulo: Frontend — Estructura del Proyecto Electron + React
estado: parcial
fuentes: [globalmart-frontend/src/, globalmart-frontend/package.json, globalmart-frontend/forge.config.ts]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [stack, ms1-identity]
publica: []
consume: []
reglas: [RNF-06]
---
# Frontend — Estructura del Proyecto Electron + React

> App de escritorio multiplataforma construida con Electron + React + TypeScript. Actualmente solo tiene 3 páginas y está en etapa inicial de integración con el backend.

## Árbol de archivos

```
globalmart-frontend/
├── src/
│   ├── index.ts          Electron main process (punto de entrada)
│   ├── preload.ts        Preload script (bridge main ↔ renderer)
│   ├── renderer.tsx      Punto de entrada del renderer (React)
│   ├── App.tsx           Router principal
│   ├── pages/
│   │   ├── Login.tsx     Pantalla de login
│   │   ├── Pos.tsx       Pantalla POS / carrito
│   │   └── Admin.tsx     Pantalla administración (contenido NO verificado)
│   ├── components/
│   │   ├── AppLayout.tsx Layout general de la app
│   │   └── pos/
│   │       ├── BarcodeInput.tsx  Input con debounce para búsqueda
│   │       └── CartItem.tsx      Componente de ítem en carrito
│   ├── hooks/
│   │   └── useAuth.ts    Hook de autenticación con electron-store
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
- **axios** para llamadas HTTP
- **electron-store** para persistencia local del JWT
- **React Router** para navegación entre pantallas

## Estado actual por pantalla

| Pantalla | Integración API | Estado |
|---------|-----------------|--------|
| Login | POST http://127.0.0.1:5124/auth/login (directo) | [IMPLEMENTADO] |
| Pos | Datos hardcodeados, sin API real | [PARCIAL] |
| Admin | NO verificado | [NO VERIFICADO] |

## Hallazgos importantes

1. **Sin Kong:** El frontend llama directo a `http://127.0.0.1:5124/auth/login` (puerto local de MS-1 en dev), no pasa por Kong en `:8000`.
2. **Datos hardcodeados en POS:** Pos.tsx inicializa con 2 productos hardcodeados ("Coca Cola 2L", "Pan de Molde").
3. **Cobro no implementado:** `handleCheckout()` muestra `alert('Funcionalidad de cobro se implementará en el futuro.')`.
4. **Sin RoleSwitcher:** No hay componente para cambiar el rol activo.
5. **Sin integración con Turnos:** POS no abre turno antes de vender.
6. **Sin hardware:** No hay SerialPort, ni integración con balanza ni impresora.

## Conexiones
- Autenticación: [[auth-y-roles]]
- Pantallas detalle: [[pantallas]]
- IPC Electron: [[electron-ipc]]
- API que consume: [[ms1-identity]], [[ms5-pos]], [[ms3-catalog]]

## Fuentes
- `globalmart-frontend/src/App.tsx`
- `globalmart-frontend/src/pages/Login.tsx`
- `globalmart-frontend/src/pages/Pos.tsx`
- `globalmart-frontend/package.json`
