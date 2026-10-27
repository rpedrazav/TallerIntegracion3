import type {
  ScaleErrorCode,
  ScaleRendererApi,
  StableWeightReadOptions,
} from './scale-contract';

export type ScaleCartWeightResult =
  | { ok: true; weightKg: number }
  | { ok: false; code: ScaleErrorCode; message: string };

const MINIMUM_WEIGHT_KG = 0.01;
const GRAMS_PER_KILOGRAM = 1000;
const FLOATING_POINT_TOLERANCE = 1e-9;

const isValidCartWeight = (weightKg: number): boolean => {
  if (!Number.isFinite(weightKg) || weightKg < MINIMUM_WEIGHT_KG) return false;
  const grams = weightKg * GRAMS_PER_KILOGRAM;
  return Math.abs(grams - Math.round(grams)) <= FLOATING_POINT_TOLERANCE;
};

/** Aplica al respaldo manual el mismo mínimo y la misma precisión que a la balanza. */
export const manualWeightForCart = (input: string): ScaleCartWeightResult => {
  const normalized = input.trim().replace(',', '.');
  const weightKg = Number(normalized);
  if (!/^\d+(?:\.\d{1,3})?$/.test(normalized) || !isValidCartWeight(weightKg)) {
    return { ok: false, code: 'invalid_reading', message: 'Ingresa un peso de al menos 0,010 kg con precisión de 0,001 kg.' };
  }
  return { ok: true, weightKg };
};

/**
 * Único punto por el que una lectura de balanza puede transformarse en peso
 * del carrito. Reconecta cuando sea necesario y rechaza cualquier respuesta
 * que no sea estable, positiva, canónica y precisa a un gramo.
 */
export const readStableWeightForCart = async (
  scale: ScaleRendererApi,
  options: StableWeightReadOptions = {
    timeoutMs: 5_000,
    stabilityWindowMs: 250,
  },
): Promise<ScaleCartWeightResult> => {
  try {
    const currentState = await scale.getState();
    if (currentState.connectionStatus !== 'connected') {
      const connection = await scale.connect();
      if (!connection.ok || connection.state.connectionStatus !== 'connected') {
        return {
          ok: false,
          code: connection.ok ? 'connection_failed' : connection.error.code,
          message: connection.ok
            ? 'La balanza no confirmó la conexión.'
            : connection.error.message,
        };
      }
    }

    const result = await scale.readStableWeight(options);
    if (!result.ok) {
      return { ok: false, code: result.error.code, message: result.error.message };
    }

    const reading = result.value;
    if (
      reading.stable !== true
      || reading.unit !== 'kg'
      || !isValidCartWeight(reading.weightKg)
    ) {
      return {
        ok: false,
        code: 'invalid_reading',
        message: 'La balanza no entregó un peso estable, positivo y válido.',
      };
    }

    return { ok: true, weightKg: reading.weightKg };
  } catch {
    return {
      ok: false,
      code: 'device_error',
      message: 'No se pudo consultar la balanza.',
    };
  }
};
