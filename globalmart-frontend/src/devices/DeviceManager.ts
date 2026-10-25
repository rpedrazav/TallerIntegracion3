import {
  ITerminalAdapter,
  IScaleAdapter,
  DevicesConfig,
} from './types';
import { MockTerminalAdapter } from './adapters/MockTerminalAdapter';
import { RealTerminalAdapter } from './adapters/RealTerminalAdapter';
import { MockScaleAdapter } from './adapters/MockScaleAdapter';
import { defaultDevicesConfig } from '../config/devices.config';

/**
 * Gestor central de adaptadores de dispositivos (S2-087).
 * Selecciona el adaptador mock o real según la configuración.
 * Arranca por defecto en modo mock sin requerir drivers ni hardware.
 */
export class DeviceManager {
  private static instance: DeviceManager;
  private readonly config: DevicesConfig;
  private terminalAdapter: ITerminalAdapter;
  private scaleAdapter: IScaleAdapter;

  private constructor(config: DevicesConfig = defaultDevicesConfig) {
    this.config = config;

    // Inicializar adaptador de terminal
    if (this.config.terminalMode === 'real') {
      this.terminalAdapter = new RealTerminalAdapter();
    } else {
      this.terminalAdapter = new MockTerminalAdapter();
    }

    // Inicializar adaptador de balanza
    this.scaleAdapter = new MockScaleAdapter();
  }

  public static getInstance(config?: DevicesConfig): DeviceManager {
    if (!DeviceManager.instance) {
      DeviceManager.instance = new DeviceManager(config);
    }
    return DeviceManager.instance;
  }

  public getTerminal(): ITerminalAdapter {
    return this.terminalAdapter;
  }

  public getScale(): IScaleAdapter {
    return this.scaleAdapter;
  }

  public getConfig(): DevicesConfig {
    return { ...this.config };
  }
}
