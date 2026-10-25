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
});
