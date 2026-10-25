---
id: limites-terminal-simulado
tipo: integracion
titulo: Límites del Terminal POS Simulado y Matriz de Contrato (S2-086)
estado: implementado
fuentes: [globalmart-frontend/src/devices/, globalmart-frontend/src/preload.ts]
verificado_contra_codigo: true
ultima_revision: 2026-10-25
depende_de: [hardware, pasarela-pago, electron-ipc]
publica: []
consume: []
reglas: [RN-02]
---

# Límites del Terminal POS Simulado (S2-086 / TI3-717)

> Define las operaciones, responsabilidades y fronteras del Terminal POS simulado desacoplado de la pasarela de pagos, estableciendo la matriz de contrato, dependencias y evidencia pendiente.

---

## 1. Desacoplamiento de Responsabilidades

| Capa | Componente | Responsabilidad | Lo que NO hace |
|---|---|---|---|
| **Terminal POS (Hardware/Mock)** | `MockTerminalAdapter` (Electron Main) | Interacción directa con el cliente en mostrador: mostrar monto, capturar gesto de tarjeta (chip/nfc), simular lectura de tarjeta y emitir token opaco. | **NUNCA** autoriza transacciones financieras bancarias directamente ni se conecta a redes de adquirencia sin pasar por MS-5. |
| **Pasarela de Pagos (Backend)** | `PasarelaPagoClient` (MS-5) | Orquestar autorización bancaria, validación de cupo, generación de RRN y código de autorización financiero bajo cumplimiento **RN-02**. | No interactúa con pantallas locales ni teclados del punto de venta físico. |

---

## 2. Matriz de Contrato del Terminal Simulado

| Operación | Método | Parámetros | Retorno | Estados Posibles |
|---|---|---|---|---|
| **Inicio** | `iniciar()` | `{ monto: number, moneda?: string, correlationId?: string }` | `OperacionTerminalResult` | `IDLE` → `ESPERANDO_TARJETA` |
| **Estado** | `obtenerEstado()` | Ninguno | `EstadoTerminalResult` | Retorna estado actual y datos de operación activa |
| **Cancelación** | `cancelar()` | Ninguno | `OperacionTerminalResult` | `ESPERANDO_TARJETA` / `PROCESANDO` → `CANCELADO` → `IDLE` |
| **Autorización / Simulación** | `autorizar()` | `simulacion?: 'APROBAR' \| 'RECHAZAR' \| 'TIMEOUT' \| 'ERROR_LECTURA'` | `AutorizacionTerminalResult` | `PROCESANDO` → `APROBADO` (con token opaco) o `RECHAZADO` / `ERROR` |

---

## 3. Declaración de No Compatibilidad con Marcas No Probadas

> [!IMPORTANT]
> **Criterio de Aceptación Cumplido:**  
> **GlobalMart OS NO declara compatibilidad con marcas ni modelos comerciales de terminales físicos no homologados** (tales como Ingenico Telium/Tetra, Verifone Engage, Pax, Transbank POS Integrado físico o similares) hasta que se cuente con hardware provisto por los adquirentes y se complete la certificación formal de laboratorio.
>
> El adaptador `RealTerminalAdapter` bloquea activamente cualquier ejecución de rutas desconocidas y previene fallos por ausencia de controladores en entornos sin periféricos.

---

## 4. Dependencias y Evidencia Pendiente

| Elemento | Estado | Dependencia Requerida | Evidencia Pendiente |
|---|---|---|---|
| Simulador Mock en Memoria | ✅ Implementado | Ninguna (Software puro) | `npm run smoke:simulators` ejecutado con éxito. |
| Adaptador Hardware Real | ⚠️ Reservado | Homologación con fabricante y SDK oficial | Certificación de laboratorio con terminal físico real. |
| Integración IPC Segura | ✅ Implementado | Electron Preload + contextIsolation | Handlers validados contra inyección de tipos. |
| Desacoplamiento Pasarela | ✅ Implementado | MS-5 `/ventas/{id}/cobrar-tarjeta` | Endpoint y tests de correlación ejecutados. |
