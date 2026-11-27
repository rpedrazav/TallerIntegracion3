import { create } from 'zustand';
import { parseJwt } from '../utils/jwt';

interface TenantState {
  token: string | null;
  tenantId: string | null;
  tenantNombre: string | null;
  esAdmin: boolean;
  esSuperAdmin: boolean;
  setToken: (token: string | null) => void;
  clear: () => void;
}

// Tenant, nombre del minimarket y rol salen solo del JWT emitido por MS-1.
export const useTenantStore = create<TenantState>((set) => ({
  token: null,
  tenantId: null,
  tenantNombre: null,
  esAdmin: false,
  esSuperAdmin: false,
  setToken: (token) => {
    const payload = parseJwt(token);
    const rolActivo = payload?.active_role?.toUpperCase();
    set({
      token,
      tenantId: payload?.tenant_id ?? null,
      tenantNombre: payload?.tenant_nombre ?? null,
      esAdmin: rolActivo === 'ADMIN' || rolActivo === 'SUPER_ADMIN',
      esSuperAdmin: rolActivo === 'SUPER_ADMIN',
    });
  },
  clear: () => set({ token: null, tenantId: null, tenantNombre: null, esAdmin: false, esSuperAdmin: false }),
}));
