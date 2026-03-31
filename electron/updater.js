const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const https = require('node:https');
const { spawn } = require('node:child_process');
const { app, dialog, shell } = require('electron/main');

const packageJson = require('../package.json');

const RELEASES_API_BASE_URL = 'https://api.github.com/repos/nchvrgr/ERP_STOCK/releases';
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

function ensureUniqueTempInstallerPath(assetName) {
  const fallbackName = process.platform === 'win32'
    ? `erp-stock-update-${Date.now()}.exe`
    : `erp-stock-update-${Date.now()}`;
  const safeName = String(assetName || fallbackName)
    .replace(/[<>:"/\\|?*\x00-\x1F]/g, '-')
    .replace(/\s+/g, '.')
    .replace(/\.{2,}/g, '.');
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'vinedos-update-'));
  return path.join(tempDir, safeName);
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
  const installers = assets.filter((asset) =>
    supportedExtensions.some((extension) => asset.name?.toLowerCase().endsWith(extension.toLowerCase()))
  );

  if (installers.length === 0) {
    return null;
  }

  installers.sort((left, right) => {
    const leftDate = Date.parse(left.updated_at || left.created_at || 0);
    const rightDate = Date.parse(right.updated_at || right.created_at || 0);

    if (rightDate !== leftDate) {
      return rightDate - leftDate;
    }

    const leftSetup = /setup/i.test(left.name || '');
    const rightSetup = /setup/i.test(right.name || '');
    if (leftSetup !== rightSetup) {
      return rightSetup ? 1 : -1;
    }

    return String(right.name || '').localeCompare(String(left.name || ''));
  });

  return installers[0];
}

function hasUsableInstallerSource(asset) {
  if (!asset) {
    return false;
  }

  const localPath = String(asset.local_path || '').trim();
  const downloadUrl = String(asset.browser_download_url || '').trim();
  return Boolean(localPath || downloadUrl);
}

function getMandatoryStatus(publishedAt) {
  const publishedDate = new Date(publishedAt);

  if (Number.isNaN(publishedDate.getTime())) {
    return false;
  }

  return Date.now() - publishedDate.getTime() > SEVEN_DAYS_MS;
}

async function resolveUpdateState(options = {}) {
  const log = typeof options.log === 'function' ? options.log : () => {};

  const testRelease = getTestReleaseConfig();
  const currentVersion = normalizeVersion(packageJson.version);
  let release = testRelease;

  if (!release) {
    const releaseUrl =
      process.env.ERP_STOCK_UPDATE_RELEASE_URL ||
      `${RELEASES_API_BASE_URL}/latest`;
    release = await requestJson(releaseUrl);
    log(`update check using release url=${releaseUrl}`);
  }

  const latestVersion = normalizeVersion(release?.tag_name);

  if (!latestVersion) {
    log('update unavailable: latest release without valid tag_name');
    return {
      status: 'unavailable',
      currentVersion,
      latestVersion: currentVersion,
      message: 'No se pudo determinar la ultima version publicada.'
    };
  }

  if (testRelease) {
    log(`update check using test mode=${process.env.ERP_STOCK_UPDATE_TEST_MODE}`);
  }

  if (!latestVersion || !isNewerVersion(latestVersion, currentVersion)) {
    log(`update skipped: latest=${latestVersion || 'none'} current=${currentVersion}`);
    return {
      status: 'up-to-date',
      currentVersion,
      latestVersion: latestVersion || currentVersion
    };
  }

  const releaseVersion = normalizeVersion(release?.tag_name);

  const installerAsset = findInstallerAsset(release);
  if (!hasUsableInstallerSource(installerAsset)) {
    log(`update skipped: no usable installer asset found for platform=${process.platform}`);
    return {
      status: 'unavailable',
      currentVersion,
      latestVersion,
      message: 'Hay una nueva version, pero no se encontro un instalador compatible.'
    };
  }

  const isMandatory = getMandatoryStatus(release.published_at);
  log(
    `update available latest=${latestVersion} current=${currentVersion} mandatory=${isMandatory} localAsset=${Boolean(installerAsset.local_path)}`
  );

  return {
    status: 'available',
    currentVersion,
    latestVersion,
    isMandatory,
    installerAsset
  };
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

async function promptForRestartInstall(mainWindow, versionLabel) {
  const result = await dialog.showMessageBox(mainWindow || null, {
    type: 'info',
    buttons: ['Aceptar', 'Cancelar'],
    defaultId: 0,
    cancelId: 1,
    noLink: true,
    title: 'Reiniciar para actualizar',
    message: `Se instalara la version ${versionLabel} y la app se reiniciara automaticamente.`,
    detail: 'Guarda cualquier cambio pendiente antes de continuar.'
  });

  return result.response === 0;
}

function quotePowerShellLiteral(value) {
  return `'${String(value || '').replace(/'/g, "''")}'`;
}

function createWindowsRestartHelper(installerPath, productName, targetVersion, parentPid) {
  const helperPath = path.join(os.tmpdir(), `vinedos-update-restart-${Date.now()}.ps1`);
  const helperScript = [
    "$ErrorActionPreference = 'SilentlyContinue'",
    `$installer = ${quotePowerShellLiteral(installerPath)}`,
    `$productName = ${quotePowerShellLiteral(productName)}`,
    `$targetVersion = ${quotePowerShellLiteral(targetVersion)}`,
    `$parentPid = ${Number.isFinite(parentPid) ? parentPid : -1}`,
    "$logPath = Join-Path $env:TEMP 'vinedos-update-helper.log'",
    'function Write-HelperLog {',
    '  param([string]$Message)',
    '  Add-Content -LiteralPath $logPath -Value ("[{0}] {1}" -f (Get-Date).ToString("o"), $Message)',
    '}',
    'function Resolve-InstalledEntry {',
    '  param([string]$Name)',
    "  return Get-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\*' -ErrorAction SilentlyContinue |",
    '    Where-Object { $_.DisplayName -like ($Name + "*") -and $_.UninstallString } |',
    '    Sort-Object DisplayVersion -Descending |',
    '    Select-Object -First 1',
    '}',
    'function Resolve-InstalledExe {',
    '  param([string]$Name)',
    '  $entry = Resolve-InstalledEntry -Name $Name',
    '  if ($entry) {',
    '    if ($entry.InstallLocation) {',
    '      $candidate = Join-Path $entry.InstallLocation ($Name + ".exe")',
    '      if (Test-Path $candidate) { return $candidate }',
    '    }',
    '    if ($entry.DisplayIcon) {',
    "      $displayIconPath = ([string]$entry.DisplayIcon).Split(',')[0].Trim('\"')",
    '      if ($displayIconPath -and (Test-Path $displayIconPath)) { return $displayIconPath }',
    '    }',
    "    $matches = [regex]::Matches([string]$entry.UninstallString, '\"([^\"]+)\"')",
    '    if ($matches.Count -gt 0) {',
    "      $uninstallPath = $matches[0].Groups[1].Value",
    '      if ($uninstallPath) {',
    '        $candidate = Join-Path (Split-Path -Parent $uninstallPath) ($Name + ".exe")',
    '        if (Test-Path $candidate) { return $candidate }',
    '      }',
    '    }',
    '  }',
    "  $programsDir = Join-Path $env:LOCALAPPDATA 'Programs'",
    '  if (Test-Path $programsDir) {',
    '    $candidate = Get-ChildItem -Path $programsDir -Recurse -Filter ($Name + ".exe") -ErrorAction SilentlyContinue |',
    '      Sort-Object LastWriteTime -Descending |',
    '      Select-Object -First 1 -ExpandProperty FullName',
    '    if ($candidate) { return $candidate }',
    '  }',
    '  return $null',
    '}',
    'function Wait-ForProcessExit {',
    '  param([int]$TargetPid, [int]$TimeoutSeconds)',
    '  if ($TargetPid -lt 1) { return $true }',
    '  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)',
    '  while ((Get-Date) -lt $deadline) {',
    '    $process = Get-Process -Id $TargetPid -ErrorAction SilentlyContinue',
    '    if (-not $process) { return $true }',
    '    Start-Sleep -Milliseconds 500',
    '  }',
    '  return $false',
    '}',
    'function Wait-ForTargetVersion {',
    '  param([string]$Name, [string]$ExpectedVersion, [int]$TimeoutSeconds)',
    '  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)',
    '  while ((Get-Date) -lt $deadline) {',
    '    $entry = Resolve-InstalledEntry -Name $Name',
    '    if ($entry -and [string]$entry.DisplayVersion -eq $ExpectedVersion) {',
    '      return $entry',
    '    }',
    '    Start-Sleep -Seconds 2',
    '  }',
    '  return $null',
    '}',
    'Write-HelperLog "helper start targetVersion=$targetVersion parentPid=$parentPid installer=$installer"',
    '$parentExited = Wait-ForProcessExit -Pid $parentPid -TimeoutSeconds 60',
    'Write-HelperLog "parent exited=$parentExited"',
    'Start-Sleep -Seconds 1',
    "$installerProcess = Start-Process -FilePath $installer -ArgumentList '/S' -PassThru -Wait -WindowStyle Hidden",
    '$installerExitCode = if ($installerProcess) { $installerProcess.ExitCode } else { -1 }',
    'Write-HelperLog "installer exit code=$installerExitCode"',
    'if ($installerExitCode -ne 0) {',
    '  Remove-Item -LiteralPath $PSCommandPath -Force',
    '  exit',
    '}',
    '$installedEntry = Wait-ForTargetVersion -Name $productName -ExpectedVersion $targetVersion -TimeoutSeconds 90',
    'if (-not $installedEntry) {',
    '  Write-HelperLog "target version not detected after install"',
    '  Remove-Item -LiteralPath $PSCommandPath -Force',
    '  exit',
    '}',
    '$installedExe = Resolve-InstalledExe -Name $productName',
    'Write-HelperLog "installed exe=$installedExe version=$($installedEntry.DisplayVersion)"',
    'if ($installedExe -and (Test-Path $installedExe)) {',
    '  Start-Process -FilePath $installedExe | Out-Null',
    '  Write-HelperLog "restarted installed app"',
    '}',
    'Remove-Item -LiteralPath $PSCommandPath -Force'
  ].join('\r\n');

  fs.writeFileSync(helperPath, helperScript);
  return helperPath;
}

async function runInstaller(asset, mainWindow, log, options = {}) {
  let installerPath = String(asset.local_path || '').trim();

  if (!installerPath) {
    const tempPath = ensureUniqueTempInstallerPath(asset.name);
    await downloadFile(asset.browser_download_url, tempPath);
    installerPath = tempPath;
    log(`installer downloaded path=${installerPath}`);
  } else {
    if (!fs.existsSync(installerPath)) {
      throw new Error(`No se encontro el instalador configurado para pruebas: ${installerPath}`);
    }
    log(`installer using local path=${installerPath}`);
  }

  if (!fs.existsSync(installerPath)) {
    throw new Error(`No se encontró el instalador descargado: ${installerPath}`);
  }

  if (options.restartAfterInstall && process.platform === 'win32') {
    const restartAccepted = await promptForRestartInstall(mainWindow, options.versionLabel || 'nueva');
    log(`update restart prompt accepted=${restartAccepted}`);

    if (!restartAccepted) {
      return { cancelled: true };
    }
  }

  const openResult = await shell.openPath(installerPath);
  if (openResult) {
    log(`shell.openPath failed: ${openResult}`);

    if (process.platform !== 'win32') {
      throw new Error(openResult);
    }

    try {
      const cmd = `start "" "${installerPath.replace(/"/g, '""')}"`;
      const child = spawn('cmd.exe', ['/d', '/s', '/c', cmd], {
        detached: true,
        stdio: 'ignore',
        windowsHide: true
      });
      child.unref();
      log(`installer started via cmd fallback path=${installerPath}`);
    } catch (error) {
      log('installer cmd fallback failed', error);
      throw new Error(openResult || (error instanceof Error ? error.message : String(error)));
    }
  }

  log(`installer opened path=${installerPath}`);

  if (typeof options.onInstallStarted === 'function') {
    await options.onInstallStarted();
  }

  // Give the installer enough time to become visible before closing the app.
  setTimeout(() => {
    app.quit();
  }, 1800);

  return { restarting: true };
}

async function checkForUpdates(options = {}) {
  const log = typeof options.log === 'function' ? options.log : () => {};
  const mainWindow = options.mainWindow || null;

  try {
    const updateState = await resolveUpdateState({ log });
    if (updateState.status !== 'available') {
      return;
    }

    const accepted = await promptForUpdate(mainWindow, updateState.isMandatory, updateState.latestVersion);
    log(`update prompt result accepted=${accepted}`);

    if (!accepted) {
      if (updateState.isMandatory) {
        app.quit();
      }
      return;
    }

    await runInstaller(updateState.installerAsset, mainWindow, log, {
      onInstallStarted: options.onInstallStarted
    });
  } catch (error) {
    log('update check failed', error);
    dialog.showErrorBox(
      'Error de actualizacion',
      `No se pudo iniciar el instalador. ${error instanceof Error ? error.message : String(error)}`
    );
  }
}

async function checkForUpdatesOnDemand(options = {}) {
  const log = typeof options.log === 'function' ? options.log : () => {};

  try {
    const updateState = await resolveUpdateState({ log });

    if (updateState.status === 'up-to-date') {
      return {
        status: 'up-to-date',
        currentVersion: updateState.currentVersion,
        latestVersion: updateState.latestVersion,
        message: 'No se encontraron nuevas actualizaciones.'
      };
    }

    if (updateState.status === 'unavailable') {
      return {
        status: 'error',
        currentVersion: updateState.currentVersion,
        latestVersion: updateState.latestVersion,
        message: updateState.message
      };
    }

    return {
      status: 'available',
      currentVersion: updateState.currentVersion,
      latestVersion: updateState.latestVersion,
      isMandatory: updateState.isMandatory,
      message: `Se encontro una nueva actualizacion: v${updateState.latestVersion}.`
    };
  } catch (error) {
    log('update on demand failed', error);
    return {
      status: 'error',
      message: error instanceof Error ? error.message : String(error)
    };
  }
}

async function installLatestUpdateOnDemand(options = {}) {
  const log = typeof options.log === 'function' ? options.log : () => {};
  const mainWindow = options.mainWindow || null;

  try {
    const updateState = await resolveUpdateState({ log });

    if (updateState.status === 'up-to-date') {
      return {
        status: 'up-to-date',
        currentVersion: updateState.currentVersion,
        latestVersion: updateState.latestVersion,
        message: 'No se encontraron nuevas actualizaciones.'
      };
    }

    if (updateState.status === 'unavailable') {
      return {
        status: 'error',
        currentVersion: updateState.currentVersion,
        latestVersion: updateState.latestVersion,
        message: updateState.message
      };
    }

    const installResult = await runInstaller(updateState.installerAsset, mainWindow, log, {
      restartAfterInstall: true,
      versionLabel: updateState.latestVersion,
      onInstallStarted: options.onInstallStarted
    });

    if (installResult?.cancelled) {
      return {
        status: 'cancelled',
        currentVersion: updateState.currentVersion,
        latestVersion: updateState.latestVersion,
        message: 'La actualizacion fue cancelada.'
      };
    }

    return {
      status: 'installing',
      currentVersion: updateState.currentVersion,
      latestVersion: updateState.latestVersion,
      message: `Reiniciando para instalar la version ${updateState.latestVersion}.`
    };
  } catch (error) {
    log('update install on demand failed', error);
    return {
      status: 'error',
      message: error instanceof Error ? error.message : String(error)
    };
  }
}

module.exports = {
  checkForUpdates,
  checkForUpdatesOnDemand,
  installLatestUpdateOnDemand
};
