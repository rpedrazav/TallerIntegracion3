import {
  ITerminalAdapter,
  IniciarOperacionParams,
  OperacionTerminalResult,
  EstadoTerminalResult,
  AutorizacionTerminalResult,
  SimulatedAction,
} from '../types';

/**
 * Adaptador de terminal POS físico real (S2-086 / S2-087).
 * Criterio de aceptación:
 * - Reserva el proveedor real sin activar rutas desconocidas ni ejecutar drivers no verificados.
 * - NO declara compatibilidad con marcas ni lectores de tarjeta no probados (criterio S2-086).
 */
export class RealTerminalAdapter implements ITerminalAdapter {
  private readonly mensajeError =
    'Adaptador de hardware físico reservado: no se declara compatibilidad con lectores no probados ni certificados. Utilice el modo mock.';

  public async iniciar(_params: IniciarOperacionParams): Promise<OperacionTerminalResult> {
    return {
      exito: false,
      estado: 'ERROR',
      mensaje: this.mensajeError,
    };
  }

  public async obtenerEstado(): Promise<EstadoTerminalResult> {
    return {
      estado: 'ERROR',
    };
  }

  public async cancelar(): Promise<OperacionTerminalResult> {
    return {
      exito: false,
      estado: 'ERROR',
      mensaje: this.mensajeError,
    };
  }

  public async autorizar(_simulacion?: SimulatedAction): Promise<AutorizacionTerminalResult> {
    return {
      exito: false,
      estado: 'ERROR',
      mensaje: this.mensajeError,
      correlationId: 'none',
    };
  }
}
