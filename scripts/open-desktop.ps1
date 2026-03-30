$ErrorActionPreference = 'Stop'

$releaseDir = Join-Path $PSScriptRoot '..\release\win-unpacked'
$resolvedExePath = (
  Get-ChildItem -Path $releaseDir -Filter '*.exe' -File |
  Where-Object { $_.Name -notlike 'Uninstall *' } |
  Select-Object -First 1 -ExpandProperty FullName
)

if (-not $resolvedExePath) {
  throw "No se encontro ningun ejecutable en $releaseDir"
}

$processName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedExePath)

Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'vinedos-de-la-villa' -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'ERP Stock' -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'ApiWeb' -ErrorAction SilentlyContinue | Stop-Process -Force

$process = Start-Process -FilePath $resolvedExePath -PassThru

Start-Sleep -Seconds 2

$deadline = (Get-Date).AddSeconds(12)
$visibleWindow = $null

while ((Get-Date) -lt $deadline) {
  $process.Refresh()

  if (-not $process.HasExited -and ($process.MainWindowHandle -ne 0 -or -not [string]::IsNullOrWhiteSpace($process.MainWindowTitle))) {
    $visibleWindow = $process
    break
  }

  if ($process.HasExited) {
    throw "La aplicacion se cerro inmediatamente con codigo $($process.ExitCode)."
  }

  Start-Sleep -Milliseconds 500
}

if (-not $visibleWindow) {
  $alive = $null

  if (-not $process.HasExited) {
    $process.Refresh()
    $alive = [pscustomobject]@{
      ProcessName = $process.ProcessName
      Id = $process.Id
      MainWindowTitle = $process.MainWindowTitle
    }
  }

  if ($alive) {
    throw "La aplicacion quedo iniciada pero no expuso una ventana principal visible."
  }

  throw "No se detecto ninguna ventana principal de la aplicacion despues del arranque."
}
