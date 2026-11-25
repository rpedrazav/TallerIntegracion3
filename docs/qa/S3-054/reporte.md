# S3-054 — Configurar build `Setup.exe` con Electron Forge

Fecha: 2026-11-24. Sprint 3 (Semana 11). Meta: Entrega en clúster y `.exe`.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Generar el instalador de Windows (`Setup.exe`) con el modo `remote` por defecto.
- **Entrega verificable:** `GlobalMartOS-Setup.exe` reproducible con un comando documentado.
- **Aceptación:** El instalador se genera desde una copia limpia del repositorio.

## Cambios Implementados

1. **`globalmart-frontend/forge.config.ts`:**
   - Se configuró `packagerConfig` con `name: 'GlobalMartOS'`, `executableName: 'GlobalMartOS'` y `asar: true`.
   - Se configuró `MakerSquirrel` con metadatos explícitos de Windows (`name: 'GlobalMartOS'`, `exe: 'GlobalMartOS.exe'`, `setupExe: 'GlobalMartOS-Setup.exe'`).
   - Se incorporó `https://inventory-rpedraza.dev.censei.cl` en `devContentSecurityPolicy`.
2. **`globalmart-frontend/src/index.ts`:**
   - Se ajustó la ventana principal para escritorio POS (`title: 'GlobalMart OS'`, `1280×800`, mínimo `1024×680`).
   - Se restringió `openDevTools()` a `!app.isPackaged && !process.env.E2E_USER_DATA` para que el ejecutable instalado no abra DevTools frente al cajero.
3. **`globalmart-frontend/package.json`:**
   - Se definió `productName: "GlobalMartOS"`, descripción oficial y script dedicado:
     ```json
     "make:win": "electron-forge make --platform=win32 --arch=x64"
     ```
   - `webpack.plugins.ts` inyecta `'process.env.API_MODE': JSON.stringify(process.env.API_MODE || 'remote')`, garantizando que el instalador salga en modo **`remote`** por defecto (`https://{auth,catalog,pos,inventory}-rpedraza.dev.censei.cl/api`).

## Reproducción desde una Copia Limpia del Repositorio

```powershell
git clone <url-del-repo>
cd TallerIntegracion3\globalmart-frontend
npm ci
npm run make:win
```

Salida generada (ignorada por Git en `globalmart-frontend/out/`):
- Instalador Windows: `globalmart-frontend\out\make\squirrel.windows\x64\GlobalMartOS-Setup.exe`
- Paquete NuGet Squirrel: `globalmart-frontend\out\make\squirrel.windows\x64\GlobalMartOS-1.0.0-full.nupkg`
- Ejecutable empaquetado: `globalmart-frontend\out\GlobalMartOS-win32-x64\GlobalMartOS.exe`

## Evidencia de Ejecución (Build Limpio)

Se eliminaron previamente `out/` y `.webpack/` y se ejecutó:

- **`npm run typecheck`:** 0 errores, exit code 0.
- **`npm run test:unit`:** 74/74 pruebas aprobadas, exit code 0.
- **`npm run make:win`:** exit code 0 (`Making a squirrel distributable for win32/x64` completado).
- **Artefacto generado:**
  - Archivo: `out/make/squirrel.windows/x64/GlobalMartOS-Setup.exe`
  - Tamaño: `153.958.912` bytes (~146,83 MiB)
  - SHA-256: `B1986EC4E4E8A55FFF355FC373317F914869A26AA2FBC9C69A3998B963EBBCCA`
