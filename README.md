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
