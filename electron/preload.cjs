const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('desktopBridge', {
  openTicketPreview: (payload) => ipcRenderer.invoke('ticket-preview:open', payload)
});
