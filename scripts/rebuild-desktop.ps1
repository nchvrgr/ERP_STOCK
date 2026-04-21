$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$winUnpackedDir = Join-Path $projectRoot 'release\win-unpacked'

$processNames = @(
  'Viñedo de la Villa',
  'vinedos-de-la-villa',
  'ERP Stock',
  'ApiWeb',
  'electron'
)

foreach ($name in $processNames) {
  Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force
}

Get-Process -ErrorAction SilentlyContinue |
  Where-Object {
    $_.Path -and $_.Path.StartsWith($winUnpackedDir, [System.StringComparison]::OrdinalIgnoreCase)
  } |
  Stop-Process -Force

Push-Location $projectRoot

try {
  & npm run desktop:pack
  if ($LASTEXITCODE -ne 0) {
    throw "desktop:pack fallo con codigo $LASTEXITCODE"
  }

  & npm run desktop:open
  if ($LASTEXITCODE -ne 0) {
    throw "desktop:open fallo con codigo $LASTEXITCODE"
  }
}
finally {
  Pop-Location
}
