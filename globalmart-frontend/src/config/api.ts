/**
 * Configuración centralizada de destinos de APIs — GlobalMart OS Frontend (S3-026)
 *
 * Modos soportados mediante la variable única `API_MODE`:
 * - 'remote' (por defecto): Ingress del clúster (dev.censei.cl)
 * - 'local': Docker Compose a través de Kong API Gateway (http://localhost:8000)
 */

export type ApiMode = 'remote' | 'local';

export interface ServiceUrls {
  identity: string;
  catalog: string;
  pos: string;
  inventory: string;
}

const MODE_URLS: Record<ApiMode, ServiceUrls> = {
  remote: {
    identity: 'https://auth-rpedraza.dev.censei.cl/api',
    catalog: 'https://catalog-rpedraza.dev.censei.cl/api',
    pos: 'https://pos-rpedraza.dev.censei.cl/api',
    inventory: 'https://inventory-rpedraza.dev.censei.cl/api',
  },
  local: {
    identity: 'http://localhost:8000/api',
    catalog: 'http://localhost:8000/api',
    pos: 'http://localhost:8000/api',
    inventory: 'http://localhost:8000/api',
  },
};

export function resolveApiMode(rawMode?: string): ApiMode {
  return rawMode?.trim().toLowerCase() === 'local' ? 'local' : 'remote';
}

export function getApiUrls(mode: ApiMode = API_MODE): ServiceUrls {
  return MODE_URLS[mode];
}

function readApiMode(): string | undefined {
  try {
    return process.env.API_MODE;
  } catch {
    return undefined;
  }
}

export const API_MODE: ApiMode = resolveApiMode(readApiMode());

export const API_URLS: ServiceUrls = getApiUrls(API_MODE);

export function buildServiceUrl(service: keyof ServiceUrls, path: string): string {
  const cleanPath = path.trim();
  const normalizedPath = cleanPath.startsWith('/') ? cleanPath : `/${cleanPath}`;
  return `${API_URLS[service]}${normalizedPath}`;
}
