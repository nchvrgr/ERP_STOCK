param(
    [switch]$ConservarBase,
    [switch]$OmitirNpmInstall = $false,
    [int]$TiempoEsperaApiSegundos = 90
)

$argsSetup = @()
if ($ConservarBase) { $argsSetup += "-KeepDb" }
if ($OmitirNpmInstall) { $argsSetup += "-SkipNpmInstall" }
$argsSetup += "-ApiWaitSeconds"
$argsSetup += $TiempoEsperaApiSegundos

powershell -ExecutionPolicy Bypass -File "herramientas/setup-local.ps1" @argsSetup
