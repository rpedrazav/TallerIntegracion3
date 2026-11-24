# Manual de Ejecución y Guía de Demo — Sprint 2 (S2-119 / TI3-750)

> **Propósito:** Permitir que cualquier integrante del equipo, docente o evaluador reproduzca de principio a fin la demostración funcional del **Sprint 2 de GlobalMart OS** en una máquina local **sin requerir balanza física ni terminal bancario POS real**, distinguiendo con transparencia qué está implementado con simuladores y qué requiere hardware homologado.

---

## 1. Prerrequisitos Verificados

| Herramienta | Versión Mínima | Comando de Comprobación |
|---|---|---|
| Docker Engine / Docker Desktop + Compose v2 | 24+ / Compose v2.20+ | `docker compose version` |
| .NET SDK | `8.0.x` | `dotnet --version` |
| Node.js + npm | `20.x`+ | `node --version && npm --version` |
| `curl` y `bash` (Git Bash / WSL en Windows) | Estándar | `curl --version` |

### Mapa de Puertos del Entorno Local
| Componente | Puerto Host | Rol en la Demo |
|---|---|---|
| **Kong API Gateway** | `:8000` (Admin `:8001`) | Punto único de entrada (`API_MODE=gateway`), valida JWT HS256 y enruta a MS-1..MS-5 y simulador |
| **Simulador de Pasarela de Pagos** | `:5050` | Mock determinista RN-02 (`sim-approved-v1`, `sim-declined-v1`, `sim-timeout-v1`) |
| **MS-1 Tenant & Identity** | `:5124` | Login JWT, CRUD usuarios, configuración de tenant (IVA) y sucursales |
| **MS-2 Tax & Compliance** | `:5239` | Cálculo autoritativo de IVA por línea y comprobantes correlativos |
| **MS-3 Catalog & Pricing** | `:5203` | Productos unitarios y pesables (`EsPesoVariable`), categorías, búsqueda `pg_trgm` y baja lógica |
| **MS-4 Warehouse & Inventory** | `:5205` | Consumidor Kafka `sale.completed` con descuento idempotente de stock |
| **MS-5 POS & Cart** | `:5000` | Apertura/cuadre/cierre de turnos, carrito autoritativo, cobro efectivo/tarjeta y outbox |
| **PostgreSQL 16 (x8) / Kafka / Kafdrop** | `:5433–5440` / `:9092` / `:9000` | Persistencia aislada por microservicio y bus de eventos asíncrono |

---

## 2. Arranque Reproducible Paso a Paso (Sin Dispositivos Físicos)

### Paso 1: Configurar archivos `.env` (opcional — valores por defecto listos)
```bash
# Desde la raíz del repositorio:
cp Docker/env.example Docker/.env
cp globalmart-frontend/.env.example globalmart-frontend/.env
```
> Nota: `Docker/docker-compose.yml` ya incluye valores por defecto (`admin` / `admin_password`) compatibles con `appsettings.json` incluso si no se crea `Docker/.env`.

### Paso 2: Levantar la infraestructura Docker y esperar estado `healthy`
```bash
docker compose -f Docker/docker-compose.yml up -d \
  postgres-tenant postgres-supply postgres-catalog \
  postgres-pos postgres-tax postgres-warehouse \
  postgres-analytics postgres-loyalty \
  zookeeper kafka kafka-init-topics kafdrop kong payment-simulator

# Comprobar que los contenedores figuren como 'healthy':
docker compose -f Docker/docker-compose.yml ps
```

### Paso 3: Compilar e iniciar los microservicios del núcleo (MS-1 a MS-5)
```bash
dotnet build GlobalMartOS.sln --configuration Release

# En Linux / macOS:
./run-apis.sh

# En Windows (CMD / PowerShell):
.\run-apis.bat
```
> Al iniciar en `Development`, cada microservicio aplica automáticamente sus migraciones EF Core (`db.Database.Migrate()`) y **MS-1** crea el seed de demostración si no existe.

### Paso 4: Ejecutar el Smoke Test automatizado de extremo a extremo
```bash
./scripts/smoke-test.sh
```
Resultado esperado: `[OK - 200]` en los 5 microservicios y el simulador, autenticación exitosa, catálogo `/api/products` HTTP 200 vía Kong, estado de turno POS HTTP 200/404 vía Kong, pago simulado `sim-approved-v1` aprobado y bloqueo RN-02 (`HTTP 400 CARD_DATA_FORBIDDEN`) ante intento de enviar PAN.

### Paso 5: Iniciar el Frontend de Escritorio (Electron + React)
```bash
cd globalmart-frontend
npm ci

# Opción recomendada A — Enrutado por Kong API Gateway (:8000):
API_MODE=gateway npm start

# Opción alternativa B — Directo a puertos locales (:5124, :5203, :5000):
API_MODE=local npm start
```

---

## 3. Configuración de Simuladores (Balanza, Terminal POS y Pasarela)

El sistema arranca **por defecto** en modo 100% simulado sin requerir drivers USB/COM ni hardware:

| Simulador | Variable de Configuración | Valor por Defecto | Cómo Controlar Escenarios en la Demo |
|---|---|---|---|
| **Balanza de Mostrador (Electron Main)** | `GLOBALMART_SCALE_ADAPTER` | `simulator` | En la columna izquierda de `/pos`, usar el widget **⚖️ Balanza Simulada** (`+0.25kg`, `+0.5kg`, `+1kg`, `+2.5kg` y `Tarar`). Al elegir un producto pesable, `window.api.scale.readStableWeight` entrega lecturas estables normalizadas a `kg` (mín. `0.010 kg`, precisión `0.001 kg`). |
| **Terminal POS Mostrador (Electron Main + Renderer)** | `GLOBALMART_TERMINAL_ADAPTER` | `mock` + `Ms5TerminalAdapter` | Sin hardware conectado: `PaymentModal` coordina el cobro por tarjeta con MS-5 mediante `TerminalPort` usando claves UUID de idempotencia y referencias opacas. |
| **Pasarela Bancaria Externa (Docker `:5050`)** | `PasarelaPago__BaseUrl` en MS-5 | `http://localhost:5050` | Escenarios deterministas enviados en `scenario_id`:<br/>• `sim-approved-v1` → Aprueba y devuelve `SIM-REF-...` (HTTP 200)<br/>• `sim-declined-v1` → Rechaza con `DO_NOT_HONOR` (HTTP 422 en MS-5)<br/>• `sim-timeout-v1` → Demora 35 s (> timeout 30 s de MS-5 → HTTP 504 e intento `PENDIENTE` reconciliable) |

Para ejecutar los smokes dedicados de simuladores sin abrir interfaz gráfica:
```bash
# Smoke de balanza y terminal simulados en Electron/Node:
./scripts/smoke-simuladores-frontend.sh

# Smoke de los 3 escenarios de tarjeta y correlación de logs sin PAN/CVV en MS-5:
./scripts/smoke-tarjeta.sh
```

---

## 4. Guion Paso a Paso para Reproducir la Demo de Sprint 2

### Credenciales Seed de Desarrollo
- **Tenant ID:** `aaaaaaaa-0000-0000-0000-000000000001`
- **Administrador:** `admin@demo.cl` / contraseña `demo1234` (rol `ADMIN`)
- **Cajero:** `cajero@demo.cl` / contraseña `demo1234` (rol `CAJERO`)

### Escena 1 — Administración, Usuarios, Configuración Fiscal y Catálogo (Meta G1)
1. Iniciar sesión en Electron con **`admin@demo.cl`** / `demo1234`.
2. Navegar a **Administración (`/admin`)**:
   - En **Usuarios**, crear un usuario nuevo, editar su correo (`PUT /users/{id}`) y probar la **Desactivación lógica** (`DELETE /users/{id}` → conserva fila como *Inactivo*, bloquea autodesactivación).
   - En **Configuración del Tenant**, verificar/actualizar el porcentaje de IVA (ej. `19%`, validado numéricamente con Zod) y guardar (`PUT /tenants/{id}/config`).
3. Navegar a **Productos (`/admin/productos`)**:
   - Crear una categoría si aún no existe y registrar **1 producto unitario** (ej. *Café Grano 250g*, código `7800000000011`, precio `$4.500`, `esPesoVariable = false`) y **1 producto pesable** (ej. *Manzana Fuji Granel*, código `7800000000028`, precio `$1.890`/kg, `esPesoVariable = true`).
   - Probar el botón **Desactivar** sobre un producto de prueba y verificar que permanece listado como **Inactivo** (baja lógica S2-011) sin poder agregarse en el POS.
4. *(Opcional RBAC)* Cerrar sesión, entrar con **`cajero@demo.cl`** e intentar ir a `/admin` o `/admin/productos`: verificar bloqueo visual **Acceso denegado** (TI3-675).

### Escena 2 — Apertura de Turno de Caja (RN-06)
1. Con sesión de `cajero@demo.cl` (o `admin@demo.cl`), entrar a **POS (`/pos`)**.
2. Si no existe turno abierto, el sistema redirige automáticamente a **`/abrir-turno`**.
3. Seleccionar un fondo inicial (ej. `$50.000`) y presionar **Abrir Turno** → redirige a `/pos` con banner verde de confirmación.

### Escena 3 — Carrito Autoritativo, Pesaje Simulado, Efectivo y Kafka Outbox (Metas G2 y G4)
1. En `/pos`, buscar o escanear el código del producto unitario (`7800000000011`) y agregarlo al borrador.
2. En el widget **⚖️ Balanza Simulada** (abajo a la izquierda), presionar `+0.5kg` y seleccionar el producto pesable (*Manzana Fuji Granel*): el POS captura el peso estable por IPC y agrega la línea en `kg` con 3 decimales.
3. Elegir método **Efectivo** y presionar **`INICIAR VENTA`**:
   - MS-5 resuelve precios desde MS-3 y calcula el IVA real del tenant vía MS-2; el banner confirma *"Venta pendiente #... Los ítems y totales provienen de MS-5"*.
4. Presionar **`COBRAR`**, ingresar un monto menor al total para demostrar la validación `Monto insuficiente` (400 con diferencia exacta), y luego ingresar un monto suficiente (ej. `$10.000`) y confirmar:
   - El modal muestra **Venta aprobada** y el **vuelto exacto** calculado por MS-5.
   - En segundo plano, `OutboxPublisherWorker` publica `sale.completed` en Kafka (visible en Kafdrop `http://localhost:9000` → topic `sale.completed`) y MS-4 descuenta el stock de forma idempotente.

### Escena 4 — Cobro con Tarjeta Simulada e Idempotencia RN-02 (Metas G3 y G5)
1. Agregar un producto nuevo al carrito, seleccionar método **Tarjeta simulada** y presionar **`INICIAR VENTA`**.
2. Presionar **`COBRAR`** y confirmar el pago con tarjeta:
   - MS-5 reserva `IntentoPago`, invoca al simulador (`:5050`) con `Idempotency-Key` **sin transmitir PAN ni CVV**, persiste el `Pago` de tipo `TARJETA` con la referencia opaca (`SIM-REF-...`) y aprueba la venta.
3. *(Verificación de rechazo y timeout por API/CLI)* Ejecutar `./scripts/smoke-tarjeta.sh` para mostrar en consola los casos `sim-declined-v1` (HTTP 422, venta sigue `PENDIENTE`, 0 pagos, 0 eventos outbox) y `sim-timeout-v1` (HTTP 504, reserva permanece `PENDIENTE` impidiendo doble cargo y permitiendo reconciliación con `GET /ventas/{id}/intento-tarjeta`), más la auditoría regex `[COBRO_CORRELATION]` con **0 PANs / 0 CVVs**.

### Escena 5 — Arqueo de Denominaciones y Cierre de Turno (`/cerrar-turno`)
1. Desde `/pos`, presionar **`💼 Cerrar Turno / Cuadre de Caja`**.
2. Observar el **Efectivo Esperado (Ventas)** cargado desde MS-5 (`POST /api/turnos/cuadre`).
3. Ingresar billetes y monedas en la tabla interactiva:
   - Primero dejar una discrepancia para mostrar la tarjeta de diferencia en **rojo** (*Faltante* o *Sobrante*).
   - Ajustar las denominaciones para igualar el monto esperado y presionar **`🔍 Calcular Cuadre`**: la diferencia cambia a **verde (`$0 — Cuadre Exacto`)**.
4. Presionar **`🔒 Confirmar y Cerrar Turno`** → el turno pasa a estado `CERRADO` en PostgreSQL.

---

## 5. Límites Físicos Explícitos y Condición para Hardware Real

> [!WARNING]
> **Transparencia de Alcance del Sprint 2:**  
> Esta demostración **NO certifica compatibilidad con balanzas físicas RS-232/USB ni con terminales bancarios EMV/NFC reales**.

| Componente | Lo que SÍ está implementado y demostrado en Sprint 2 | Lo que permanece bloqueado hasta contar con hardware físico |
|---|---|---|
| **Balanza (G4)** | Contrato `ScaleAdapter`, validación de gramos/kilos S2-061 (`>= 10 g`, precisión `1 g`), `SimulatedScaleAdapter`, esqueleto `SerialScaleAdapter` con transporte/parser inyectables, whitelist IPC cerrada (`scale:*`) y captura en `Pos.tsx`. | Fabricante, modelo, cableado COM/USB, gramática real de trama ASCII, compilación nativa de `serialport` para Electron y pruebas con masas patrón físicas. |
| **Terminal POS y Pasarela (G3 / G5)** | Contrato `TerminalPort`, máquina de estados pura, `MockTerminalAdapter`, `RealTerminalAdapter` bloqueado por defecto, pasarela simulada Node (`:5050`), idempotencia durable en `IntentosPago`, reconciliación de timeouts y cumplimiento estricto RN-02 (cero datos de tarjeta). | PINPad físico certificado por adquirente (Transbank/Getnet/etc.), protocolo local o API Cloud-to-Cloud homologada, llaves de integración y tarjetas EMV de laboratorio. |

Para conocer el checklist exacto de insumos técnicos que deben aportarse antes de activar hardware real, consultar [`docs/context/integraciones/guia-sustitucion-hardware-real.md`](../context/integraciones/guia-sustitucion-hardware-real.md).

---

## 6. Solución Rápida de Problemas

- **Puerto ocupado al correr `./run-apis.sh`:** ejecutar `pkill -f 'dotnet run --no-launch-profile'` y verificar con `ss -tlnp` (Linux) o `netstat -ano` (Windows).
- **Kong responde `502 Bad Gateway` en `API_MODE=gateway`:** verificar que `./run-apis.sh` esté corriendo en el host (los MS deben escuchar en `0.0.0.0`, ya configurado en `run-apis.sh` y `run-apis.bat`) y que el contenedor `kong` tenga `extra_hosts: host.docker.internal:host-gateway`.
- **Limpiar y reiniciar desde cero:** `docker compose -f Docker/docker-compose.yml down` detiene contenedores conservando datos. *(Evitar `down -v` salvo que desee borrar explícitamente todas las bases de datos locales).*
