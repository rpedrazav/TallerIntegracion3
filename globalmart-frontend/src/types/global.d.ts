import type { ScaleRendererApi } from '../hardware/scale-contract';

export {};

declare global {
  interface Window {
    api: {
      ping: () => Promise<string>;
      getToken: () => Promise<string | null>;
      setToken: (token: string) => Promise<void>;
      logout: () => Promise<void>;
      scale: ScaleRendererApi;
    };
  }
}
