# S2-085 — Protocolo de prueba física de balanza

Fecha de preparación: 2026-10-24.

**Estado de la tarea física: CONDICIONADA — pendiente de modelo real de balanza.** Este documento entrega el protocolo y el checklist ejecutable. No acredita lectura física, integración Electron ni aceptación E2E: todo resultado de hardware queda [NO VERIFICADO] hasta ejecutar el checklist con el equipo.

| Campo | Valor |
|---|---|
| ID del plan | S2-085 |
| MoSCoW / dificultad | Should / Alta |
| Meta | Balanza con simulador |
| Condición de desbloqueo | Equipo físico disponible + manual del fabricante (protocolo de comunicación, Max, Min, división `e`) |
| Contrato de referencia | [peso-variable.md](../../context/dominio/peso-variable.md) (S2-061): kg canónico, mínimo 0.010 kg, precisión 0.001 kg, unidad explícita |
| Herramienta incluida | [capturar-balanza.ps1](capturar-balanza.ps1) |

## 1. Estado verificado del repositorio (2026-10-24)

- No existe dependencia `serialport` en [package.json](../../../globalmart-frontend/package.json) ni handlers IPC `balanza:*` en el frontend. El diseño IPC de [hardware.md](../../context/integraciones/hardware.md) sigue PLANIFICADO.
- El simulador/adaptador de balanza con conversión `g → kg` está PENDIENTE (S2-061 §7). Por eso el protocolo define también cómo reutilizar las tramas físicas como fixtures del simulador (§6).
- MS-5 sí valida peso al agregar ítem (S2-062) y calcula el subtotal en servidor (S2-063). Esos pasos (P8) se pueden ejecutar con el backend actual una vez que exista un peso leído.

## 2. Equipo y preparación

| ID | Requisito | Criterio | ☐ |
|---|---|---|---|
| E1 | Balanza física con salida serie RS-232 o USB-CDC/serie | Modelo, n.º de serie y firmware anotados | ☐ |
| E2 | Manual del fabricante con formato de trama y comandos | Copia en `evidencia/manual/` o referencia exacta | ☐ |
| E3 | Cable y, si aplica, adaptador USB-serie (FTDI/CH340/CP210x/PL2303) | Modelo del adaptador y driver anotados | ☐ |
| E4 | Masas patrón (ideal: certificadas). Mínimo: 10 g, 20 g, 100 g, 500 g, 1 kg, 2 kg y una cercana a Max | Valor nominal y clase/certificado anotados; si no hay certificado, marcar resultados de exactitud como [NO VERIFICADO — sin patrón] | ☐ |
| E5 | PC Windows con el POS (Electron) y backend local según [como-ejecutar.md](../../context/proyecto/como-ejecutar.md) | Versión de Windows y commit del repo anotados | ☐ |
| E6 | Superficie nivelada, sin corrientes de aire ni vibración | Burbuja de nivel centrada (foto) | ☐ |
| E7 | Balanza encendida ≥ 15 min antes de medir (calentamiento) [valor propuesto; usar el del manual si difiere] | Hora de encendido anotada | ☐ |

Datos de placa a registrar (foto obligatoria): **Max**, **Min**, **e** (división de verificación), **d** (división real), clase de exactitud, rangos múltiples si existen.

> [!WARNING]
> El contrato S2-061 fija mínimo 10 g y precisión 1 g como decisión de simulador. Si la balanza real tiene `Min` > 10 g o `e` > 1 g, **no se modifica el contrato durante la prueba**: se registra como hallazgo para decidir después (ver criterio P5-4).

## 3. Checklist ejecutable

Cada paso tiene un criterio medible. Resultado: `APROBADO`, `FALLIDO` o `NO APLICA` (con justificación). Guardar toda evidencia en `docs/qa/S2-085/evidencia/` con el nombre indicado.

### P1 — Identificación de puerto

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P1-1 | Con la balanza **desconectada**, ejecutar `powershell -ExecutionPolicy Bypass -File docs/qa/S2-085/capturar-balanza.ps1 -Listar` | Salida guardada como línea base | `P1-1-sin-balanza.txt` |
| P1-2 | Conectar balanza y repetir el listado | Aparece exactamente **un** puerto COM nuevo respecto de P1-1 | `P1-2-con-balanza.txt` |
| P1-3 | Abrir Administrador de dispositivos → Puertos (COM y LPT) | Nombre, COMx, VID/PID (Propiedades → Detalles → Id. de hardware) y driver coinciden con P1-2 | `P1-3-devmgmt.png` |
| P1-4 | Desconectar y reconectar 3 veces (mismo puerto USB) | El número COM es el mismo en las 3 reconexiones | `P1-4-reconexion.txt` |
| P1-5 | Reconectar en otro puerto USB físico | Anotar si cambia el COM. Si cambia, la integración debe identificar por VID/PID y no por COM fijo (hallazgo) | `P1-5-otro-usb.txt` |

### P2 — Parámetros de comunicación

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P2-1 | Tomar baudios, paridad, bits de datos y parada del manual (o menú de la balanza) | Parámetros anotados con su fuente | Tabla en el registro |
| P2-2 | Capturar 10 s sin enviar comando: `... -Puerto COMx -Baudios <b> -Paridad <p> -BitsDatos <d> -BitsParada <s> -Segundos 10 -Salida docs/qa/S2-085/evidencia/P2-2-pasivo.csv` | Determina el modo: **continuo** (RX sin TX) o **bajo demanda** (0 bytes, código de salida 3) | `P2-2-pasivo.csv` |
| P2-3 | Si no hay datos ni con comando (P3) y el manual no es concluyente: probar 9600 8N1, 9600 7E1, 4800 8N1, 2400 7E1 [combinaciones habituales; NO VERIFICADO para el modelo] | Una única combinación produce tramas ASCII legibles y repetibles; las otras, basura o nada | `P2-3-<config>.csv` |

### P3 — Comandos y formato de trama

Los comandos dependen del fabricante. Candidatos frecuentes a probar **solo si el manual no los especifica** [NO VERIFICADO]: `W\r`, `P\r`, `S\r\n`, ENQ `0x05`, `SI\r\n` (MT-SICS). Para tara/cero: `T\r`, `Z\r` u otros del manual.

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P3-1 | Enviar comando de lectura con 1 kg sobre el plato: `... -Comando "W\r"` o `-ComandoHex 05` | Respuesta en ≤ 500 ms tras TX [objetivo propuesto] (columna `ms_desde_inicio`) | `P3-1-lectura.csv` |
| P3-2 | Documentar la trama: encabezado, signo, valor, separador decimal, unidad, indicador de estabilidad (ST/US, `?`, bit de estado), terminador y checksum si existe | 5 tramas consecutivas se interpretan sin ambigüedad con la descripción | Sección «Formato de trama» del registro |
| P3-3 | Verificar que la trama trae **unidad explícita** (`g`/`kg`) | Si no trae unidad: hallazgo bloqueante para el adaptador (S2-061 §3 exige rechazar lecturas sin unidad); registrar la unidad configurada en la balanza como alternativa a decidir | Registro |
| P3-4 | Comando de cero y de tara (si existen) | Tras el comando, lectura en vacío = 0 y tara aplicada se refleja en la trama | `P3-4-cero-tara.csv` |
| P3-5 | Enviar comando inválido (p. ej. `X\r`) | La balanza ignora o responde error documentado; no queda bloqueada (siguiente lectura válida responde) | `P3-5-invalido.csv` |

### P4 — Estabilidad

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P4-1 | Plato vacío, 60 s de lecturas (`-IntervaloMs 500` si es bajo demanda) | ≥ 100 lecturas; todas = 0 ± 1 `d` | `P4-1-vacio.csv` |
| P4-2 | 1 kg sobre el plato, 60 s | Rango (máx − mín) de lecturas marcadas estables ≤ 1 `e` | `P4-2-1kg.csv` |
| P4-3 | Colocar 500 g y medir tiempo desde colocación hasta primera trama estable | ≤ 2 s [objetivo propuesto] en 5 repeticiones | `P4-3-asentamiento.csv` + video |
| P4-4 | Mover el plato / colocar el producto con golpe | Las tramas intermedias vienen marcadas **inestables**; el adaptador no debe aceptar ninguna de ellas | `P4-4-inestable.csv` + video |
| P4-5 | Captura continua de 30 min con 1 kg | Sin desconexiones ni tramas corruptas; deriva ≤ 1 `e` | `P4-5-deriva.csv` |

### P5 — Exactitud y tolerancia

Tolerancia de aceptación: **|lectura − masa patrón| ≤ 1 `e`** de la placa en el rango correspondiente. Si el fabricante declara un error máximo permitido mayor para ese rango, se usa el del fabricante y se anota la fuente. Sin masa patrón certificada, el resultado de exactitud queda [NO VERIFICADO — sin patrón].

| ID | Masa | Repeticiones | Criterio | ☐ |
|---|---|---|---|---|
| P5-1 | 10 g (mínimo del contrato) | 5 | Lectura estable dentro de tolerancia, o hallazgo si 10 g < `Min` de la balanza | ☐ |
| P5-2 | 100 g, 500 g, 1 kg, 2 kg | 5 c/u | Cada lectura dentro de tolerancia | ☐ |
| P5-3 | Masa cercana a Max | 3 | Dentro de tolerancia | ☐ |
| P5-4 | Comparar `Min` y `e` con el contrato (0.010 kg / 0.001 kg) | — | Registrar compatible / incompatible; si `e` > 1 g las lecturas siguen siendo múltiplos válidos de 1 g, pero si `Min` > 10 g se registra hallazgo de producto | ☐ |
| P5-5 | Repetibilidad: misma masa (500 g) quitar/poner 10 veces | 10 | Desviación máx. entre lecturas ≤ 1 `e` | ☐ |
| P5-6 | Excentricidad: 500 g en centro y 4 esquinas | 5 | Diferencia respecto del centro ≤ 1 `e` | ☐ |
| P5-7 | Sobrecarga (> Max, solo si es seguro para el equipo) | 1 | La trama indica sobrecarga/error; el adaptador no entrega peso | ☐ |
| P5-8 | Peso negativo (tarar con objeto y retirarlo) | 1 | Trama con signo negativo; el adaptador lo rechaza (peso no positivo) | ☐ |

### P6 — Robustez de conexión

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P6-1 | Desconectar cable USB/serie durante una captura | La herramienta/adaptador reporta error en ≤ 5 s [objetivo propuesto]; no se envía peso | `P6-1-desconexion.csv` |
| P6-2 | Reconectar | Lectura válida en ≤ 10 s sin reiniciar la app [objetivo propuesto] | `P6-2-reconexion.csv` |
| P6-3 | Apagar balanza con puerto abierto | Igual que P6-1 | `P6-3-apagado.csv` |
| P6-4 | Puerto ocupado por otra aplicación | Error claro de puerto en uso; sin bloqueo de la app | Captura de pantalla |

### P7 — Conversión al contrato S2-061 (adaptador)

Ejecutable cuando exista el adaptador. Hasta entonces se valida manualmente contra las tramas capturadas.

| ID | Lectura física | `peso_kg` esperado | ☐ |
|---|---|---|---|
| P7-1 | 750 g estable | 0.750 | ☐ |
| P7-2 | 1.234 kg estable (balanza en kg) | 1.234 | ☐ |
| P7-3 | Trama inestable | Rechazada, no se envía | ☐ |
| P7-4 | Trama sin unidad / unidad `lb` u `oz` | Rechazada en adaptador | ☐ |
| P7-5 | < 10 g (p. ej. 5 g) | Rechazada tras convertir | ☐ |
| P7-6 | Trama corrupta / checksum inválido | Rechazada | ☐ |

### P8 — Flujo POS de punta a punta

Precondiciones: turno ABIERTO (RN-06), JWT válido del tenant (RN-07), producto con `EsPesoVariable = true` y precio por kg en MS-3.

| ID | Acción | Criterio de aceptación | Evidencia |
|---|---|---|---|
| P8-1 | Escanear producto pesable, poner 750 g, confirmar | Ítem con `peso_kg = 0.750` y `cantidad = 0.750`; subtotal = `redondear(precio_kg × 0.750, 2, ToEven)` calculado por MS-5 | Video + respuesta HTTP |
| P8-2 | Venta mixta: 250 g + 1 kg + producto unitario | Dos líneas `0.250` y `1.000` kg con subtotales correctos | Video + respuesta HTTP |
| P8-3 | Balanza desconectada durante la venta | La UI ofrece ingreso manual con unidad explícita (fallback de [hardware.md](../../context/integraciones/hardware.md)); no se agrega ítem con peso obsoleto | Video |
| P8-4 | Producto no pesable con balanza cargada | No se envía `peso_kg` | Respuesta HTTP |

## 4. Evidencia mínima a capturar

1. Fotos: placa de datos, conexión física, nivel, masas patrón con su certificado.
2. Salidas de `-Listar` (P1-1, P1-2) y captura del Administrador de dispositivos con VID/PID.
3. CSV crudos generados por [capturar-balanza.ps1](capturar-balanza.ps1) para P2–P6 (contienen hex y ASCII con marca de tiempo; no editarlos).
4. Tabla de resultados P5 (masa nominal, 5 lecturas, error máx., tolerancia aplicada, resultado).
5. Videos cortos de P4-3, P4-4, P8-1 y P8-3.
6. Registro firmado: ejecutor, fecha, commit del repositorio, modelo/serie, parámetros serie.

## 5. Plantilla de registro

```text
Ejecutor:            Fecha:            Commit:
Balanza (marca/modelo/serie/firmware):
Max:        Min:        e:        d:        Clase:
Adaptador/driver:                 VID/PID:            COM:
Parámetros serie (baudios/paridad/datos/parada) y fuente:
Modo: continuo | bajo demanda    Comando lectura:     Comando cero/tara:
Formato de trama (ejemplo crudo + descripción de campos):

| Paso | Resultado | Valor medido | Criterio | Evidencia | Observación |
|------|-----------|--------------|----------|-----------|-------------|
```

## 6. Relación con el simulador

Las tramas reales capturadas en P3/P4/P5/P7 (CSV con hex) deben incorporarse como fixtures del simulador de balanza cuando se implemente, de modo que simulador y equipo físico compartan el mismo parser y los mismos casos de rechazo. Hasta contar con el equipo, el simulador solo puede validar el contrato conceptual `{valor, unidad}` de S2-061.

## 7. Criterio de cierre de la tarea física

La tarea física pasa de **CONDICIONADA** a **APROBADA** solo si P1–P6 están aprobados, P7 aprobado con el adaptador real y P8-1 a P8-3 aprobados, con la evidencia de §4. Cualquier hallazgo bloqueante (sin unidad en trama, `Min` > 10 g, COM inestable) se documenta y se decide antes de cerrar.

## 8. Validación realizada en esta entrega

- `capturar-balanza.ps1 -Listar` ejecutado en el equipo de desarrollo: sin puertos COM (no hay balanza conectada). La consulta PnP puede requerir permisos fuera de entornos restringidos.
- `capturar-balanza.ps1 -Puerto COM99 ...`: reporta error de puerto inexistente, lo registra en el CSV y termina con código 2.
- Ningún paso P1–P8 ha sido ejecutado con hardware: **[NO VERIFICADO]**.
