import { defaultDevicesConfig } from '../config/devices.config';
import type { DevicesConfig } from '../devices/types';
import { Ms5TerminalAdapter } from './Ms5TerminalAdapter';
import { SimulatedTerminalAdapter } from './SimulatedTerminalAdapter';
import type { TerminalPort } from './TerminalPort';

export function createTerminalPort(config: DevicesConfig = defaultDevicesConfig): TerminalPort {
  if (config.terminalMode === 'real') return new Ms5TerminalAdapter();

  return new SimulatedTerminalAdapter({
    scenario: config.mockTerminalScenario,
    delayMs: config.mockTerminalDelayMs,
  });
}
