import type { ScaleAdapter } from './scale-contract';
import {
  type SerialScaleConfiguration,
  validateSerialScaleConfiguration,
} from './serial-scale-adapter';
import {
  SimulatedScaleAdapter,
  type SimulatedScaleAdapterConfig,
} from './simulated-scale-adapter';

export const SCALE_ADAPTER_DRIVERS = ['simulator', 'serial'] as const;
export type ScaleAdapterDriver = typeof SCALE_ADAPTER_DRIVERS[number];

export const SCALE_ADAPTER_ENVIRONMENT_KEY = 'GLOBALMART_SCALE_ADAPTER';
export const SERIAL_SCALE_ENVIRONMENT_KEYS = Object.freeze({
  portPath: 'GLOBALMART_SCALE_SERIAL_PORT',
  baudRate: 'GLOBALMART_SCALE_SERIAL_BAUD_RATE',
  parserId: 'GLOBALMART_SCALE_SERIAL_PARSER',
} as const);

export interface ScaleAdapterConfiguration {
  driver: ScaleAdapterDriver;
  simulator?: SimulatedScaleAdapterConfig;
  serial?: SerialScaleConfiguration;
}

export interface ScaleAdapterFactoryDependencies {
  /** El proceso main inyecta transporte/parser; la fábrica no importa serialport. */
  createSerialAdapter?: (configuration: SerialScaleConfiguration) => ScaleAdapter;
}

export const readScaleAdapterConfiguration = (
  environment: Readonly<Record<string, string | undefined>>,
): ScaleAdapterConfiguration => {
  const configuredDriver = environment[SCALE_ADAPTER_ENVIRONMENT_KEY]
    ?.trim()
    .toLowerCase() ?? 'simulator';

  if (configuredDriver !== 'simulator' && configuredDriver !== 'serial') {
    throw new Error(
      `${SCALE_ADAPTER_ENVIRONMENT_KEY} debe ser "simulator" o "serial".`,
    );
  }

  if (configuredDriver === 'simulator') return { driver: 'simulator' };

  const baudRate = Number(environment[SERIAL_SCALE_ENVIRONMENT_KEYS.baudRate]);
  const serial = validateSerialScaleConfiguration({
    portPath: environment[SERIAL_SCALE_ENVIRONMENT_KEYS.portPath] ?? '',
    baudRate,
    parserId: environment[SERIAL_SCALE_ENVIRONMENT_KEYS.parserId] ?? '',
  });

  return { driver: 'serial', serial };
};

/** Selecciona el adaptador sin filtrar configuración serial hacia el renderer. */
export const createScaleAdapter = (
  config: ScaleAdapterConfiguration,
  dependencies: ScaleAdapterFactoryDependencies = {},
): ScaleAdapter => {
  if (config.driver === 'simulator') {
    return new SimulatedScaleAdapter(config.simulator);
  }

  if (!dependencies.createSerialAdapter) {
    throw new Error('El adaptador serial está seleccionado pero su proveedor está deshabilitado.');
  }

  if (!config.serial) {
    throw new Error('Falta la configuración privada del adaptador serial.');
  }

  return dependencies.createSerialAdapter(config.serial);
};
