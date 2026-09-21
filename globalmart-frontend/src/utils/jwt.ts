import { JwtPayload } from '../types/auth.types';

/**
 * Decodifica el payload de un token JWT firmado (base64) sin librerías externas.
 * Maneja caracteres especiales y UTF-8 correctamente.
 *
 * @param token - El token JWT completo (header.payload.signature)
 * @returns El payload decodificado como JwtPayload o null si el formato es inválido.
 */
export function parseJwtPayload(token: string): JwtPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      console.warn('[jwt] El token no tiene un formato JWT válido (esperaba 3 segmentos separados por punto).');
      return null;
    }

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload) as JwtPayload;
  } catch (err) {
    console.error('[jwt] Error al decodificar JWT payload:', err);
    return null;
  }
}

/**
 * Crea un token JWT de prueba firmado para entornos de desarrollo/demo (TI3-165).
 */
export function createDemoToken(role: 'CAJERO' | 'ADMIN' = 'CAJERO'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');

  const payload: JwtPayload = {
    sub: role === 'ADMIN' ? 'admin_001' : 'cajero_santiago_001',
    tenant_id: 'minimarket_santiago_001',
    sucursal_id: 'sucursal_providencia',
    roles: [role],
    active_role: role,
    permissions: role === 'ADMIN' ? ['*'] : ['discount.apply'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };

  const payloadEncoded = btoa(unescape(encodeURIComponent(JSON.stringify(payload))))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');

  const signature = 'demo_dev_signature';
  return `${header}.${payloadEncoded}.${signature}`;
}
