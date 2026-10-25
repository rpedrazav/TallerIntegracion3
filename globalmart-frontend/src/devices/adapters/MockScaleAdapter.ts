import { IScaleAdapter, ScaleReading } from '../types';

/**
 * Adaptador de balanza simulada (Mock) para pesaje de productos a granel (S2-087 / S2-089).
 * Opera completamente en memoria sin controladores ni permisos de hardware nativos.
 */
export class MockScaleAdapter implements IScaleAdapter {
  private pesoActualKg = 0;
  private taraKg = 0;

  public async obtenerPeso(): Promise<ScaleReading> {
    const pesoNeto = Math.max(0, Math.round((this.pesoActualKg - this.taraKg) * 1000) / 1000);
    return {
      pesoKg: pesoNeto,
      estable: true,
      unidad: 'kg',
      timestamp: Date.now(),
    };
  }

  public async tarar(): Promise<{ exito: boolean; mensaje?: string }> {
    this.taraKg = this.pesoActualKg;
    return {
      exito: true,
      mensaje: `Balanza tarada en ${this.taraKg} kg. Peso neto actual: 0.000 kg.`,
    };
  }

  public async simularPeso(pesoKg: number): Promise<ScaleReading> {
    if (typeof pesoKg !== 'number' || isNaN(pesoKg) || pesoKg < 0) {
      throw new Error('El peso a simular debe ser un número positivo válido.');
    }
    this.pesoActualKg = pesoKg;
    return this.obtenerPeso();
  }
}
