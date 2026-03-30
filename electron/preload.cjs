const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('desktopBridge', {
  openTicketPreview: (payload) => ipcRenderer.invoke('ticket-preview:open', payload),
  checkForAppUpdate: () => ipcRenderer.invoke('app:update:check'),
  getAppVersion: () => ipcRenderer.invoke('app:get-version')
});
