const path = require('node:path');
const fs = require('node:fs');
const http = require('node:http');
const os = require('node:os');
const { pathToFileURL } = require('node:url');
const { spawn } = require('node:child_process');
const { checkForUpdatesOnDemand, installLatestUpdateOnDemand } = require('./updater');

const APP_PORT = 18450;
const APP_URL = `http://127.0.0.1:${APP_PORT}`;
const APP_NAME = 'Viñedos de la Villa';
const BOOT_LOG_FILE = path.join(os.tmpdir(), 'vinedos-electron-boot.log');

function logBoot(message, error) {
  const suffix = error
    ? ` ${error instanceof Error ? error.stack || error.message : String(error)}`
    : '';
  fs.appendFileSync(BOOT_LOG_FILE, `[${new Date().toISOString()}] ${message}${suffix}\n`);
}

logBoot('process start');

const { app, BrowserWindow, dialog, ipcMain } = require('electron/main');
let mainWindow = null;
let backendProcess = null;
let isQuitting = false;
let backendLogStream = null;
const gotSingleInstanceLock = app.requestSingleInstanceLock();

if (!gotSingleInstanceLock) {
  logBoot('single-instance-lock rejected');
  logElectron('single-instance-lock rejected, quitting duplicate instance');
  app.quit();
}

function getDataDirectory() {
  const dataDir = path.join(app.getPath('userData'), 'data');
  fs.mkdirSync(dataDir, { recursive: true });
  return dataDir;
}

function getLogDirectory() {
  const logDir = path.join(app.getPath('userData'), 'logs');
  fs.mkdirSync(logDir, { recursive: true });
  return logDir;
}

function logElectron(message, error) {
  logBoot(`electron:${message}`, error);
  const logFile = path.join(getLogDirectory(), 'electron.log');
  const suffix = error
    ? ` ${error instanceof Error ? error.stack || error.message : String(error)}`
    : '';
  fs.appendFileSync(logFile, `[${new Date().toISOString()}] ${message}${suffix}\n`);
}

function getBackendExecutable() {
  return path.join(process.resourcesPath, 'backend', 'ApiWeb.exe');
}

function getWindowIconPath() {
  if (app.isPackaged) {
    return path.join(process.resourcesPath, 'app.asar', 'build', 'icons', 'app.png');
  }

  return path.join(__dirname, '..', 'build', 'icons', 'app.png');
}

function getPreloadPath() {
  return path.join(__dirname, 'preload.cjs');
}

function getPreviewDirectory() {
  const previewDir = path.join(app.getPath('temp'), 'vinedos-ticket-preview');
  fs.mkdirSync(previewDir, { recursive: true });
  return previewDir;
}

function getBuildStamp() {
  try {
    const stampTarget = app.isPackaged
      ? path.join(process.resourcesPath, 'app.asar')
      : path.join(__dirname, '..', 'cliente', 'pos-ui', 'dist', 'index.html');
    return fs.statSync(stampTarget).mtimeMs.toString();
  } catch (error) {
    logElectron('getBuildStamp fallback', error);
    return Date.now().toString();
  }
}

async function clearWindowCache(session) {
  try {
    await session.clearCache();
    await session.clearStorageData({
      storages: ['serviceworkers', 'cachestorage']
    });
    logElectron('window cache cleared');
  } catch (error) {
    logElectron('window cache clear failed', error);
  }
}

function startBackend() {
  const backendExecutable = getBackendExecutable();
  const backendDirectory = path.dirname(backendExecutable);
  const logFile = path.join(getLogDirectory(), 'backend.log');
  logElectron(`startBackend executable=${backendExecutable}`);
  backendLogStream = fs.createWriteStream(logFile, { flags: 'a' });
  backendLogStream.write(`\n[${new Date().toISOString()}] Starting backend\n`);

  backendProcess = spawn(backendExecutable, [], {
    cwd: backendDirectory,
    env: {
      ...process.env,
      ASPNETCORE_URLS: APP_URL,
      DOTNET_ENVIRONMENT: 'Production',
      POS_APP_MODE: 'desktop',
      POS_DATA_DIR: getDataDirectory()
    },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe']
  });

  logElectron(`backend spawned pid=${backendProcess.pid ?? 'unknown'}`);

  backendProcess.stdout.on('data', (chunk) => {
    backendLogStream?.write(chunk);
  });

  backendProcess.stderr.on('data', (chunk) => {
    backendLogStream?.write(chunk);
  });

  backendProcess.on('exit', (code) => {
    logElectron(`backend exited code=${code ?? 'unknown'}`);
    backendLogStream?.write(`[${new Date().toISOString()}] Backend exited with code ${code ?? 'unknown'}\n`);
    backendLogStream?.end();
    backendLogStream = null;
    backendProcess = null;
    if (!isQuitting) {
      dialog.showErrorBox(
        APP_NAME,
        `El servicio interno se cerro inesperadamente. Codigo de salida: ${code ?? 'desconocido'}.`
      );
      app.quit();
    }
  });
}

function waitForBackend(timeoutMs = 30000) {
  const startedAt = Date.now();
  logElectron(`waitForBackend start timeoutMs=${timeoutMs}`);

  return new Promise((resolve, reject) => {
    const probe = () => {
      const request = http.get(`${APP_URL}/api/v1/health`, (response) => {
        response.resume();
        if (response.statusCode && response.statusCode >= 200 && response.statusCode < 500) {
          logElectron(`waitForBackend success status=${response.statusCode}`);
          resolve();
          return;
        }

        retry();
      });

      request.on('error', retry);
      request.setTimeout(2000, () => {
        request.destroy();
        retry();
      });
    };

    const retry = () => {
      if (Date.now() - startedAt > timeoutMs) {
        logElectron('waitForBackend timeout');
        reject(new Error('Timeout esperando el arranque del backend.'));
        return;
      }

      setTimeout(probe, 500);
    };

    probe();
  });
}

async function createWindow() {
  logElectron('createWindow start');
  mainWindow = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 1100,
    minHeight: 720,
    show: false,
    autoHideMenuBar: true,
    title: APP_NAME,
    icon: getWindowIconPath(),
    webPreferences: {
      contextIsolation: true,
      sandbox: true,
      preload: getPreloadPath()
    }
  });

  await clearWindowCache(mainWindow.webContents.session);
  const url = `${APP_URL}?desktopBuild=${encodeURIComponent(getBuildStamp())}`;
  logElectron(`window loadURL ${url}`);
  mainWindow.maximize();
  logElectron('window maximize requested');
  mainWindow.loadURL(url);
  mainWindow.once('ready-to-show', () => {
    logElectron('window ready-to-show');
    if (!mainWindow) {
      return;
    }

    if (!mainWindow.isMaximized()) {
      mainWindow.maximize();
      logElectron('window maximize enforced on ready-to-show');
    }

    mainWindow.show();
    mainWindow.focus();
  });
  mainWindow.webContents.on('did-finish-load', () => {
    logElectron('window did-finish-load');
  });
  mainWindow.webContents.on('did-fail-load', (_, errorCode, errorDescription, validatedURL) => {
    logElectron(`window did-fail-load code=${errorCode} url=${validatedURL} desc=${errorDescription}`);
  });
  mainWindow.webContents.on('render-process-gone', (_, details) => {
    logElectron(`render-process-gone reason=${details.reason} exitCode=${details.exitCode}`);
  });
  mainWindow.on('show', () => {
    logElectron('window show');
  });
  mainWindow.on('closed', () => {
    logElectron('window closed');
    mainWindow = null;
  });
}

async function openTicketPreviewWindow(payload) {
  const html = typeof payload?.html === 'string' ? payload.html.trim() : '';
  if (!html) {
    throw new Error('No se pudo generar la vista previa del ticket.');
  }

  const sourceWindow = new BrowserWindow({
    width: 420,
    height: 760,
    show: false,
    autoHideMenuBar: true,
    webPreferences: {
      contextIsolation: true,
      sandbox: true,
      preload: getPreloadPath()
    }
  });

  try {
    await sourceWindow.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(html)}`);
    const pdfBuffer = await sourceWindow.webContents.printToPDF({
      printBackground: true,
      preferCSSPageSize: true
    });
    const pdfPath = path.join(getPreviewDirectory(), `ticket-preview-${Date.now()}.pdf`);
    fs.writeFileSync(pdfPath, pdfBuffer);

    const previewWindow = new BrowserWindow({
      width: 980,
      height: 760,
      autoHideMenuBar: true,
      title: payload?.title || 'Vista previa de impresion',
      icon: getWindowIconPath(),
      webPreferences: {
        contextIsolation: true,
        sandbox: true,
        preload: getPreloadPath()
      }
    });

    await previewWindow.loadURL(pathToFileURL(pdfPath).toString());
    previewWindow.show();
    previewWindow.focus();
    logElectron(`ticket preview opened path=${pdfPath}`);
    return { ok: true };
  } finally {
    if (!sourceWindow.isDestroyed()) {
      sourceWindow.destroy();
    }
  }
}

function stopBackend() {
  logElectron('stopBackend called');
  if (!backendProcess) {
    backendLogStream?.end();
    backendLogStream = null;
    return;
  }

  backendProcess.kill();
  backendProcess = null;
  backendLogStream?.end();
  backendLogStream = null;
}

app.whenReady().then(async () => {
  logElectron('app.whenReady resolved');
  try {
    startBackend();
    await waitForBackend();
    await createWindow();
  } catch (error) {
    logElectron('app.whenReady failed', error);
    dialog.showErrorBox(APP_NAME, error instanceof Error ? error.message : String(error));
    app.quit();
  }
});

ipcMain.handle('ticket-preview:open', async (_, payload) => {
  try {
    return await openTicketPreviewWindow(payload);
  } catch (error) {
    logElectron('ticket-preview open failed', error);
    throw error;
  }
});

ipcMain.handle('app:update:check', async () => {
  return checkForUpdatesOnDemand({
    mainWindow,
    log: logElectron
  });
});

ipcMain.handle('app:update:install', async () => {
  return installLatestUpdateOnDemand({
    mainWindow,
    log: logElectron
  });
});

ipcMain.handle('app:get-version', async () => {
  return app.getVersion();
});

app.on('before-quit', () => {
  logElectron('before-quit');
  isQuitting = true;
  stopBackend();
});

app.on('window-all-closed', () => {
  logElectron('window-all-closed');
  app.quit();
});

app.on('activate', () => {
  logElectron('activate');
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow().catch((error) => {
      logElectron('activate createWindow failed', error);
    });
  }
});

app.on('second-instance', () => {
  logElectron('second-instance');
  if (!mainWindow) {
    return;
  }

  if (mainWindow.isMinimized()) {
    mainWindow.restore();
  }

  mainWindow.focus();
});

app.on('will-quit', () => {
  logElectron('will-quit');
});

process.on('uncaughtException', (error) => {
  logElectron('uncaughtException', error);
});

process.on('unhandledRejection', (reason) => {
  logElectron('unhandledRejection', reason);
});
