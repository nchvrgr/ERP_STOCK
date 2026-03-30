const { contextBridge, ipcRenderer } = require('electron');
const packageJson = require('../package.json');

contextBridge.exposeInMainWorld('desktopBridge', {
  openTicketPreview: (payload) => ipcRenderer.invoke('ticket-preview:open', payload),
  checkForAppUpdate: () => ipcRenderer.invoke('app:update:check'),
  getAppVersion: () => packageJson.version
});
