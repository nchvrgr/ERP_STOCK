param(
    [switch]$KeepDb,
    [switch]$SkipNpmInstall = $false,
    [int]$ApiWaitSeconds = 90
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Wait-ApiHealth([int]$TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing "http://localhost:8080/api/v1/health" -TimeoutSec 5
            if ($response.StatusCode -eq 200 -and $response.Content -match "ok") {
                return $true
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    return $false
}

Write-Step "Checking required tools"
docker --version | Out-Null
docker compose version | Out-Null
npm --version | Out-Null

Write-Step "Configuring git hooks"
if (Test-Path ".git") {
    git config core.hooksPath .githooks
}

Write-Step "Preparing servidor (Docker)"
if (-not $KeepDb) {
    docker compose down -v
}

docker compose up -d

Write-Step "Waiting for API health check"
if (-not (Wait-ApiHealth -TimeoutSeconds $ApiWaitSeconds)) {
    Write-Host "API did not become healthy in $ApiWaitSeconds seconds." -ForegroundColor Red
    Write-Host "Run: docker compose logs -f pos-api" -ForegroundColor Yellow
    exit 1
}

Write-Step "Configuring cliente env"
$envFile = "cliente/pos-ui/.env.local"
@"
VITE_API_BASE_URL=http://localhost:8080
"@ | Set-Content -Path $envFile -Encoding UTF8

if (-not $SkipNpmInstall) {
    Write-Step "Installing cliente dependencies"
    Push-Location "cliente/pos-ui"
    try {
        npm install
    }
    finally {
        Pop-Location
    }
}

Write-Step "Done"
Write-Host "API servidor: http://localhost:8080/api/v1/health" -ForegroundColor Green
Write-Host "Cliente web:  http://localhost:5173/login" -ForegroundColor Green
Write-Host ""
Write-Host "Next command:" -ForegroundColor Yellow
Write-Host "cd cliente/pos-ui; npm run dev" -ForegroundColor Yellow
