const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('desktopBridge', {
  openTicketPreview: (payload) => ipcRenderer.invoke('ticket-preview:open', payload),
  checkForAppUpdate: () => ipcRenderer.invoke('app:update:check'),
  installAppUpdate: () => ipcRenderer.invoke('app:update:install'),
  getAppVersion: () => ipcRenderer.invoke('app:get-version')
});
