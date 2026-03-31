param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$Notes = '',
  [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
  throw "Version invalida: $Version. Usa formato X.Y.Z"
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  throw 'GitHub CLI (gh) no esta instalado o no esta en PATH.'
}

$ghAuth = gh auth status 2>$null
if ($LASTEXITCODE -ne 0) {
  throw 'GitHub CLI no autenticado. Ejecuta: gh auth login'
}

$tag = "v$Version"

# 1) Bump de version en package.json y package-lock.json sin crear tag automatico.
npm version $Version --no-git-tag-version | Out-Null

# 2) Build de instalador para generar .exe, .blockmap y latest.yml.
if (-not $SkipBuild) {
  npx electron-builder --win nsis
}

# 3) Commit + tag + push.
git add package.json package-lock.json electron/main.cjs electron/updater.js cliente/pos-ui/src/pages/LoginPage.vue scripts/publish-desktop-update.ps1 README.md
if ((git diff --cached --name-only).Length -eq 0) {
  throw 'No hay cambios para commitear.'
}

git commit -m "release: $tag"

git tag $tag

git push origin main

git push origin $tag

# 4) Publicar release con assets del updater.
if ([string]::IsNullOrWhiteSpace($Notes)) {
  $Notes = "Desktop update $tag"
}

gh release create $tag `
  "release/Vinedos.de.la.Villa.Setup.exe" `
  "release/Vinedos.de.la.Villa.Setup.exe.blockmap" `
  "release/latest.yml" `
  --title $tag `
  --notes $Notes

Write-Host "Release publicado: $tag" -ForegroundColor Green
