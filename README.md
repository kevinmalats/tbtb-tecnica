# TBTB Contactos de pacientes

Implementacion inicial de la prueba TBTB Global, basada en `02-plan.md`, `api-contract.yaml` y `acceptance-scenarios.md`.

## Estado actual

- API ASP.NET Core `net8.0` con demo-auth por `X-Demo-Actor-Id`.
- Endpoints iniciales de catalogos, pacientes, agenda, contactos, correcciones, historial y health.
- Almacenamiento en memoria para habilitar el primer flujo ejecutable mientras se integra SQL Server/EF Core.
- Scripts SQL iniciales para esquema, indices, permisos y seed de catalogos/actores.
- Esqueleto Angular por funcionalidades, sin instalacion verificada porque el entorno local tiene Node 22.4.1 y `npm` falla por permisos del perfil.

## Verificacion local

```powershell
dotnet build api/Tbtb.sln
dotnet run --project api/src/Tbtb.Api/Tbtb.Api.csproj
```

Actor de demostracion:

```text
X-Demo-Actor-Id: 11111111-1111-4111-8111-111111111111
```

## Docker Compose

Crear el archivo de variables y levantar el stack:

```powershell
Copy-Item .env.example .env
docker compose up --build -d
docker compose ps
```

La API queda disponible en `http://localhost:8080` y SQL Server en
`localhost:1433`, salvo que se cambien `TBTB_API_PORT` o `TBTB_DB_PORT`.
El servicio efimero `db-init` crea la base `Tbtb` y aplica, en orden, los
scripts de esquema, indices, permisos y datos demo.

Swagger UI queda disponible en `http://localhost:8080/swagger`. Para probar
las rutas `/api/v1`, usar `Authorize` e ingresar uno de los UUID demo, por
ejemplo `11111111-1111-4111-8111-111111111111` para Gestor A.

El frontend Angular se publica en `http://localhost:4200` por defecto. Si el
puerto esta ocupado, cambiar `TBTB_PUBLIC_PORT` en `.env`; el entorno local
verificado durante el desarrollo usa `http://localhost:4201`.

Para desarrollo con recarga de la API:

```powershell
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

Para detener el stack conservando la base:

```powershell
docker compose down
```

Para eliminar tambien el volumen de datos:

```powershell
docker compose down --volumes
```

## Pendiente inmediato

- Reemplazar el store en memoria por infraestructura SQL Server/EF Core o ADO.NET sobre los scripts.
- Agregar pruebas unitarias e integracion contra SQL Server real.
- Completar Angular cuando Node/npm esten alineados con la version requerida.
- Incorporar el frontend compilado al stack cuando la aplicacion Angular este completa.
