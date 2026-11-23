import type { AxiosError } from 'axios';

const mensajes: Record<string, string> = {
  SOLICITUD_INVALIDA: 'Revisa el motivo: debe contener entre 1 y 500 caracteres.',
  IDENTIDAD_INVALIDA: 'Tu sesión no contiene una identidad válida. Vuelve a iniciar sesión.',
  VENTA_NO_ENCONTRADA: 'La venta no existe o no tienes acceso a ella.',
  ESTADO_NO_ANULABLE: 'El estado actual de la venta no permite anularla. Revisa su estado antes de continuar.',
  VENTA_YA_ANULADA: 'La venta ya está anulada. No se realizará otra anulación.',
  PAGO_INCIERTO: 'El cobro aún no tiene un resultado definitivo. Resuelve el pago antes de anular la venta.',
  METODO_NO_SOPORTADO: 'La anulación de una venta con pago mixto no está disponible.',
  REEMBOLSO_NO_DISPONIBLE: 'No se puede anular este pago con tarjeta sin una devolución confirmada por la pasarela.',
  PAGO_INCONSISTENTE: 'Los pagos registrados no coinciden con la venta. Solicita una revisión antes de anular.',
  TURNO_NO_ANULABLE: 'El turno original está cerrado o no está disponible. No se puede anular esta venta.',
  REVERSA_RECHAZADA: 'La pasarela rechazó la devolución. La venta sigue completada y la tarjeta no fue reembolsada.',
  REVERSA_PENDIENTE: 'La devolución de tarjeta sigue pendiente de confirmación. Consulta la reversa existente; no solicites otra devolución.',
  ERROR_PERSISTENCIA: 'No se pudo confirmar la anulación en el sistema. Revisa la venta y la reversa antes de volver a intentarlo.',
};

export function mensajeErrorAnulacion(error: unknown): string {
  const response = (error as AxiosError<{ codigo?: string }>)?.response;
  const codigo = response?.data?.codigo;
  if (codigo && Object.prototype.hasOwnProperty.call(mensajes, codigo)) return mensajes[codigo];
  switch (response?.status) {
    case 400: return mensajes.SOLICITUD_INVALIDA;
    case 401: return 'Tu sesión venció o no es válida. Vuelve a iniciar sesión.';
    case 403: return 'Solo un administrador autorizado puede anular ventas.';
    case 404: return mensajes.VENTA_NO_ENCONTRADA;
    case 409: return 'La venta tiene un conflicto que impide confirmar la anulación. Revisa su estado antes de continuar.';
    default: return 'No se pudo confirmar el resultado de la anulación. Revisa la venta y, si pagaste con tarjeta, la reversa existente antes de reintentar.';
  }
}
