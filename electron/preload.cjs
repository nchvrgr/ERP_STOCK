const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('desktopBridge', {
  openTicketPreview: (payload) => ipcRenderer.invoke('ticket-preview:open', payload),
  checkForAppUpdate: () => ipcRenderer.invoke('app:update:check'),
  installAppUpdate: () => ipcRenderer.invoke('app:update:install'),
  getAppVersion: () => ipcRenderer.invoke('app:get-version')
});

window.addEventListener('keydown', (event) => {
  if (event.key === 'Escape' && !event.defaultPrevented) {
    const customEvent = new Event('pos-escape-handled');
    customEvent.isEscape = true;
    window.dispatchEvent(customEvent);
  }
});
