import axios, { AxiosError, AxiosInstance } from 'axios';
import { API_URLS } from '../config/api';

const API_TIMEOUT_MS = 15000;

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
        window.location.hash = '#/login';
      }
      return Promise.reject(error);
    });
  }
  return client;
}

const identity = createClient(API_URLS.identity, false);
// Login va sin JWT ni interceptor de 401; el resto de MS-1 usa este cliente protegido.
const identityAuth = createClient(API_URLS.identity, true);
const catalog = createClient(API_URLS.catalog, true);
const pos = createClient(API_URLS.pos, true);

export interface LoginRequest {
  email: string;
  password: string;
  tenantCodigo: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  usuario: { id: string; nombre: string; email: string; activeRole: string; roles: string[] };
}

export const identityApi = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const response = await identity.post<LoginResponse>('/auth/login', request);
    return response.data;
  },
};

export interface Minimarket {
  id: string;
  nombre: string;
  codigo: string | null;
  activo: boolean;
  creadoEn: string;
}

export interface CrearMinimarketRequest {
  nombre: string;
  codigo: string;
  adminEmail: string;
  adminPassword: string;
}

// S3-098: alta y listado de minimarkets (solo SUPER_ADMIN; MS-1 responde 403 al resto).
export const tenantsApi = {
  list: async (): Promise<Minimarket[]> => {
    const response = await identityAuth.get<Minimarket[]>('/tenants');
    return response.data;
  },
  create: async (request: CrearMinimarketRequest): Promise<Minimarket> => {
    const response = await identityAuth.post<Minimarket>('/tenants', request);
    return response.data;
  },
};

export interface CatalogProduct {
  id: string;
  nombre: string;
  codigoBarras: string;
  precioBase: number;
  esPesoVariable: boolean;
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
  searchProducts: async (query: string): Promise<ProductPage> => {
    const response = await catalog.get<ProductPage>('/products/search', { params: { q: query, page: 1, pageSize: 15 } });
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
    peso_kg?: number | null;
  }>;
}

export interface CrearVentaItem {
  producto_id: string;
  cantidad?: number;
  peso_kg?: number;
}

export interface CobrarEfectivoResponse {
  venta: VentaPendiente;
  montoRecibido: number;
  vuelto: number;
}

export interface CobrarTarjetaResponse {
  venta: VentaPendiente;
  status: string;
  gateway_reference: string | null;
  message: string | null;
}

export interface EstadoIntentoTarjeta {
  status: 'NONE' | 'UNCERTAIN' | 'APPROVED' | 'DECLINED';
  venta: VentaPendiente;
  gateway_reference?: string | null;
  message?: string | null;
  request_id?: string;
}

export interface AnulacionInfo {
  motivo: string;
  autorizado_por: string;
  created_at: string;
}

export interface AnularVentaResponse {
  venta_id: string;
  estado: string;
  anulacion: AnulacionInfo;
  reembolso_tarjeta: string;
  devolucion_efectivo: string;
}

export const posApi = {
  activeShift: async (): Promise<TurnoActivo> => {
    const response = await pos.get<TurnoActivo>('/turnos/activo');
    return response.data;
  },
  openShifts: async (): Promise<TurnoActivo[]> => {
    const response = await pos.get<TurnoActivo[]>('/turnos/abiertos');
    return response.data;
  },
  openShift: async (sucursalId: string, montoInicial: number): Promise<TurnoActivo> => {
    const response = await pos.post<TurnoActivo>('/turnos/abrir', {
      sucursal_id: sucursalId, monto_fondo_inicial: montoInicial,
    });
    return response.data;
  },
  balanceShift: async (montoDeclarado: number): Promise<CuadreResult> => {
    const response = await pos.post<CuadreResult>('/turnos/cuadre', {
      monto_declarado: montoDeclarado,
    });
    return response.data;
  },
  closeShift: async (): Promise<TurnoActivo> => {
    const response = await pos.post<TurnoActivo>('/turnos/cerrar');
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
  addSaleItem: async (
    saleId: string,
    productId: string,
    quantity?: number,
    weightKg?: number,
  ): Promise<void> => {
    await pos.post(`/ventas/${saleId}/items`, {
      producto_id: productId,
      cantidad: quantity,
      peso_kg: weightKg,
    });
  },
  updateSaleItem: async (
    saleId: string,
    itemId: string,
    quantity?: number,
    weightKg?: number,
  ): Promise<void> => {
    await pos.put(`/ventas/${saleId}/items/${itemId}`, {
      cantidad: quantity,
      peso_kg: weightKg,
    });
  },
  deleteSaleItem: async (saleId: string, itemId: string): Promise<void> => {
    await pos.delete(`/ventas/${saleId}/items/${itemId}`);
  },
  createSale: async (items: CrearVentaItem[], method: 'EFECTIVO' | 'TARJETA'): Promise<VentaPendiente> => {
    const response = await pos.post<VentaPendiente>('/ventas', {
      items,
      metodo_pago: method,
    });
    return response.data;
  },
  chargeCash: async (saleId: string, amountReceived: number): Promise<CobrarEfectivoResponse> => {
    const response = await pos.post<CobrarEfectivoResponse>(`/ventas/${saleId}/cobrar`, {
      monto_recibido: amountReceived,
    });
    return response.data;
  },
  chargeCard: async (saleId: string, idempotencyKey: string): Promise<CobrarTarjetaResponse> => {
    const response = await pos.post<CobrarTarjetaResponse>(`/ventas/${saleId}/cobrar-tarjeta`, {
      idempotency_key: idempotencyKey,
    });
    return response.data;
  },
  cardAttemptStatus: async (saleId: string): Promise<EstadoIntentoTarjeta> => {
    const response = await pos.get<EstadoIntentoTarjeta>(`/ventas/${saleId}/intento-tarjeta`);
    return response.data;
  },
  changeSaleMethod: async (saleId: string, method: 'EFECTIVO' | 'TARJETA'): Promise<VentaPendiente> => {
    const response = await pos.patch<VentaPendiente>(`/ventas/${saleId}/metodo-pago`, { metodo_pago: method });
    return response.data;
  },
  anularVenta: async (saleId: string, motivo: string): Promise<AnularVentaResponse> => {
    const response = await pos.post<AnularVentaResponse>(`/ventas/${saleId}/anular`, { motivo });
    return response.data;
  },
};
