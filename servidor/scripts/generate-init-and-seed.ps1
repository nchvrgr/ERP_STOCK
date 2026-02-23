param(
    [string]$Project = "servidor/src/Infraestructura",
    [string]$StartupProject = "servidor/src/ApiWeb",
    [string]$OutDir = "servidor/scripts/sql"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$fullPath = Join-Path $OutDir "modelo-completo.sql"
$initPath = Join-Path $OutDir "esquema.sql"
$seedPath = Join-Path $OutDir "datos-iniciales.sql"
$sharedSeedPath = Join-Path $OutDir "datos-compartidos.sql"
$resetPath = Join-Path $OutDir "reiniciar-esquema.sql"

# Generates SQL for current EF model (DDL + HasData inserts)
dotnet dotnet-ef dbcontext script -p $Project -s $StartupProject --output $fullPath | Out-Null

$full = Get-Content -Raw -Path $fullPath

# Capture INSERT blocks as optional seed
$insertMatches = [regex]::Matches($full, '(?ms)^INSERT INTO\s+.+?;\s*$')
$seedBlocks = @()
foreach ($m in $insertMatches) { $seedBlocks += $m.Value.TrimEnd() }

$seedHeader = @(
    '-- Datos iniciales generados automaticamente desde EF HasData'
    '-- Ejecutar luego de esquema.sql'
    ''
)

if ($seedBlocks.Count -gt 0) {
    ($seedHeader + $seedBlocks) -join "`r`n`r`n" | Set-Content -Path $seedPath -Encoding UTF8
}
elseif (-not (Test-Path $seedPath)) {
    ($seedHeader + '-- No HasData inserts found.') -join "`r`n" | Set-Content -Path $seedPath -Encoding UTF8
}

# Remove inserts from full script -> pure DDL init
$init = [regex]::Replace($full, '(?ms)^INSERT INTO\s+.+?;\s*$', '')
$init = [regex]::Replace($init, '(?m)^[ \t]*\r?\n', '')

$ddlPreamble = @'
-- Extra DDL required by model defaults/custom SQL migrations
CREATE SEQUENCE IF NOT EXISTS venta_numero_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;

'@

Set-Content -Path $initPath -Value ($ddlPreamble + $init) -Encoding UTF8

@"
-- WARNING: drops everything in public schema
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
"@ | Set-Content -Path $resetPath -Encoding UTF8

if (-not (Test-Path $sharedSeedPath)) {
@"
-- Datos compartidos del equipo (versionados en git)
-- Agrega INSERT personalizados para que los nuevos clones tengan los mismos datos demo/test.
-- Se ejecuta automaticamente en docker-compose despues de esquema.sql y datos-iniciales.sql.
"@ | Set-Content -Path $sharedSeedPath -Encoding UTF8
}

Write-Host "Generated:"
Write-Host " - $initPath"
Write-Host " - $seedPath"
Write-Host " - $sharedSeedPath"
Write-Host " - $fullPath"
Write-Host " - $resetPath"
