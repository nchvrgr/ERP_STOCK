const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const https = require('node:https');

const { app, dialog, shell } = require('electron/main');

const packageJson = require('../package.json');

const RELEASES_URL = 'https://api.github.com/repos/nchvrgr/ERP_STOCK/releases/latest';
const SEVEN_DAYS_MS = 7 * 24 * 60 * 60 * 1000;

function normalizeVersion(version) {
  return String(version || '').trim().replace(/^v/i, '');
}

function addDays(date, days) {
  return new Date(date.getTime() + days * 24 * 60 * 60 * 1000);
}

function incrementPatchVersion(version) {
  const parts = normalizeVersion(version).split('.').map((part) => Number.parseInt(part, 10) || 0);

  while (parts.length < 3) {
    parts.push(0);
  }

  parts[2] += 1;
  return parts.join('.');
}

function isNewerVersion(latestVersion, currentVersion) {
  const latest = normalizeVersion(latestVersion).split('.').map((part) => Number.parseInt(part, 10) || 0);
  const current = normalizeVersion(currentVersion).split('.').map((part) => Number.parseInt(part, 10) || 0);
  const length = Math.max(latest.length, current.length);

  for (let index = 0; index < length; index += 1) {
    const latestPart = latest[index] || 0;
    const currentPart = current[index] || 0;

    if (latestPart > currentPart) {
      return true;
    }

    if (latestPart < currentPart) {
      return false;
    }
  }

  return false;
}

function requestJson(url) {
  return new Promise((resolve, reject) => {
    const request = https.get(
      url,
      {
        headers: {
          'Accept': 'application/vnd.github+json',
          'User-Agent': `${packageJson.name}/${packageJson.version}`
        }
      },
      (response) => {
        const chunks = [];

        if (response.statusCode && response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
          response.resume();
          requestJson(response.headers.location).then(resolve).catch(reject);
          return;
        }

        if (!response.statusCode || response.statusCode < 200 || response.statusCode >= 300) {
          response.resume();
          reject(new Error(`GitHub API respondio con estado ${response.statusCode || 'desconocido'}.`));
          return;
        }

        response.on('data', (chunk) => {
          chunks.push(chunk);
        });

        response.on('end', () => {
          try {
            const body = Buffer.concat(chunks).toString('utf8');
            resolve(JSON.parse(body));
          } catch (error) {
            reject(error);
          }
        });
      }
    );

    request.on('error', reject);
    request.setTimeout(15000, () => {
      request.destroy(new Error('Timeout consultando releases de GitHub.'));
    });
  });
}

function getDefaultAssetName(assetSource) {
  if (!assetSource) {
    return process.platform === 'win32' ? 'ERP_STOCK Setup.exe' : 'ERP_STOCK Update.dmg';
  }

  try {
    if (/^https?:\/\//i.test(assetSource) || /^file:\/\//i.test(assetSource)) {
      const parsedUrl = new URL(assetSource);
      const parsedName = path.basename(parsedUrl.pathname);
      return parsedName || getDefaultAssetName();
    }
  } catch {
    return path.basename(assetSource);
  }

  return path.basename(assetSource);
}

function getTestReleaseConfig() {
  const mode = String(process.env.ERP_STOCK_UPDATE_TEST_MODE || '').trim().toLowerCase();
  if (!mode || mode === 'off') {
    return null;
  }

  if (mode !== 'optional' && mode !== 'mandatory') {
    return null;
  }

  const assetPath = String(process.env.ERP_STOCK_UPDATE_TEST_ASSET_PATH || '').trim();
  const assetUrl = String(process.env.ERP_STOCK_UPDATE_TEST_ASSET_URL || '').trim();
  const assetSource = assetPath || assetUrl;
  const defaultPublishedAt = mode === 'mandatory'
    ? addDays(new Date(), -8).toISOString()
    : new Date().toISOString();

  return {
    tag_name: process.env.ERP_STOCK_UPDATE_TEST_VERSION || incrementPatchVersion(packageJson.version),
    published_at: process.env.ERP_STOCK_UPDATE_TEST_PUBLISHED_AT || defaultPublishedAt,
    assets: assetSource
      ? [
          {
            name: process.env.ERP_STOCK_UPDATE_TEST_ASSET_NAME || getDefaultAssetName(assetSource),
            browser_download_url: assetUrl || '',
            local_path: assetPath || ''
          }
        ]
      : []
  };
}

function downloadFile(url, destinationPath) {
  return new Promise((resolve, reject) => {
    const fileStream = fs.createWriteStream(destinationPath);

    const cleanup = (error) => {
      fileStream.close(() => {
        fs.rm(destinationPath, { force: true }, () => {
          reject(error);
        });
      });
    };

    const request = https.get(
      url,
      {
        headers: {
          'Accept': 'application/octet-stream',
          'User-Agent': `${packageJson.name}/${packageJson.version}`
        }
      },
      (response) => {
        if (response.statusCode && response.statusCode >= 300 && response.statusCode < 400 && response.headers.location) {
          response.resume();
          fileStream.close(() => {
            fs.rm(destinationPath, { force: true }, () => {
              downloadFile(response.headers.location, destinationPath).then(resolve).catch(reject);
            });
          });
          return;
        }

        if (!response.statusCode || response.statusCode < 200 || response.statusCode >= 300) {
          response.resume();
          cleanup(new Error(`Descarga fallida con estado ${response.statusCode || 'desconocido'}.`));
          return;
        }

        response.pipe(fileStream);
        fileStream.on('finish', () => {
          fileStream.close(() => resolve(destinationPath));
        });
      }
    );

    request.on('error', cleanup);
    request.setTimeout(30000, () => {
      request.destroy(new Error('Timeout descargando instalador.'));
    });
    fileStream.on('error', cleanup);
  });
}

function getSupportedExtensions() {
  switch (process.platform) {
    case 'win32':
      return ['.exe', '.msi'];
    case 'darwin':
      return ['.dmg', '.zip'];
    default:
      return ['.AppImage', '.deb', '.rpm', '.tar.gz'];
  }
}

function findInstallerAsset(release) {
  const supportedExtensions = getSupportedExtensions();
  const assets = Array.isArray(release?.assets) ? release.assets : [];

  return (
    assets.find((asset) => supportedExtensions.some((extension) => asset.name?.toLowerCase().endsWith(extension.toLowerCase()))) ||
    null
  );
}

function getMandatoryStatus(publishedAt) {
  const publishedDate = new Date(publishedAt);

  if (Number.isNaN(publishedDate.getTime())) {
    return false;
  }

  return Date.now() - publishedDate.getTime() > SEVEN_DAYS_MS;
}

async function promptForUpdate(mainWindow, isMandatory, versionLabel) {
  const options = isMandatory
    ? {
        type: 'warning',
        buttons: ['Actualizar ahora'],
        defaultId: 0,
        cancelId: 0,
        noLink: true,
        noCancel: true,
        title: 'Actualizacion obligatoria',
        message: 'Esta actualizacion es obligatoria para seguir usando ERP_STOCK. Por favor, actualiza ahora.',
        detail: `Hay una nueva version disponible (${versionLabel}).`
      }
    : {
        type: 'info',
        buttons: ['Actualizar', 'Mas tarde'],
        defaultId: 0,
        cancelId: 1,
        noLink: true,
        title: 'Actualizacion disponible',
        message: `Hay una nueva version disponible (${versionLabel}).`,
        detail: 'Se recomienda instalarla ahora.'
      };

  const result = await dialog.showMessageBox(mainWindow || null, options);
  return result.response === 0;
}

async function runInstaller(asset, mainWindow, log) {
  let installerPath = String(asset.local_path || '').trim();

  if (!installerPath) {
    const tempPath = path.join(os.tmpdir(), asset.name || `erp-stock-update-${Date.now()}`);
    await downloadFile(asset.browser_download_url, tempPath);
    installerPath = tempPath;
    log(`installer downloaded path=${installerPath}`);
  } else {
    log(`installer using local path=${installerPath}`);
  }

  const openResult = await shell.openPath(installerPath);
  if (openResult) {
    throw new Error(openResult);
  }

  await dialog.showMessageBox(mainWindow || null, {
    type: 'info',
    buttons: ['Cerrar aplicacion'],
    defaultId: 0,
    noLink: true,
    title: 'Instalador iniciado',
    message: 'El instalador se abrio correctamente.',
    detail: 'La aplicacion se cerrara para completar la actualizacion.'
  });

  app.quit();
}

async function checkForUpdates(options = {}) {
  const log = typeof options.log === 'function' ? options.log : () => {};
  const mainWindow = options.mainWindow || null;

  try {
    const testRelease = getTestReleaseConfig();
    const release = testRelease || await requestJson(process.env.ERP_STOCK_UPDATE_RELEASE_URL || RELEASES_URL);
    const latestVersion = normalizeVersion(release?.tag_name);
    const currentVersion = normalizeVersion(packageJson.version);

    if (testRelease) {
      log(`update check using test mode=${process.env.ERP_STOCK_UPDATE_TEST_MODE}`);
    }

    if (!latestVersion || !isNewerVersion(latestVersion, currentVersion)) {
      return;
    }

    const installerAsset = findInstallerAsset(release);
    if (!installerAsset?.browser_download_url) {
      log(`update skipped: no installer asset found for platform=${process.platform}`);
      return;
    }

    const isMandatory = getMandatoryStatus(release.published_at);
    const accepted = await promptForUpdate(mainWindow, isMandatory, latestVersion);

    if (!accepted) {
      if (isMandatory) {
        app.quit();
      }
      return;
    }

    await runInstaller(installerAsset, mainWindow, log);
  } catch (error) {
    log('update check failed', error);
  }
}

module.exports = {
  checkForUpdates
};
