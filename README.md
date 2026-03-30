# ERP_STOCK

## Guia rápida
Para levantar todo por primera vez, ejecuta solo estos 2 comandos desde la raiz del repo:

```powershell
powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1
```

```powershell
cd cliente/pos-ui 
npm run dev
```

Abre:
- Cliente web: `http://localhost:5173/login`
- API health: `http://localhost:8080/api/v1/health`

Login demo:
- usuario: `admin`
- password: `admin`

## Guia detallada

## Estructura del repositorio
- `servidor/`: API, dominio y acceso a datos (.NET)
- `cliente/`: aplicacion web (Vue + Vite)
- `herramientas/`: scripts de automatizacion local
  - `configurar-entorno.ps1`: setup principal en espanol
  - `exportar-semilla-compartida.ps1`: exporta datos compartidos de DB

## 1) Requisitos
- Docker Desktop
- Node.js + npm
- PowerShell (Windows)

## 2) Levantar el proyecto (recomendado)
Desde la raiz del repo:

```powershell
powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1
```

Este comando:
- levanta `postgres` + `pos-api` con Docker
- recrea DB y aplica: `esquema.sql` + `datos-iniciales.sql` + `datos-compartidos.sql`
- configura cliente para usar API en `http://localhost:8080`
- instala dependencias de cliente
- configura hooks de Git (`core.hooksPath=.githooks`)

Importante:
- corre este setup al menos una vez por clon para activar el hook de exportacion de DB.

Luego:

```powershell
cd cliente/pos-ui
npm run dev
```

Abrir:
- Cliente web: `http://localhost:5173/login`
- API health: `http://localhost:8080/api/v1/health`

Login demo:
- usuario: `admin`
- password: `admin`

## 3) Puertos esperados
- Postgres Docker: `5433`
- API Docker: `8080`
- Cliente Vite: `5173`

## Aplicacion de escritorio instalable

Ahora el repo tambien puede generar un instalador Windows `.exe` que:
- instala una sola aplicacion de escritorio
- crea accesos directos de escritorio y menu inicio desde el instalador
- corre frontend + backend dentro de la misma app
- usa una base SQLite local embebida, sin Docker ni PostgreSQL externo
- conserva los datos al cerrar y reabrir la ventana

### Construir el instalador

Desde la raiz del repo:

```powershell
npm.cmd install
npm.cmd run desktop:dist
```

Salida esperada:
- Instalador: `release/ERP Stock Setup 1.0.0.exe`
- App desempaquetada: `release/win-unpacked/`

### Probar cambios sin reinstalar

Para reconstruir la app de escritorio y abrirla sin pasar por el instalador:

```powershell
npm.cmd run desktop:refresh
```

Atajos utiles:

```powershell
npm.cmd run desktop:pack
```

Reconstruye `release/win-unpacked` con los cambios nuevos.

```powershell
npm.cmd run desktop:open
```

Abre la app ya reconstruida desde `release/win-unpacked`.

### Donde se guardan los datos

En modo escritorio, la base local se guarda en:

```text
%APPDATA%\ERP Stock\data\erp-stock.db
```

Eso evita depender de permisos de escritura sobre `Program Files` y mantiene los datos aunque cierres la aplicacion.

### Probar actualizaciones manuales

La app de escritorio consulta por defecto:

```text
https://api.github.com/repos/nchvrgr/ERP_STOCK/releases/latest
```

Para que aparezca el dialogo de actualizacion, el `tag_name` del release debe ser mayor que la version local de `package.json`.

#### Flujo real con GitHub Releases

1. Deja la app local en una version base, por ejemplo `1.0.0`.
2. Genera el instalador:

```powershell
npm.cmd run desktop:dist
```

3. Publica un release en GitHub con un tag mayor, por ejemplo `1.0.1`.
4. Sube como asset el instalador `.exe` generado en `release/`.
5. Abre la app local `1.0.0`.

Resultado esperado:
- si el release tiene menos de 7 dias: dialogo con `Actualizar` y `Mas tarde`
- si el release tiene mas de 7 dias: dialogo obligatorio con `Actualizar ahora`

#### Flujo de prueba local sin depender de GitHub

El updater soporta un modo de prueba por variables de entorno.

Atajos directos:

```powershell
npm.cmd run desktop:test-update:optional
```

```powershell
npm.cmd run desktop:test-update:mandatory
```

Ambos comandos usan la app empaquetada en `release/win-unpacked` y el instalador mas nuevo disponible en `release/`.

Update opcional:

```powershell
$env:ERP_STOCK_UPDATE_TEST_MODE='optional'
$env:ERP_STOCK_UPDATE_TEST_ASSET_PATH=(Resolve-Path '.\release\Viñedos de la Villa Setup 1.0.0.exe')
.\release\win-unpacked\Viñedos de la Villa.exe
```

Update obligatorio:

```powershell
$env:ERP_STOCK_UPDATE_TEST_MODE='mandatory'
$env:ERP_STOCK_UPDATE_TEST_ASSET_PATH=(Resolve-Path '.\release\Viñedos de la Villa Setup 1.0.0.exe')
.\release\win-unpacked\Viñedos de la Villa.exe
```

Variables disponibles:
- `ERP_STOCK_UPDATE_TEST_MODE`: `optional`, `mandatory` u `off`
- `ERP_STOCK_UPDATE_TEST_VERSION`: version simulada del release. Por defecto usa la siguiente patch version
- `ERP_STOCK_UPDATE_TEST_PUBLISHED_AT`: fecha ISO manual si quieres controlar exactamente el corte de 7 dias
- `ERP_STOCK_UPDATE_TEST_ASSET_PATH`: ruta local a un instalador para abrirlo sin descarga
- `ERP_STOCK_UPDATE_TEST_ASSET_URL`: URL remota del instalador si prefieres probar descarga real
- `ERP_STOCK_UPDATE_TEST_ASSET_NAME`: nombre del asset simulado
- `ERP_STOCK_UPDATE_RELEASE_URL`: reemplaza la URL real del endpoint de GitHub

Para limpiar el modo de prueba en la misma terminal:

```powershell
Remove-Item Env:ERP_STOCK_UPDATE_TEST_MODE -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_ASSET_PATH -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_ASSET_URL -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_VERSION -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_PUBLISHED_AT -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_TEST_ASSET_NAME -ErrorAction SilentlyContinue
Remove-Item Env:ERP_STOCK_UPDATE_RELEASE_URL -ErrorAction SilentlyContinue
```

## 4) Flujo diario
Si ya tenes todo instalado y no queres resetear DB:

```powershell
powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1 -ConservarBase
cd cliente/pos-ui
npm run dev
```

## 5) Subir cambios de codigo (programa)
1. Hacer cambios.
2. Commit y push:

```bash
git add .
git commit -m "Descripcion del cambio"
git push
```

## 6) Compartir cambios de DB (automatico)
No hace falta editar `datos-compartidos.sql` a mano.

Con el hook de pre-commit activo, cada `git commit`:
- exporta la DB actual a `servidor/scripts/sql/datos-compartidos.sql`
- agrega ese archivo al commit automaticamente

O sea, para compartir programa + DB:

```bash
git add .
git commit -m "Descripcion del cambio"
git push
```

Nota:
- el `pre-commit` exporta los datos de la DB del contenedor `pos-postgres`.
- si ese contenedor no esta levantado, el commit puede fallar.

Si queres saltear la exportacion de DB en un commit puntual:

```bash
set SKIP_DB_EXPORT=1
git commit -m "Commit sin export de DB"
```

En la maquina que baja esos cambios, para aplicar la DB compartida:

```powershell
powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1
```

Si solo queres actualizar codigo sin resetear DB local:

```powershell
powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1 -ConservarBase
```

## 7) Si algo falla rapido
- Error `ERR_CONNECTION_REFUSED` a `:5072`: frontend apuntando a puerto viejo.
  - Ejecutar de nuevo:
  ```powershell
  powershell -ExecutionPolicy Bypass -File herramientas/configurar-entorno.ps1 -ConservarBase
  ```
- API no responde:
  ```powershell
  docker compose ps
  docker compose logs -f pos-api
  ```
- El commit falla en pre-commit por DB/contenedor:
  - levantar Docker y repetir commit, o
  - usar `SKIP_DB_EXPORT=1` para ese commit puntual

## 8) Control automatico en GitHub (CI)
El repo ahora incluye el workflow:
- `.github/workflows/ci-backend.yml`

Se ejecuta en cada `push` y `pull request`, y valida:
- restore del backend
- chequeo de vulnerabilidades NuGet
- tests backend (`servidor/tests/Pruebas`)

Si alguno falla, el pipeline queda en rojo y no pasa desapercibido.
