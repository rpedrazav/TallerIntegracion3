import { DevicesConfig, SimulatedTerminalScenario } from '../devices/types';

function getProcessEnv(name: string): string | undefined {
  return typeof process !== 'undefined' ? process.env[name] : undefined;
}

const terminalMode = getProcessEnv('DEVICE_TERMINAL_MODE') === 'real' ? 'real' : 'mock';
const scaleMode = getProcessEnv('DEVICE_SCALE_MODE') === 'real' ? 'real' : 'mock';
const configuredDelay = Number(getProcessEnv('MOCK_TERMINAL_DELAY_MS'));
const mockTerminalDelayMs = Number.isFinite(configuredDelay) && configuredDelay >= 0
  ? configuredDelay
  : 400;
const autoApproveMock = getProcessEnv('AUTO_APPROVE_MOCK') === 'true';
const configuredScenario = getProcessEnv('MOCK_TERMINAL_SCENARIO');
const simulatedScenarios: readonly SimulatedTerminalScenario[] = [
  'waiting',
  'approved',
  'declined',
  'cancelled',
  'disconnected',
];
const mockTerminalScenario: SimulatedTerminalScenario = simulatedScenarios.includes(
  configuredScenario as SimulatedTerminalScenario,
)
  ? configuredScenario as SimulatedTerminalScenario
  : autoApproveMock ? 'approved' : 'waiting';

/**
 * Configuración de adaptadores de dispositivos para Electron (S2-087).
 * Por defecto opera en modo 'mock', arrancando sin controladores ni permisos de hardware.
 * El modo 'real' está reservado para periféricos certificados y no activa rutas desconocidas.
 */
export const defaultDevicesConfig: DevicesConfig = {
  terminalMode,
  scaleMode,
  mockTerminalDelayMs,
  autoApproveMock,
  mockTerminalScenario,
};
