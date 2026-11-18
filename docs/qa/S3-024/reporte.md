# S3-024 / TI3-775 — Eliminar archivos basura de la raíz

Fecha: 2026-11-17. Sprint 3 (Semana 10). Meta: Frontend simple y agradable.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Borrar los archivos inventariados en la Semana 9 (`S3-004` / `TI3-755`) y ajustar `.gitignore` para que no vuelvan.
- **Entrega verificable:** Commit de limpieza y `.gitignore` actualizado.
- **Aceptación:** El build y el CI siguen pasando después de la limpieza.

## Archivos Eliminados de la Raíz (23 archivos versionados, ~46,62 MiB)

- **Scripts temporales `fix*.js` (6):** `fix.js`, `fix2.js`, `fix3.js`, `fix4.js`, `fix5.js`, `fix_btn.js`.
- **Scripts y payloads de prueba manual (6):** `test.js`, `test.json`, `test2.json`, `test_abrir.json`, `test_auth.json`, `test_bad.json`.
- **Scripts y volcados SQL sueltos (4):** `script.py`, `query.sql`, `generate_sql.ps1`, `insert_productos.sql`.
- **Script personal hardcodeado (1):** `Patch-Envs.ps1` (ruta personal `C:\Users\kkk\Downloads\...`).
- **Lockfile vacío en raíz (1):** `package-lock.json` (`"packages": {}`, sin `package.json` en la raíz).
- **Binarios y comprimido de `kubelogin` (5 + directorio `kubelogin_extracted/`):** `kubectl-oidc_login.exe`, `kubelogin.zip`, `kubelogin_extracted/kubelogin.exe`, `kubelogin_extracted/LICENSE`, `kubelogin_extracted/README.md`.

*(Nota: los scripts temporales dentro de `globalmart-frontend/` se eliminan en la tarea siguiente `S3-025`; `new-ingresses.yaml`, `run-apis.*`, `GlobalMart_ContextMaster.md` y `globalmart-frontend/package-lock.json` se conservaron intactos.)*

## Reglas Agregadas en `.gitignore`

Se añadieron reglas explícitas al final de `.gitignore` para prevenir el reingreso de `*.exe`, `kubectl-oidc_login*`, `kubelogin*.zip`, `kubelogin_extracted/`, `/fix*.js`, `/test.js`, `/test*.json`, `/script.py`, `/*.sql`, `/generate_sql.ps1` y `/package-lock.json` (verificado con `git check-ignore --no-index -v` sin afectar `globalmart-frontend/package-lock.json`).

## Resultados de Verificación Post-Limpieza

- **`dotnet build GlobalMartOS.sln`:** 0 errores, exit code 0.
- **`dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --no-build`:** 60/60 aprobadas, exit code 0.
- **`npm run typecheck` (`globalmart-frontend`):** 0 errores, exit code 0.
- **`node --test tests/*.test.cjs` (`globalmart-frontend`):** 66/66 aprobadas, exit code 0.
- **`git check-ignore --no-index -v`:** todas las rutas de basura en raíz ignoradas; `globalmart-frontend/package-lock.json` no ignorado.
