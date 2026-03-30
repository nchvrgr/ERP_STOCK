param(
  [ValidateSet('optional', 'mandatory')]
  [string]$Mode = 'optional',
  [string]$InstallerPath = ''
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$releaseDir = Join-Path $root 'release'
$winUnpackedDir = Join-Path $releaseDir 'win-unpacked'
$appExe = Get-ChildItem -Path $winUnpackedDir -Filter '*.exe' -File |
  Where-Object { $_.Name -notlike 'Uninstall *' } |
  Select-Object -First 1 -ExpandProperty FullName

if (-not $appExe) {
  throw "No se encontro la app de escritorio en $winUnpackedDir. Ejecuta primero 'npm run desktop:pack' o 'npm run desktop:dist'."
}

if (-not $InstallerPath) {
  $InstallerPath = Get-ChildItem -Path $releaseDir -Filter '*.exe' -File |
    Where-Object { $_.Name -like '*Setup*' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName
}

if (-not $InstallerPath) {
  throw "No se encontro un instalador en $releaseDir. Ejecuta primero 'npm run desktop:dist'."
}

$resolvedInstallerPath = (Resolve-Path $InstallerPath).Path
$processName = [System.IO.Path]::GetFileNameWithoutExtension($appExe)
$installerProcessName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedInstallerPath)

Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'ApiWeb' -ErrorAction SilentlyContinue | Stop-Process -Force

$env:ERP_STOCK_UPDATE_TEST_MODE = $Mode
$env:ERP_STOCK_UPDATE_TEST_ASSET_PATH = $resolvedInstallerPath
Remove-Item Env:ERP_STOCK_UPDATE_TEST_ASSET_URL -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_RELEASE_URL -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_PUBLISHED_AT -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_ASSET_NAME -ErrorAction SilentlyContinue

$appDir = [System.IO.Path]::GetDirectoryName($appExe)
$cmdArgs = "/c start `"`" /D `"$appDir`" `"$appExe`""
Start-Process -FilePath 'cmd.exe' -ArgumentList $cmdArgs -WindowStyle Hidden | Out-Null

Start-Sleep -Seconds 2

$deadline = (Get-Date).AddSeconds(12)
$visibleWindow = $null

while ((Get-Date) -lt $deadline) {
  $process = Get-Process -Name $processName -ErrorAction SilentlyContinue | Select-Object -First 1
  $installerProcess = Get-Process -Name $installerProcessName -ErrorAction SilentlyContinue | Select-Object -First 1

  if ($installerProcess) {
    $visibleWindow = $installerProcess
    break
  }

  if (-not $process) {
    Start-Sleep -Milliseconds 500
    continue
  }

  if ($process.MainWindowHandle -ne 0 -or -not [string]::IsNullOrWhiteSpace($process.MainWindowTitle)) {
    $visibleWindow = $process
    break
  }

  Start-Sleep -Milliseconds 500
}

if (-not $visibleWindow) {
  $latestProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue | Select-Object -First 1
  $latestInstallerProcess = Get-Process -Name $installerProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($latestInstallerProcess) {
    $visibleWindow = $latestInstallerProcess
  }

  if ($visibleWindow) {
    Write-Host "El instalador ya quedo iniciado por el flujo de update."
    Write-Host "Mode: $Mode"
    Write-Host "App: $appExe"
    Write-Host "Installer: $resolvedInstallerPath"
    return
  }

  if ($latestProcess) {
    throw "La app quedo iniciada pero no expuso una ventana principal visible."
  }

  throw "La app de prueba no quedo en ejecucion despues del arranque."
}

Write-Host "Modo de prueba de update iniciado."
Write-Host "Mode: $Mode"
Write-Host "App: $appExe"
Write-Host "Installer: $resolvedInstallerPath"
