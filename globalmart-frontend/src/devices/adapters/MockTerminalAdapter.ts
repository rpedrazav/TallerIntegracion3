import {
  ITerminalAdapter,
  IniciarOperacionParams,
  OperacionTerminalResult,
  EstadoTerminalResult,
  AutorizacionTerminalResult,
  TerminalState,
  SimulatedAction,
} from '../types';

/**
 * Adaptador de terminal POS simulado (Mock) para desarrollo y smoke tests (S2-086 / S2-087).
 * Criterio de aceptación: Arranca sin controladores ni permisos de hardware.
 * Desacoplado: No declara compatibilidad con marcas ni protocolos físicos no probados.
 */
export class MockTerminalAdapter implements ITerminalAdapter {
  private estado: TerminalState = 'IDLE';
  private operacionActiva?: {
    monto: number;
    moneda: string;
    correlationId: string;
    iniciadoEn: number;
  };

  public async iniciar(params: IniciarOperacionParams): Promise<OperacionTerminalResult> {
    if (this.estado === 'ESPERANDO_TARJETA' || this.estado === 'PROCESANDO') {
      return {
        exito: false,
        estado: this.estado,
        mensaje: 'Ya existe una operación en curso en el terminal.',
        correlationId: this.operacionActiva?.correlationId,
      };
    }

    const correlationId = params.correlationId || `corr_mock_${Date.now()}`;
    this.estado = 'ESPERANDO_TARJETA';
    this.operacionActiva = {
      monto: params.monto,
      moneda: params.moneda || 'CLP',
      correlationId,
      iniciadoEn: Date.now(),
    };

    return {
      exito: true,
      estado: this.estado,
      mensaje: 'Terminal listo. Inserte, deslice o acerque tarjeta.',
      correlationId,
    };
  }

  public async obtenerEstado(): Promise<EstadoTerminalResult> {
    return {
      estado: this.estado,
      operacionActiva: this.operacionActiva ? { ...this.operacionActiva } : undefined,
    };
  }

  public async cancelar(): Promise<OperacionTerminalResult> {
    if (this.estado === 'IDLE') {
      return {
        exito: true,
        estado: 'IDLE',
        mensaje: 'Terminal ya se encuentra inactivo.',
      };
    }

    const correlationId = this.operacionActiva?.correlationId;
    this.estado = 'CANCELADO';
    this.operacionActiva = undefined;

    // Se reinicia a IDLE tras cancelación
    setTimeout(() => {
      if (this.estado === 'CANCELADO') {
        this.estado = 'IDLE';
      }
    }, 500);

    return {
      exito: true,
      estado: 'CANCELADO',
      mensaje: 'Operación cancelada por el usuario o cajero.',
      correlationId,
    };
  }

  public async autorizar(simulacion: SimulatedAction = 'APROBAR'): Promise<AutorizacionTerminalResult> {
    if (this.estado !== 'ESPERANDO_TARJETA') {
      return {
        exito: false,
        estado: this.estado,
        mensaje: `No se puede autorizar en estado actual: ${this.estado}. Debe iniciar una operación primero.`,
        correlationId: this.operacionActiva?.correlationId || 'none',
      };
    }

    const correlationId = this.operacionActiva?.correlationId || `corr_mock_${Date.now()}`;
    this.estado = 'PROCESANDO';

    switch (simulacion) {
      case 'APROBAR': {
        const tokenTransaccion = `tok_terminal_sim_${Math.random().toString(36).substring(2, 10)}`;
        const codigoAutorizacion = `AUTH-${Math.random().toString(36).substring(2, 8).toUpperCase()}`;
        this.estado = 'APROBADO';
        this.operacionActiva = undefined;

        return {
          exito: true,
          estado: 'APROBADO',
          tokenTransaccion,
          codigoAutorizacion,
          mensaje: 'Transacción aprobada en terminal simulado.',
          correlationId,
        };
      }

      case 'RECHAZAR': {
        this.estado = 'RECHAZADO';
        this.operacionActiva = undefined;

        return {
          exito: false,
          estado: 'RECHAZADO',
          mensaje: 'Tarjeta declinada o fondos insuficientes (simulado).',
          correlationId,
        };
      }

      case 'TIMEOUT': {
        this.estado = 'ERROR';
        this.operacionActiva = undefined;

        return {
          exito: false,
          estado: 'ERROR',
          mensaje: 'Tiempo de espera de lectura de tarjeta agotado en terminal.',
          correlationId,
        };
      }

      case 'ERROR_LECTURA':
      default: {
        this.estado = 'ERROR';
        this.operacionActiva = undefined;

        return {
          exito: false,
          estado: 'ERROR',
          mensaje: 'Error al leer chip o banda magnética.',
          correlationId,
        };
      }
    }
  }
}
