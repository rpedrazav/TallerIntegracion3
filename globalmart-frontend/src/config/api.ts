/**
 * Configuración Centralizada de Destinos de APIs — GlobalMart OS Frontend
 * Tarea: TI3-659 (Plan ID: S2-028)
 *
 * Permite alternar entre los entornos:
 * - 'gateway': Tráfico unificado a través de Kong API Gateway (http://localhost:8000)
 * - 'local': Llamadas directas a microservicios en puertos locales de desarrollo
 * - 'remote': Servicios desplegados en ambiente remoto (dev.censei.cl)
 *
 * Admite variables de entorno (process.env o window.env) y overrides individuales.
 */

export type ApiMode = 'gateway' | 'local' | 'remote';

export interface ServiceUrls {
  identity: string;
  catalog: string;
  pos: string;
  inventory: string;
}

function getEnv(key: string): string | undefined {
  if (typeof process !== 'undefined' && process.env && process.env[key]) {
    return process.env[key];
  }
  if (typeof window !== 'undefined') {
    const win = window as unknown as { env?: Record<string, string> };
    if (win.env && win.env[key]) {
      return win.env[key];
    }
  }
  return undefined;
}

// Modo actual: variable de entorno API_MODE o REACT_APP_API_MODE; default: 'remote'
export const API_MODE: ApiMode = (getEnv('API_MODE') || getEnv('REACT_APP_API_MODE') || 'remote') as ApiMode;

// URLs base por defecto para cada modo
const DEFAULT_URLS: Record<ApiMode, ServiceUrls> = {
  remote: {
    identity: 'https://auth-rpedraza.dev.censei.cl',
    catalog: 'https://catalog-rpedraza.dev.censei.cl',
    pos: 'https://pos-rpedraza.dev.censei.cl',
    inventory: 'https://inventory-rpedraza.dev.censei.cl',
  },
  gateway: {
    // En modo Gateway, todas las peticiones pasan por Kong en el puerto 8000
    identity: 'http://localhost:8000/api',
    catalog: 'http://localhost:8000/api',
    pos: 'http://localhost:8000/api',
    inventory: 'http://localhost:8000/api',
  },
  local: {
    // Puertos dev de host según launchSettings.json y run-apis.sh
    identity: 'http://localhost:5124',
    catalog: 'http://localhost:5203',
    pos: 'http://localhost:5000',
    inventory: 'http://localhost:5205',
  },
};

const gatewayBase = getEnv('GATEWAY_URL') || getEnv('REACT_APP_GATEWAY_URL');

export const API_URLS: ServiceUrls = {
  identity: getEnv('IDENTITY_API_URL') || getEnv('REACT_APP_IDENTITY_API_URL') ||
    (API_MODE === 'gateway' && gatewayBase ? `${gatewayBase.replace(/\/+$/, '')}/api` : DEFAULT_URLS[API_MODE].identity),

  catalog: getEnv('CATALOG_API_URL') || getEnv('REACT_APP_CATALOG_API_URL') ||
    (API_MODE === 'gateway' && gatewayBase ? `${gatewayBase.replace(/\/+$/, '')}/api` : DEFAULT_URLS[API_MODE].catalog),

  pos: getEnv('POS_API_URL') || getEnv('REACT_APP_POS_API_URL') ||
    (API_MODE === 'gateway' && gatewayBase ? `${gatewayBase.replace(/\/+$/, '')}/api` : DEFAULT_URLS[API_MODE].pos),
  inventory: getEnv('INVENTORY_API_URL') || getEnv('REACT_APP_INVENTORY_API_URL') ||
    (API_MODE === 'gateway' && gatewayBase ? `${gatewayBase.replace(/\/+$/, '')}/api` : DEFAULT_URLS[API_MODE].inventory),
};

/**
 * Helper para construir una URL a un endpoint asegurando que no se dupliquen slashes
 * ni prefijos '/api'.
 */
export function buildServiceUrl(service: keyof ServiceUrls, path: string): string {
  const base = API_URLS[service].replace(/\/+$/, '');
  let cleanPath = path.trim();

  // Si la base ya termina en /api y el path empieza con /api/, evitar duplicarlo
  if (base.endsWith('/api') && cleanPath.startsWith('/api/')) {
    cleanPath = cleanPath.substring(4);
  } else if (!cleanPath.startsWith('/')) {
    cleanPath = `/${cleanPath}`;
  }

  return `${base}${cleanPath}`;
}
