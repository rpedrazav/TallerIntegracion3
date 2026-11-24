# S3-004 / TI3-755 — Inventariar archivos basura del repositorio

Fecha: 2026-11-17. Sprint 3 (Semana 9). Meta: Frontend simple y agradable.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Listar archivos sueltos sin uso: `fix*.js`, `test*.json`, `script.py`, `query.sql`, `kubelogin`, `tbody.txt` y similares.
- **Entrega verificable:** Lista de archivos a eliminar con confirmación de que ningún script o CI los referencia.
- **Aceptación:** Ningún archivo de la lista aparece referenciado en código, CI ni documentación vigente.
- **Prioridad MoSCoW:** Must. Dificultad: Baja.
- **Hallazgo o necesidad:** La raíz del repositorio acumula scripts y binarios temporales.

**Alcance de esta entrega (Semana 9):** inventario estático y verificación de referencias. **No se eliminó ningún archivo** en esta tarea; la eliminación efectiva y el ajuste de `.gitignore` corresponden a **TI3-775 (Semana 10 — Eliminar archivos basura de la raíz)**.

---

## 1. Lista confirmada de archivos basura a eliminar (30 archivos versionados, ~46,65 MiB)

Ninguno de los 30 archivos listados a continuación aparece referenciado en código productivo (`src/`, `globalmart-frontend/src/`), pruebas (`tests/`, `globalmart-frontend/e2e/`, `globalmart-frontend/tests/`), CI (`.github/workflows/`), `Docker/`, `k8s/`, `scripts/` ni documentación vigente (`docs/`, `DOCKER.md`, `AGENTS.md`). El registro reproducible de búsqueda está en [`referencias.txt`](referencias.txt).

### 1.1 Scripts temporales de parcheo en la raíz (6 archivos)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 1 | `fix.js` | 804 B | Script puntual de reemplazo de texto sobre `ProductosPage.tsx` | 0 |
| 2 | `fix2.js` | 693 B | Script puntual de reemplazo temporal | 0 |
| 3 | `fix3.js` | 577 B | Script puntual de reemplazo temporal | 0 |
| 4 | `fix4.js` | 935 B | Script puntual de reemplazo temporal | 0 |
| 5 | `fix5.js` | 978 B | Script puntual de reemplazo temporal | 0 |
| 6 | `fix_btn.js` | 575 B | Script puntual de ajuste temporal de botón | 0 |

### 1.2 Scripts y payloads de prueba manual en la raíz (6 archivos)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 7 | `test.js` | 585 B | Llamada puntual `axios.post` a `api-rpedraza.dev.censei.cl` con `rejectUnauthorized: false` | 0 *(los aciertos de `reversals.test.js` y `build-test.js` son archivos distintos)* |
| 8 | `test.json` | 126 B | Payload JSON temporal de login (`password`) | 0 |
| 9 | `test2.json` | 126 B | Payload JSON temporal duplicado | 0 |
| 10 | `test_abrir.json` | 38 B | Payload JSON temporal (`montoInicialCaja`) | 0 |
| 11 | `test_auth.json` | 126 B | Payload JSON temporal de login (`demo1234`) | 0 |
| 12 | `test_bad.json` | 129 B | Payload JSON temporal de prueba negativa (`badpassword`) | 0 |

### 1.3 Scripts y dumps SQL sueltos en la raíz (4 archivos)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 13 | `script.py` | 2.907 B | Script puntual con ruta local hardcodeada (`C:/Users/kkk/Desktop/TallerIntegracion3/...`) para insertar comentarios XML | 0 |
| 14 | `query.sql` | 34 B | Consulta ad-hoc `SELECT * FROM "Tenants" LIMIT 1;` | 0 |
| 15 | `generate_sql.ps1` | 1.380 B | Generador ad-hoc que produce `insert_productos.sql` | 0 |
| 16 | `insert_productos.sql` | 71.311 B | Dump SQL generado únicamente por `generate_sql.ps1` | 0 *(solo citado por `generate_sql.ps1`, también en esta lista)* |

### 1.4 Lockfile vacío en la raíz (1 archivo)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 17 | `package-lock.json` | 103 B | Lockfile vacío (`"packages": {}`) sin `package.json` en la raíz | 0 *(CI y `DOCKER.md` usan exclusivamente `globalmart-frontend/package-lock.json`)* |

### 1.5 Binarios y comprimidos de `kubelogin` versionados (5 archivos, ~46,54 MiB)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 18 | `kubectl-oidc_login.exe` | 19.376.128 B | Binario Windows de cliente `kubectl oidc-login` versionado por error | 0 en código/CI/docs *(el manual indica instalar el plugin localmente)* |
| 19 | `kubelogin.zip` | 10.034.213 B | Archivo comprimido de instalación de `kubelogin` | 0 |
| 20 | `kubelogin_extracted/kubelogin.exe` | 19.376.128 B | Duplicado exacto de `kubectl-oidc_login.exe` | 0 |
| 21 | `kubelogin_extracted/LICENSE` | 11.545 B | Licencia del paquete descomprimido | 0 |
| 22 | `kubelogin_extracted/README.md` | 4.992 B | README del paquete descomprimido | 0 |

### 1.6 Scripts temporales y fragmentos sueltos en `globalmart-frontend/` (8 archivos)

| # | Ruta | Tamaño | Motivo / Descripción | Referencias en código, CI o docs |
|---|---|---:|---|---|
| 23 | `globalmart-frontend/fix_pos.js` | 3.283 B | Parche manual temporal sobre POS | 0 |
| 24 | `globalmart-frontend/fix_tbody.js` | 10.005 B | Parche manual temporal sobre tablas | 0 |
| 25 | `globalmart-frontend/fix_td.js` | 3.591 B | Parche manual temporal sobre celdas | 0 |
| 26 | `globalmart-frontend/run_fix.js` | 320 B | Script puntual que reemplaza `<tbody>` en `ProductosPage.tsx` leyendo `tbody.txt` | 0 |
| 27 | `globalmart-frontend/tbody.txt` | 9.722 B | Fragmento HTML temporal leído únicamente por `run_fix.js` | 0 *(solo citado por `run_fix.js`, también en esta lista)* |
| 28 | `globalmart-frontend/poblar_bd.js` | 3.624 B | Script puntual remoto con `NODE_TLS_REJECT_UNAUTHORIZED = '0'` y lectura de `%APPDATA%` | 0 |
| 29 | `globalmart-frontend/test.js` | 585 B | Duplicado de `test.js` de la raíz | 0 |
| 30 | `globalmart-frontend/test_insert.js` | 1.084 B | Prueba puntual remota leyendo `config.json` desde `%APPDATA%` | 0 |

**Total confirmado a eliminar:** **30 archivos versionados** (`48.916.647` bytes ≈ **46,65 MiB**).

---

## 2. Archivos operativos sueltos sujetos a decisión DevOps (revisar en S3-007 / TI3-775)

No se mezclan en la lista automática de basura porque corresponden a operaciones manuales de clúster, aunque ninguno está referenciado por CI ni código productivo:

| Ruta | Tamaño | Observación | Recomendación |
|---|---:|---|---|
| `Patch-Envs.ps1` | 985 B | Contiene ruta personal hardcodeada (`C:\Users\kkk\Downloads\estudiantes-rpedraza.kubeconfig`) y fuerza `ASPNETCORE_ENVIRONMENT=Development` en el clúster. | **Eliminar** en TI3-775 (sin referencias y con ruta personal). |
| `Build-Images.ps1` | 509 B | Script manual reemplazado por `.github/workflows/docker-build.yml`. | Confirmar con DevOps si se elimina o mueve a `scripts/`. |
| `Push-Images.ps1` | 1.059 B | Script manual reemplazado por `.github/workflows/docker-build.yml`; menciona `.\Deploy-To-K8s.ps1`. | Confirmar con DevOps si se elimina o mueve a `scripts/`. |
| `Deploy-To-K8s.ps1` | 1.138 B | Script manual de `kubectl apply -f .\k8s\manifests\all-services.yaml` con ruta personal a `estudiantes-rpedraza.kubeconfig`. | Confirmar con DevOps si se elimina o mueve a `scripts/`. |
| `sa-secret.yaml` | 217 B | Manifiesto de token de ServiceAccount `github-actions` en `student-rpedraza`, sin referencias en CI ni `k8s/`. | Confirmar en S3-007 si se traslada a `k8s/` o se elimina. |

---

## 3. Archivos en la raíz que DEBEN conservarse (referenciados activamente)

- `new-ingresses.yaml`: referenciado en `docs/context/estado-actual.md` y `docs/qa/S3-001/reporte.md`.
- `run-apis.bat` y `run-apis.sh`: referenciados en `globalmart-frontend/e2e/electron/README.md`, `globalmart-frontend/src/config/api.ts`, `docs/qa/S2-112/reporte.md`, `docs/qa/S2-113/reporte.md` y `docs/qa/S3-005/reporte.md`.
- `GlobalMart_ContextMaster.md`: fuente referenciada en más de 20 nodos de `docs/context/` y `globalmart-frontend/src/types/auth.types.ts`.
- `GlobalMartOS.sln`, `Directory.Build.props`, `Directory.Build.targets`, `dotnet-tools.json`, `.env.example`, `.gitattributes`, `.gitignore`, `DOCKER.md`, `AGENTS.md`, `CLAUDE.md`, `SKILLS.md`.
- `globalmart-frontend/package-lock.json`: requerido por `.github/workflows/ci.yml` y `.github/workflows/electron-e2e.yml`.

---

## 4. Logs locales ignorados y recomendaciones para `.gitignore` (insumo para TI3-775)

- Se detectaron **28 archivos `*-integration.log`** en la raíz local del workspace; ya están ignorados por `*.log` en `.gitignore` (no están versionados en Git), pero conviene limpiarlos localmente.
- Para evitar que los archivos inventariados vuelvan al repositorio en **TI3-775 (Semana 10)**, se recomienda añadir en `.gitignore`:
  ```gitignore
  # Binarios y utilidades locales de Kubernetes (S3-004 / TI3-775)
  *.exe
  kubectl-oidc_login*
  kubelogin*.zip
  kubelogin_extracted/

  # Scripts y volcados temporales sueltos
  /fix*.js
  /test.js
  /test*.json
  /script.py
  /*.sql
  /package-lock.json
  globalmart-frontend/fix_*.js
  globalmart-frontend/run_fix.js
  globalmart-frontend/tbody.txt
  globalmart-frontend/test.js
  globalmart-frontend/test_insert.js
  ```
  *(Nota: no se recomienda agregar `*.zip` global para no afectar reportes/trazas comprimidas ya versionadas en `docs/qa/S2-025/`.)*
