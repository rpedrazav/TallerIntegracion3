# S2-110 / TI3-741 — Guion de Demo Completa de Mostrador

Fecha: 2026-11-17. Sprint 2 — GlobalMart OS.

## 1. Propósito y Alcance Declarado

Demostrar de extremo a extremo la operación de mostrador cubriendo **las cinco metas del Sprint 2** con APIs y simuladores declarados, **sin alertas placeholder (`window.alert`) ni captura de datos sensibles de tarjeta**:

| Meta | Alcance en la demo | APIs / Simuladores declarados | Modo de operación |
|---|---|---|---|
| **G1 — Administración** | Acceso por rol (`ADMIN` habilitado, `CAJERO` bloqueado), gestión de usuarios, productos y configuración de tenant. | MS-1 (`IdentityTenantService`), MS-3 (`CatalogService`) | APIs reales / contratos verificados |
| **G2 — Carrito y venta (Efectivo)** | Turno activo, búsqueda/escaneo de productos, inicio de venta en MS-5, cobro en efectivo con vuelto autoritativo y comprobante simulado. | MS-3 (`CatalogService`), MS-2 (`TaxService`), MS-5 (`POSCartService`) | APIs reales / contratos verificados |
| **G3 — Cobro con tarjeta simulado** | Autorización idempotente (RN-02), referencia opaca, manejo de rechazo con cambio a efectivo y bloqueo de doble cargo en timeout. | MS-5 (`POST /ventas/{id}/cobrar-tarjeta`, `GET /ventas/{id}/intento-tarjeta`) + Pasarela simulada | **Simulado** (sin cargo bancario real) |
| **G4 — Balanza (Producto pesable)** | Lectura de peso estable desde balanza simulada (`window.api.scale`) y respaldo manual, línea única con cálculo de subtotal e IVA en MS-5. | `SimulatedScaleAdapter` (IPC `devices:scale:*`) + MS-5 (`POST /ventas` con `peso_kg`) | **Simulado** (hardware físico condicionado — R5) |
| **G5 — Terminal POS con simulador** | Pantalla de terminal POS (S2-106), sincronización `TerminalPort` + MS-5 con clave de idempotencia única (S2-107), comprobante seguro rotulado como simulado sin PAN ni folio DTE (S2-108) y recuperación ante timeout/recarga (S2-109). | `SimulatedTerminalAdapter` (IPC `devices:terminal:*`), `posRecovery`, `ComprobantePago`, MS-5 | **Simulado** (hardware físico condicionado — R5) |

---

## 2. Preparación Previa (Checklist)

1. Confirmar que los servicios de entorno estén activos o que se ejecute la demo automatizada con simuladores declarados:
   - Servicios locales: MS-1 (`:5001`), MS-2 (`:5239`), MS-3 (`:5003`), MS-4 (`:5004`), MS-5 (`:5005`).
   - Simuladores configurados en `globalmart-frontend/src/config/devices.config.ts`:
     - `scaleMode: 'mock'` (`SimulatedScaleAdapter`).
     - `terminalMode: 'mock'` (`SimulatedTerminalAdapter`).
2. Verificar ausencia de errores de tipos y suites en verde antes de presentar:
   ```powershell
   cd globalmart-frontend
   npm run typecheck
   node --test tests/*.test.cjs
   npx playwright test e2e/demo-mostrador.spec.ts
   ```
3. Credenciales de demo:
   - Administrador: `admin@demo.cl` (rol activo `ADMIN`).
   - Cajero: `cajero@demo.cl` (rol activo `CAJERO`).

---

## 3. Guion Paso a Paso (5 Escenas — ~8 minutos)

### Escena 1 — Meta G1: Administración y Control de Acceso (1 min)
1. Iniciar sesión con `admin@demo.cl` (`active_role: ADMIN`).
2. Mostrar acceso a **Administración**: listado de usuarios, edición/desactivación con confirmación controlada, catálogo de productos y configuración de IVA/moneda del tenant.
3. Cerrar sesión e iniciar sesión con `cajero@demo.cl` (`active_role: CAJERO`).
4. Mostrar que la ruta `#/admin` queda bloqueada para `CAJERO` y el operador ingresa directamente al flujo de caja/POS.
5. **Punto clave:** Sin alertas `window.alert` placeholder; todos los errores y confirmaciones usan modales y banners nativos de la UI.

### Escena 2 — Meta G2: Carrito, Venta y Cobro en Efectivo con Comprobante (2 min)
1. Verificar turno activo (o abrir turno indicando fondo inicial).
2. Buscar un producto unitario en el buscador conectado a MS-3 (ej. `Leche Entera 1L`, `$ 1.000`) y agregarlo al carrito.
3. Presionar **INICIAR VENTA** (`POST /ventas` en MS-5). Destacar que subtotal (`$ 1.000`), IVA (`$ 190`) y total (`$ 1.190`) provienen autoritativamente de MS-5 y aparece el identificador `Venta pendiente #11111111`.
4. Presionar **COBRAR**, ingresar monto recibido (`$ 2.000`) y confirmar **Confirmar efectivo**.
5. Mostrar el resultado aprobado:
   - `Vuelto calculado por MS-5: $ 810`.
   - **Comprobante de pago seguro** rotulado como `*** COMPROBANTE DE PAGO SIMULADO ***`, `DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE` y `Folio DTE: No emitido (comprobante simulado)`.
   - Montos coincidentes con `GET /ventas/{id}`.
6. Presionar **Cerrar cobro** y comprobar que el carrito queda limpio y listo para la siguiente venta.
7. **Evidencia:** [01-demo-efectivo-comprobante.png](capturas/01-demo-efectivo-comprobante.png).

### Escena 3 — Meta G4: Producto Pesable con Balanza Simulada (1,5 min)
1. Seleccionar método de pago **Tarjeta simulada** en el resumen de venta.
2. Buscar un producto de peso variable (`Manzana a granel`, `$ 2.000/kg`, etiqueta `Requiere peso estable`) y seleccionarlo.
3. Mostrar el panel de captura de **Balanza Simulada**: lectura estable `0.750 kg` entregada por `window.api.scale.readStableWeight` (mencionar también la opción de entrada manual controlada cuando la balanza reporta desconexión o inestabilidad).
4. Presionar **Confirmar peso** y luego **INICIAR VENTA**.
5. Mostrar la línea única en el carrito (`0.750 kg`) con subtotal (`$ 1.500`), impuestos (`$ 285`) y total (`$ 1.785`) calculados en MS-5.
6. **Evidencia:** [02-demo-producto-pesable.png](capturas/02-demo-producto-pesable.png).

### Escena 4 — Metas G3 y G5: Cobro con Tarjeta por Terminal POS Simulado (2 min)
1. Presionar **COBRAR** para abrir el panel de **Terminal POS (Simulador)** (S2-106).
2. Señalar los elementos obligatorios:
   - Indicador de conexión: `Estado de conexión: Conectado`.
   - Monto autoritativo desde MS-5 (`$ 1.785`).
   - Etapas de lectura y autorización en pasarela simulada.
   - Aviso de seguridad PCI: *no se solicita número de tarjeta, PAN ni CVV*.
3. **Evidencia:** [03-demo-terminal-pos.png](capturas/03-demo-terminal-pos.png).
4. Presionar **Confirmar tarjeta**: el POS genera una única clave de idempotencia UUID y la envía tanto a `TerminalPort.iniciarCobro` (vía preload IPC) como a `POST /ventas/{id}/cobrar-tarjeta` en MS-5 (S2-107).

### Escena 5 — Meta G5: Timeout, Recarga POS sin Doble Cargo y Comprobante Seguro (1,5 min)
1. Demostrar el escenario de timeout/respuesta incierta (504 desde pasarela simulada):
   - El modal transiciona a **Resultado incierto**, conserva el identificador `#66666666` y la clave de intento correlacionada, y bloquea reautorizaciones a ciegas.
   - **Evidencia:** [04-demo-recuperacion-timeout.png](capturas/04-demo-recuperacion-timeout.png).
2. Ejecutar una **recarga completa del POS (`F5` / reload)** mientras MS-5 ya confirmó la transacción en servidor:
   - El POS ejecuta `recoverPosStateOnReload`, consulta `GET /ventas/{id}` y `GET /ventas/{id}/intento-tarjeta` **sin volver a invocar `POST /cobrar-tarjeta`** (`cardChargeCalls === 1`).
   - Se recupera la venta `#66666666` en estado **Cobro aprobado** mostrando:
     - `Referencia opaca: gw-demo-s2110` (sin PAN ni datos sensibles).
     - **Comprobante de pago seguro** rotulado como simulado con `Subtotal $ 1.500`, `Impuestos $ 285`, `Total $ 1.785` y `Folio DTE: No emitido (comprobante simulado)`.
   - **Evidencia:** [05-demo-tarjeta-comprobante.png](capturas/05-demo-tarjeta-comprobante.png).

---

## 4. Plan de Contingencia durante la Demo

- Si algún servicio local no responde durante la presentación en vivo, ejecutar la demo reproducible de extremo a extremo con Playwright (`npx playwright test e2e/demo-mostrador.spec.ts --headed`) o mostrar las capturas y resultados registrados en [reporte.md](reporte.md).
- Recordar explícitamente ante preguntas de evaluación que **G4 (Balanza)** y **G5 (Terminal POS)** se acreditan en **modo simulado** (regla R5 de S2-111); la certificación física permanece condicionada a contar con hardware, modelo y protocolo del dispositivo.
