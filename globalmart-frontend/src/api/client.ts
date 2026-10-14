import axios, { AxiosError, AxiosInstance } from 'axios';

const API_TIMEOUT_MS = 15000;
const API_URLS = {
  identity: 'https://auth-rpedraza.dev.censei.cl',
  catalog: 'https://catalog-rpedraza.dev.censei.cl',
  pos: 'https://pos-rpedraza.dev.censei.cl',
} as const;

function createClient(baseURL: string, protectedApi: boolean): AxiosInstance {
  const client = axios.create({ baseURL, timeout: API_TIMEOUT_MS });
  if (protectedApi) {
    client.interceptors.request.use(async (config) => {
      const token = await window.api.getToken();
      if (token) config.headers.Authorization = `Bearer ${token}`;
      return config;
    });
    client.interceptors.response.use(undefined, async (error: AxiosError) => {
      if (error.response?.status === 401) {
        await window.api.logout().catch(() => undefined);
        localStorage.removeItem('token');
        localStorage.removeItem('tenant');
        window.location.hash = '#/login';
      }
      return Promise.reject(error);
    });
  }
  return client;
}

const identity = createClient(API_URLS.identity, false);
const catalog = createClient(API_URLS.catalog, true);
const pos = createClient(API_URLS.pos, true);

export interface LoginRequest {
  email: string;
  password: string;
  tenantId: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  usuario: { id: string; nombre: string; email: string; activeRole: string; roles: string[] };
}

export const identityApi = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const response = await identity.post<LoginResponse>('/auth/login', request, {
      headers: { 'x-tenant-id': request.tenantId },
    });
    return response.data;
  },
};

export interface CatalogProduct {
  id: string;
  nombre: string;
  codigoBarras: string;
  precioBase: number;
}

interface ProductPage {
  data: CatalogProduct[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const catalogApi = {
  listProducts: async (page = 1, pageSize = 500): Promise<ProductPage> => {
    const response = await catalog.get<ProductPage>('/products', { params: { page, pageSize } });
    return response.data;
  },
  lookupProduct: async (barcode: string): Promise<CatalogProduct> => {
    const response = await catalog.get<CatalogProduct>('/products/lookup', { params: { barcode } });
    return response.data;
  },
};

export interface TurnoActivo {
  id: string;
  tenantId: string;
  cajeroId: string;
  sucursalId: string;
  montoApertura: number;
  estado: number;
  abiertoAt: string;
}

export interface CuadreResult {
  turno_id: string;
  efectivo_esperado: number;
  monto_declarado: number;
  diferencia: number;
}

export interface VentaPendiente {
  id: string;
  turno_id: string;
  subtotal: number;
  impuestos: number;
  total: number;
  metodo_pago: string;
  estado: string;
  created_at: string;
  items: Array<{
    id: string;
    producto_id: string;
    nombre_producto: string;
    cantidad: number;
    precio_unitario: number;
    subtotal: number;
  }>;
}

export interface CrearVentaItem {
  producto_id: string;
  cantidad: number;
}

export const posApi = {
  activeShift: async (): Promise<TurnoActivo> => {
    const response = await pos.get<TurnoActivo>('/api/turnos/activo');
    return response.data;
  },
  openShift: async (sucursalId: string, montoInicial: number): Promise<TurnoActivo> => {
    const response = await pos.post<TurnoActivo>('/api/turnos/abrir', {
      sucursal_id: sucursalId, monto_fondo_inicial: montoInicial,
    });
    return response.data;
  },
  balanceShift: async (montoDeclarado: number): Promise<CuadreResult> => {
    const response = await pos.post<CuadreResult>('/api/turnos/cuadre', {
      monto_declarado: montoDeclarado,
    });
    return response.data;
  },
  closeShift: async (): Promise<TurnoActivo> => {
    const response = await pos.post<TurnoActivo>('/api/turnos/cerrar');
    return response.data;
  },
  salesByShift: async (shiftId: string): Promise<VentaPendiente[]> => {
    const response = await pos.get<VentaPendiente[]>(`/ventas/turno/${shiftId}`);
    return response.data;
  },
  getSale: async (saleId: string): Promise<VentaPendiente> => {
    const response = await pos.get<VentaPendiente>(`/ventas/${saleId}`);
    return response.data;
  },
  addSaleItem: async (saleId: string, productId: string, quantity: number): Promise<void> => {
    await pos.post(`/ventas/${saleId}/items`, { producto_id: productId, cantidad: quantity });
  },
  updateSaleItem: async (saleId: string, itemId: string, quantity: number): Promise<void> => {
    await pos.put(`/ventas/${saleId}/items/${itemId}`, { cantidad: quantity });
  },
  deleteSaleItem: async (saleId: string, itemId: string): Promise<void> => {
    await pos.delete(`/ventas/${saleId}/items/${itemId}`);
  },
  createSale: async (items: CrearVentaItem[]): Promise<VentaPendiente> => {
    const response = await pos.post<VentaPendiente>('/ventas', {
      items,
      metodo_pago: 'EFECTIVO',
    });
    return response.data;
  },
};
