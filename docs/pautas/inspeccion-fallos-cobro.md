# Pauta de Inspección de Fallos y Correlación de Cobro (S2-059 / S2-060)

**Módulo:** MS-5 (POS & Cart Service) / Pasarela de Pagos  
**Cumplimiento Normativo:** RN-02 (Pagos con tarjeta nunca localmente), PCI-DSS (Exclusión estricta de PAN/CVV).  
**Última Revisión:** 2026-10-25  

---

## 1. Estructura de Correlación

Todo cobro con tarjeta emite logs estructurados bajo la etiqueta `[COBRO_CORRELATION]`. La trazabilidad se realiza exclusivamente mediante identificadores opacos:

| Identificador | Propósito | Ejemplo |
|---|---|---|
| `CorrelationId` | Identifica la transacción de cobro completa de punta a punta entre frontend, backend y pasarela. | `corr-9f8e7d6c5b4a` |
| `IntentoId` | Identifica un intento específico de comunicación con la pasarela (para distinguir reintentos del mismo cobro). | `int-1a2b3c4d5e6f` |
| `VentaId` | Identificador único de la venta en MS-5 (`GUID`). | `c4b12a87-1234-5678-90ab-cdef12345678` |
| `CodigoAutorizacion` | Código opaco devuelto por la pasarela al autorizar la transacción. | `AUTH-9F3A1C` |
| `ReferenciaExterna` | RRN o referencia opaca del procesador. | `RRN-638654876123` |

### 🔒 Regla de Exclusión de Secretos
Bajo ninguna circunstancia se registran en los logs:
- Números de tarjeta (PAN completos o parciales).
- Códigos de seguridad (CVV / CVC).
- PINs ni tokens secretos de autenticación privada.
- Solo se manejan tokens opacos y códigos de autorización públicos.

---

## 2. Eventos Registrados

1. **Inicio de Intento:**
   ```
   [COBRO_CORRELATION] Evento=Inicio VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} Monto={Monto} Moneda={Moneda}
   ```
2. **Aprobación Exitosa:**
   ```
   [COBRO_CORRELATION] Evento=Aprobado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoAutorizacion={CodigoAutorizacion} ReferenciaExterna={ReferenciaExterna} DuracionMs={DuracionMs}
   ```
3. **Rechazo por Pasarela:**
   ```
   [COBRO_CORRELATION] Evento=Rechazado VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} CodigoRespuesta={CodigoRespuesta} Motivo={Motivo} DuracionMs={DuracionMs}
   ```
4. **Pérdida Temporal o Error de Conexión:**
   ```
   [COBRO_CORRELATION] Evento=ErrorComunicacion VentaId={VentaId} IntentoId={IntentoId} CorrelationId={CorrelationId} Error={Error} DuracionMs={DuracionMs}
   ```

---

## 3. Pauta de Diagnóstico e Inspección

### Caso A: El cliente reporta que se le cobró pero la venta no se completó
1. Buscar en logs por `CorrelationId` o `VentaId`:
   ```bash
   grep "c4b12a87-1234-5678-90ab-cdef12345678" logs/pos.log | grep "\[COBRO_CORRELATION\]"
   ```
2. Si aparece `Evento=Aprobado`:
   - Verificar si la venta tiene `Pago` registrado y estado `COMPLETADA`.
   - Verificar si hubo fallo posterior en la publicación de Kafka (la transacción de BD es atómica).
3. Si aparece `Evento=Rechazado`:
   - Informar al cajero que la transacción fue denegada por la pasarela (ej. `REC-51` por fondos insuficientes). La venta permanece en `PENDIENTE` para permitir reintento o cambio a efectivo.

### Caso B: Pérdida temporal del simulador o pasarela (HTTP 503)
1. Buscar en logs:
   ```bash
   grep "Evento=ErrorComunicacion" logs/pos.log
   ```
2. Confirmar que el estado de la venta permanece en `PENDIENTE`.
3. Validar conectividad de red con el simulador o reiniciar el servicio de pasarela.
4. El cajero puede volver a presionar "Cobrar con Tarjeta" generando un nuevo `IntentoId`.

---

## 4. Verificación Automatizada en CI / Local

Ejecutar el script reproducible:
```bash
./scripts/smoke-tarjeta.sh
```
Valida de forma desatendida las 3 situaciones de cobro y la ausencia total de secretos en los logs.
