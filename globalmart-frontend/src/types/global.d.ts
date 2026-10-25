import type { ScaleRendererApi } from '../hardware/scale-contract';
import type {
  IniciarOperacionParams,
  OperacionTerminalResult,
  EstadoTerminalResult,
  AutorizacionTerminalResult,
  SimulatedAction,
  ScaleReading,
} from '../devices/types';

export {};

declare global {
  interface Window {
    api: {
      ping: () => Promise<string>;
      getToken: () => Promise<string | null>;
      setToken: (token: string) => Promise<void>;
      logout: () => Promise<void>;
      scale: ScaleRendererApi;
      devices?: {
        terminal: {
          iniciar: (params: IniciarOperacionParams) => Promise<OperacionTerminalResult>;
          obtenerEstado: () => Promise<EstadoTerminalResult>;
          cancelar: () => Promise<OperacionTerminalResult>;
          autorizar: (simulacion?: SimulatedAction) => Promise<AutorizacionTerminalResult>;
        };
        scale: {
          obtenerPeso: () => Promise<ScaleReading>;
          tarar: () => Promise<{ exito: boolean; mensaje?: string }>;
          simularPeso: (pesoKg: number) => Promise<ScaleReading>;
        };
      };
    };
  }
}

