// Configuración centralizada del frontend.
// Cambiar estos valores para apuntar a otro entorno (staging, producción, etc.)

/** URL base del API Gateway (Kong). */
export const API_BASE_URL =
  (typeof process !== 'undefined' && process?.env?.API_BASE_URL) || 'http://localhost:8100';
