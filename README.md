# TBTB Contactos de pacientes

Implementacion inicial de la prueba TBTB Global, basada en `02-plan.md`, `api-contract.yaml` y `acceptance-scenarios.md`.

## Estado actual

- API ASP.NET Core `net8.0` con demo-auth por `X-Demo-Actor-Id`.
- Endpoints iniciales de catalogos, pacientes, agenda, contactos, correcciones, historial y health.
- Persistencia real en SQL Server mediante EF Core 8; scripts SQL versionados como fuente de verdad del esquema.
- Arquitectura backend separada en Domain, Application, Infrastructure y Api.
- Angular 21 con Tailwind CSS 4: pacientes, agenda, contactos mensuales, filtros, detalle, historial y correcciones.
- Swagger interactivo, healthchecks y despliegue reproducible con Docker Compose.

## Verificacion local

```powershell
dotnet build api/Tbtb.sln
dotnet test api/Tbtb.sln
$env:ConnectionStrings__Tbtb = "Server=127.0.0.1,1433;Database=Tbtb;User Id=tbtb_app;Password=$env:APP_DB_PASSWORD;Encrypt=True;TrustServerCertificate=True"
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
Cada script aplicado queda registrado con SHA-256 en `dbo.SchemaVersion`; el
arranque falla si se modifica un script ya registrado.

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

## Prueba de aceptacion automatizada

Con el stack levantado, ejecutar contra la API y SQL Server reales:

```powershell
./tests/acceptance-smoke.ps1
```

La prueba cubre alta y consulta de paciente, agenda, contacto, filtros combinados,
correccion con historial y conflicto de concurrencia.

Suite unitaria y perfiles aislados de integracion/E2E:

```powershell
dotnet test api/Tbtb.sln
docker compose -f compose.yaml -f compose.test.yaml down --volumes
docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests tests
docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from e2e e2e
```
