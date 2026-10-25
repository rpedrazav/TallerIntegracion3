import { DevicesConfig } from '../devices/types';

/**
 * Configuración de adaptadores de dispositivos para Electron (S2-087).
 * Por defecto opera en modo 'mock', arrancando sin controladores ni permisos de hardware.
 * El modo 'real' está reservado para periféricos certificados y no activa rutas desconocidas.
 */
export const defaultDevicesConfig: DevicesConfig = {
  terminalMode: (process.env.DEVICE_TERMINAL_MODE as 'mock' | 'real') || 'mock',
  scaleMode: (process.env.DEVICE_SCALE_MODE as 'mock' | 'real') || 'mock',
  mockTerminalDelayMs: Number(process.env.MOCK_TERMINAL_DELAY_MS) || 400,
  autoApproveMock: process.env.AUTO_APPROVE_MOCK === 'true',
};
