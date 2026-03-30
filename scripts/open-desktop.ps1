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

Get-Process -Name 'Viñedos de la Villa' -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'ERP Stock' -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'ApiWeb' -ErrorAction SilentlyContinue | Stop-Process -Force

$process = Start-Process -FilePath $resolvedExePath -PassThru

Start-Sleep -Seconds 2

$deadline = (Get-Date).AddSeconds(12)
$visibleWindow = $null

while ((Get-Date) -lt $deadline) {
  $visibleWindow = Get-Process -Name 'Viñedos de la Villa' -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 -or -not [string]::IsNullOrWhiteSpace($_.MainWindowTitle) } |
    Select-Object -First 1

  if ($visibleWindow) {
    break
  }

  if ($process.HasExited) {
    throw "La aplicacion se cerro inmediatamente con codigo $($process.ExitCode)."
  }

  Start-Sleep -Milliseconds 500
}

if (-not $visibleWindow) {
  $alive = Get-Process -Name 'Viñedos de la Villa' -ErrorAction SilentlyContinue |
    Select-Object ProcessName, Id, MainWindowTitle

  if ($alive) {
    throw "La aplicacion quedo iniciada pero no expuso una ventana principal visible."
  }

  throw "No se detecto ninguna ventana principal de la aplicacion despues del arranque."
}
