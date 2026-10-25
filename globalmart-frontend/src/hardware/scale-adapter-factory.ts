import type { ScaleAdapter } from './scale-contract';
import {
  SimulatedScaleAdapter,
  type SimulatedScaleAdapterConfig,
} from './simulated-scale-adapter';

export const SCALE_ADAPTER_DRIVERS = ['simulator', 'serial'] as const;
export type ScaleAdapterDriver = typeof SCALE_ADAPTER_DRIVERS[number];

export const SCALE_ADAPTER_ENVIRONMENT_KEY = 'GLOBALMART_SCALE_ADAPTER';

export interface ScaleAdapterConfiguration {
  driver: ScaleAdapterDriver;
  simulator?: SimulatedScaleAdapterConfig;
}

export interface ScaleAdapterFactoryDependencies {
  /** Se inyectará cuando el adaptador serial exista; esta tarea no accede a hardware. */
  createSerialAdapter?: () => ScaleAdapter;
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

  return { driver: configuredDriver };
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
    throw new Error('El adaptador serial está seleccionado pero todavía no está disponible.');
  }

  return dependencies.createSerialAdapter();
};
