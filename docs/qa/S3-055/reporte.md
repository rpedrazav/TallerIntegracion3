# S3-055 — Probar el primer `.exe` contra el clúster

Fecha: 2026-11-25. Sprint 3 (Semana 11). Meta: Entrega en clúster y `.exe`.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Instalar el ejecutable (`GlobalMartOS-Setup.exe`) y recorrer login, apertura de turno, venta, cobro y cierre contra el clúster real (`dev.censei.cl`).
- **Entrega verificable:** Registro de la prueba con defectos encontrados y evidencias visuales.
- **Aceptación:** El flujo de caja completo funciona desde el `.exe` instalado.

## Instalación Probada

- **Instalador ejecutado:** `globalmart-frontend\out\make\squirrel.windows\x64\GlobalMartOS-Setup.exe` (`--silent`, SHA-256 `B1986EC4E4E8A55FFF355FC373317F914869A26AA2FBC9C69A3998B963EBBCCA`).
- **Ruta instalada verificada:** `%LOCALAPPDATA%\GlobalMartOS\app-1.0.0\GlobalMartOS.exe` (empaquetado `resources/app.asar`, sin apertura de DevTools, modo **`remote`** por defecto).
- **Entorno destino:** clúster real `student-rpedraza` en `dev.censei.cl` con pods MS-1..MS-5 en tag `s2-20261125-40379da`. **Sin mocks ni interceptores.**

## Recorrido Completo Ejecutado desde `GlobalMartOS.exe`

| # | Paso en `.exe` instalado | Endpoint real invocado | HTTP | Evidencia |
|---|---|---|---:|---|
| 1 | Pantalla de login y autenticación (`cajero@demo.cl`) | `POST https://auth-rpedraza.dev.censei.cl/api/auth/login` | **200** | [01-exe-login.png](capturas/01-exe-login.png) |
| 2 | Verificación inicial de turno y apertura (`$ 20.000`) | `GET /api/turnos/activo` (404 esperado) → `POST https://pos-rpedraza.dev.censei.cl/api/turnos/abrir` | **200** | [02-exe-abrir-turno.png](capturas/02-exe-abrir-turno.png), [03-exe-pos-turno-abierto.png](capturas/03-exe-pos-turno-abierto.png) |
| 3 | Búsqueda de `leche` en MS-3 y creación de venta en MS-5 (`#801c40c3`) | `GET https://catalog-rpedraza.dev.censei.cl/api/products/search?q=leche&page=1&pageSize=15` (200) → `POST https://pos-rpedraza.dev.censei.cl/api/ventas` | **201** | [04-exe-venta-pendiente.png](capturas/04-exe-venta-pendiente.png) |
| 4 | Cobro en efectivo (`$ 3.000` por `$ 1.309`), vuelto MS-5 (`$ 1.691`) y comprobante seguro simulado | `POST https://pos-rpedraza.dev.censei.cl/api/ventas/801c40c3-dcc1-4769-96c6-58a3e4d54d70/cobrar` | **200** | [05-exe-cobro-comprobante.png](capturas/05-exe-cobro-comprobante.png) |
| 5 | Cuadre de caja y confirmación de cierre de turno (`fe0b8729-…`) | `POST https://pos-rpedraza.dev.censei.cl/api/turnos/cuadre` (200) → `POST https://pos-rpedraza.dev.censei.cl/api/turnos/cerrar` | **200** | [06-exe-cuadre.png](capturas/06-exe-cuadre.png), [07-exe-turno-cerrado.png](capturas/07-exe-turno-cerrado.png) |

**Resultado final:** flujo completo aprobado (`0` errores inesperados de red o rutas, `0` `/api` duplicados, `0` errores de consola JS).

## Defectos Encontrados Durante la Prueba del `.exe`

| # | Defecto encontrado | Causa raíz | Acción aplicada / Estado |
|---|---|---|---|
| **D1** | `POST /api/turnos/abrir` respondía **HTTP 503** (`No se pudo validar la sucursal.`). | `POSCartService` (`Program.cs:125`) lee `Services:IdentityUrl` (default `http://localhost:5124`) para validar la sucursal contra MS-1, pero el Deployment `poscartservice` y `k8s/manifests/all-services.yaml` no tenían `Services__IdentityUrl`. | **Corregido:** se agregó `Services__IdentityUrl=http://tenantidentityservice` en `k8s/manifests/all-services.yaml` y en el Deployment vivo del clúster. |
| **D2** | Tras corregir D1, `POST /api/turnos/abrir` respondía **HTTP 404** (`Sucursal no encontrada o inactiva.`). | El usuario `cajero@demo.cl` en `tenant_db` no tenía fila en `UsuarioSucursales`, por lo que su JWT no incluía `sucursal_id` y `AbrirTurnoPage` enviaba un UUID fallback inexistente. | **Corregido en clúster:** se asoció `cajero@demo.cl` (`bbbbbbbb-…-0001`) a `Sucursal Demo` (`11141053-16ed-4520-9077-d4c562ca0ddf`) en `UsuarioSucursales`. |
| **D3** | Al abrir `Cerrar Turno`, el botón inicial no exige completar denominaciones para confirmar el cierre (`monto declarado $0`). | Comportamiento actual de `CerrarTurnoPage` (permite cerrar con declaración `$0` cuando el usuario no incrementa denominaciones). | **Registrado** como observación menor de UX (no bloquea el flujo de caja). |
