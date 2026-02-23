param(
    [string]$Contenedor = "pos-postgres",
    [string]$UsuarioDb = "pos",
    [string]$NombreDb = "posdb",
    [string]$ArchivoSalida = "servidor/scripts/sql/datos-compartidos.sql"
)

powershell -ExecutionPolicy Bypass -File "herramientas/export-shared-seed.ps1" `
    -Container $Contenedor `
    -DbUser $UsuarioDb `
    -DbName $NombreDb `
    -OutFile $ArchivoSalida
