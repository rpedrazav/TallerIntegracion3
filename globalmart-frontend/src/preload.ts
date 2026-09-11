import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('api', {
  ping: () => ipcRenderer.invoke('ping'),
  getToken: () => ipcRenderer.invoke('auth:getToken'),
  setToken: (token: string) => ipcRenderer.invoke('auth:setToken', token),
  logout: () => ipcRenderer.invoke('auth:logout'),
});
