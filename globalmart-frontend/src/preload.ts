import { contextBridge, ipcRenderer } from 'electron';
import type { ScaleRendererApi } from './hardware/scale-contract';
import { SCALE_IPC_CHANNELS } from './hardware/scale-ipc';

const scaleApi: ScaleRendererApi = {
  connect: () => ipcRenderer.invoke(SCALE_IPC_CHANNELS.connect),
  disconnect: () => ipcRenderer.invoke(SCALE_IPC_CHANNELS.disconnect),
  readStableWeight: (options) => ipcRenderer.invoke(
    SCALE_IPC_CHANNELS.readStableWeight,
    options,
  ),
  getState: () => ipcRenderer.invoke(SCALE_IPC_CHANNELS.getState),
};

contextBridge.exposeInMainWorld('api', {
  ping: () => ipcRenderer.invoke('ping'),
  getToken: () => ipcRenderer.invoke('auth:getToken'),
  setToken: (token: string) => ipcRenderer.invoke('auth:setToken', token),
  logout: () => ipcRenderer.invoke('auth:logout'),
  scale: scaleApi,
  devices: {
    terminal: {
      iniciar: (params: any) => ipcRenderer.invoke('devices:terminal:iniciar', params),
      obtenerEstado: () => ipcRenderer.invoke('devices:terminal:obtenerEstado'),
      cancelar: () => ipcRenderer.invoke('devices:terminal:cancelar'),
      autorizar: (action?: any) => ipcRenderer.invoke('devices:terminal:autorizar', action),
    },
    scale: {
      obtenerPeso: () => ipcRenderer.invoke('devices:scale:obtenerPeso'),
      tarar: () => ipcRenderer.invoke('devices:scale:tarar'),
      simularPeso: (peso: number) => ipcRenderer.invoke('devices:scale:simularPeso', peso),
    },
  },
});
