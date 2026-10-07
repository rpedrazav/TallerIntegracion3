import { create } from 'zustand';
import { parseJwt } from '../utils/jwt';

interface TenantState {
  token: string | null;
  tenantId: string | null;
  esAdmin: boolean;
  setToken: (token: string | null) => void;
  clear: () => void;
}

export const useTenantStore = create<TenantState>((set) => ({
  token: null,
  tenantId: null,
  esAdmin: false,
  setToken: (token) => {
    if (!token) {
      set({ token: null, tenantId: localStorage.getItem('tenant') || '', esAdmin: false });
      return;
    }
    try {
      const payload = parseJwt(token);
      const id = payload?.tenant_id || localStorage.getItem('tenant') || '';
      const rolActivo = payload?.active_role?.toUpperCase();
      const roles = Array.isArray(payload?.roles)
        ? payload.roles.map((r: string) => r.toUpperCase())
        : [];
      const tieneRolAdmin =
        rolActivo === 'ADMIN' ||
        rolActivo === 'SUPER_ADMIN' ||
        roles.includes('ADMIN') ||
        roles.includes('SUPER_ADMIN');

      set({ token, tenantId: id, esAdmin: tieneRolAdmin });
    } catch {
      set({ token: null, tenantId: null, esAdmin: false });
    }
  },
  clear: () => set({ token: null, tenantId: null, esAdmin: false }),
}));
