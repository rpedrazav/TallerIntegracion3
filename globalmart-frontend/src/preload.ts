import { contextBridge, ipcRenderer } from 'electron';
import { IniciarOperacionParams, SimulatedAction } from './devices/types';

contextBridge.exposeInMainWorld('api', {
  ping: () => ipcRenderer.invoke('ping'),
  getToken: () => ipcRenderer.invoke('auth:getToken'),
  setToken: (token: string) => ipcRenderer.invoke('auth:setToken', token),
  logout: () => ipcRenderer.invoke('auth:logout'),

  // Canales de periféricos protegidos (S2-088)
  devices: {
    terminal: {
      iniciar: (params: IniciarOperacionParams) => ipcRenderer.invoke('devices:terminal:iniciar', params),
      obtenerEstado: () => ipcRenderer.invoke('devices:terminal:obtenerEstado'),
      cancelar: () => ipcRenderer.invoke('devices:terminal:cancelar'),
      autorizar: (simulacion?: SimulatedAction) => ipcRenderer.invoke('devices:terminal:autorizar', simulacion),
    },
    scale: {
      obtenerPeso: () => ipcRenderer.invoke('devices:scale:obtenerPeso'),
      tarar: () => ipcRenderer.invoke('devices:scale:tarar'),
      simularPeso: (pesoKg: number) => ipcRenderer.invoke('devices:scale:simularPeso', pesoKg),
    },
  },
});
