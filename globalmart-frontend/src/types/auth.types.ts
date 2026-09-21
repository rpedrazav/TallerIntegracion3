/**
 * Estructura del payload contenido en el JWT emitido por TenantIdentityService.
 * Basado en la especificación de GlobalMart OS (GlobalMart_ContextMaster.md).
 */
export interface JwtPayload {
  sub: string;
  tenant_id: string;
  sucursal_id: string;
  roles: string[];
  active_role: string;
  permissions: string[];
  exp: number;
  iat: number;
}

/**
 * Respuesta esperada del endpoint POST /api/auth/login.
 */
export interface LoginResponse {
  token?: string;
  access_token?: string;
  message?: string;
  [key: string]: any;
}
