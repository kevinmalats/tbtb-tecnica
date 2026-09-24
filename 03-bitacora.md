# 03. Bitacora de desarrollo

## 2026-09-24 05:28 America/Bogota

### Contexto revisado

- Leidos `01-hallazgos.md`, `02-plan.md`, `acceptance-scenarios.md` y `api-contract.yaml`.
- Alcance identificado: CA-1, CA-2 y CA-4 completos; CA-3 solo en la parte operativa de correccion/historial; CA-5 y CA-6 fuera de alcance.
- Contrato base: `/api/v1`, JSON camelCase, demo-auth con `X-Demo-Actor-Id`, Problem Details y catálogos cerrados.

### Desarrollo realizado

- Creada solucion backend `api/Tbtb.sln` con proyecto `api/src/Tbtb.Api/Tbtb.Api.csproj` en `net8.0`.
- Implementada primera API ejecutable en memoria:
  - `GET /health/live` y `GET /health/ready`.
  - `GET /api/v1/catalogs`.
  - `POST/GET /api/v1/patients` y `GET /api/v1/patients/{id}`.
  - `POST/GET /api/v1/follow-ups`.
  - `POST/GET /api/v1/contacts`, `GET /api/v1/contacts/{id}`, `POST /api/v1/contacts/{id}/corrections` y `GET /api/v1/contacts/{id}/history`.
- Agregadas validaciones iniciales para identidad, telefono, email, fechas con offset, canales/resultados, paginacion, permisos por actor y version esperada de correccion.
- Agregados scripts SQL iniciales:
  - `scripts/001-schema.sql`
  - `scripts/002-indexes.sql`
  - `scripts/003-application-permissions.sql`
  - `scripts/seed-demo.sql`
- Agregados archivos operativos iniciales: `.gitignore`, `.env.example`, `compose.yaml`, `compose.dev.yaml`, `compose.test.yaml`, `api/Dockerfile` y `README.md`.
- Creado esqueleto frontend por funcionalidades bajo `web/src/app`, con tipos de dominio iniciales para pacientes y contactos.

### Verificacion

- `dotnet build api/Tbtb.sln` ejecutado correctamente.
- Resultado: compilacion satisfactoria con `0` advertencias y `0` errores.
- Smoke test local con `dotnet run --project api/src/Tbtb.Api/Tbtb.Api.csproj --urls http://127.0.0.1:5097`:
  - `GET /health/live` respondio `200` con `status=healthy`.
  - `GET /api/v1/catalogs` con actor `11111111-1111-4111-8111-111111111111` respondio `200` con catalogos y solo Gestor A en `managers`.

### Limitaciones y pendientes

- La API aun usa almacenamiento en memoria. No se declara cobertura final de criterios de aceptacion hasta conectar persistencia SQL Server y pruebas.
- No se pudo usar `git status` por bloqueo de propiedad dudosa del repositorio entre `Kevin PC` y `CodexSandboxOffline`.
- Primer `dotnet build` en sandbox fallo por permisos sobre `C:\Users\Kevin PC\.dotnet`; se verifico con ejecucion aprobada fuera del sandbox.
- Node local es `v22.4.1`, por debajo del rango previsto para Angular 21.2, y `npm --version` falla por permisos al resolver `C:\Users\Kevin PC`. Por eso el frontend queda como estructura inicial, no compilado.
- Compose todavia no incluye SQL Server ni migrador; queda como punto de arranque de API mientras se completa infraestructura.

## 2026-09-24 - Docker Compose

### Desarrollo realizado

- Completado `compose.yaml` con los servicios `db`, `db-init` y `api`.
- SQL Server 2022 Developer usa un volumen persistente, puerto local configurable y healthcheck real mediante `sqlcmd`.
- Agregado inicializador idempotente `docker/sqlserver/init.sh`; crea la base `Tbtb` y aplica los scripts de esquema, indices, permisos y seed en orden.
- La API espera a que termine correctamente la inicializacion, recibe la cadena `ConnectionStrings__Tbtb` y publica un healthcheck HTTP.
- La imagen de la API incluye `curl` para ejecutar su comprobacion de salud.
- Creado el login SQL `tbtb_app` con permisos `SELECT`, `INSERT` y `UPDATE`; la API ya no recibe credenciales `sa`.
- Ajustados los overrides de desarrollo y pruebas, variables de `.env.example` y comandos operativos del `README.md`.
- Convertido el script de indices a ejecucion idempotente para permitir reinicios del stack sin errores por objetos existentes.
- Agregado `.dockerignore` para impedir que artefactos `bin/obj` generados en Windows contaminen la compilacion Linux de la API.

### Alcance actual

- El contenedor SQL Server queda operativo y provisionado, pero la API sigue usando `DemoStore` en memoria. La conexion de repositorios a `ConnectionStrings:Tbtb` permanece como siguiente desarrollo.
- El frontend no se incluye todavia porque el esqueleto Angular no es ejecutable ni esta verificado.

### Verificacion

- `docker compose config` valido correctamente los manifiestos base, desarrollo y pruebas.
- `docker compose --env-file .env.example up --build -d` construyo la imagen Linux de la API y levanto el stack completo.
- Estado final: `db` saludable, `api` saludable y `db-init` finalizado con codigo `0`.
- Los logs de `db-init` confirman la aplicacion de los cuatro scripts SQL y la carga de datos demo.
- Consulta directa a `Tbtb`: 9 tablas y 3 actores.
- Conexion con `tbtb_app`: lectura correcta de los 3 actores usando credenciales restringidas.
- `GET http://127.0.0.1:8080/health/ready`: `200` con `{"status":"healthy"}`.
- Durante el primer build se detecto que `obj/` local sobrescribia el restore Linux; se resolvio mediante `.dockerignore` y la reconstruccion posterior fue exitosa.

## 2026-09-24 - Swagger UI

### Desarrollo realizado

- Agregado `Swashbuckle.AspNetCore` a la API.
- Habilitada la interfaz interactiva en `/swagger` y el documento OpenAPI generado en `/swagger/v1/swagger.json`.
- Configurado el esquema de seguridad `DemoActor` como API key en el header `X-Demo-Actor-Id`.
- Las operaciones bajo `/api/v1` declaran el actor demo como requerido; los endpoints de health permanecen publicos.
- Documentado en `README.md` el acceso a Swagger y el uso del boton `Authorize` con el UUID de Gestor A.

### Verificacion

- `dotnet build api/Tbtb.sln`: compilacion correcta, 0 advertencias y 0 errores.
- Reconstruido y recreado el contenedor `api` mediante Docker Compose.
- `GET /swagger/index.html`: HTTP 200.
- `GET /swagger/v1/swagger.json`: titulo `TBTB Contactos API`, 10 rutas documentadas y esquema de seguridad asociado a `X-Demo-Actor-Id`.

## 2026-09-24 - Frontend Angular y Tailwind

### Desarrollo realizado

- Convertido el esqueleto `web/` en una aplicacion Angular 21 standalone ejecutable.
- Integrado Tailwind CSS 4 mediante PostCSS y documentada la decision visual en `02-plan.md`.
- Implementada una interfaz responsiva para salud con paleta cian/celeste, estados verde, ambar y coral, e iconos del paquete oficial `@lucide/angular`.
- Implementados resumen operativo, selector de actor demo, listado/busqueda/registro de pacientes y listado/registro mensual de contactos.
- Agregado `ApiService` con envio automatico de `X-Demo-Actor-Id` y consumo de rutas relativas `/api/v1`.
- Agregados configuracion Angular/TypeScript, estilos globales, `package-lock.json`, Dockerfile multietapa Node/Nginx y proxy Nginx hacia `api:8080`.
- Agregado servicio `web` a Compose, con healthcheck, dependencia de API saludable y override para desarrollo con recarga.
- La compilacion usa Node 22.14 dentro de Docker y `npm ci`; no depende del Node 22.4 instalado en el host.

### Verificacion

- `docker compose config`: configuracion valida con el servicio `web`.
- `docker compose build web`: compilacion Angular de produccion exitosa; bundle inicial 288.55 kB (65.28 kB estimados transferidos).
- SPA en `http://127.0.0.1:4201`: HTTP 200 y elemento raiz Angular presente.
- Healthcheck Nginx `/healthz`: respuesta `healthy`.
- Ajustado el healthcheck interno de Nginx a `127.0.0.1` para evitar la resolucion IPv6 de `localhost` en Alpine.
- Proxy web `/api/v1/catalogs`: respuesta correcta con 3 paises y el gestor autorizado.
- Revision visual en viewport movil: navegacion, metricas y contenido sin solapamientos; se muestran los 2 pacientes visibles para Gestor A.
- El puerto 4200 estaba ocupado en el equipo; se creo `.env` local ignorado por Git con `TBTB_PUBLIC_PORT=4201` sin detener el proceso existente.

### Pendientes

- Completar agenda de seguimientos, correccion/historial de contactos y pruebas automatizadas de componentes.
- `npm audit` reporta vulnerabilidades transitivas en el arbol actual de Angular tooling; revisar actualizaciones compatibles sin aplicar `--force` ni cambios mayores automaticos.

## 2026-09-24 - Etiquetas de tipos de documento

### Correccion realizada

- Separados los codigos internos del contrato de sus etiquetas visibles.
- El catalogo de tipos de documento ahora muestra `DNI`, `Documento extranjero` y `Pasaporte`, conservando los codigos `NATIONAL_ID`, `FOREIGN_ID` y `PASSPORT` para las solicitudes API.
- Actualizado el seed SQL con `WHEN MATCHED` para corregir tambien instalaciones existentes.
- La tabla de pacientes resuelve el nombre del catalogo y cuenta con etiquetas de respaldo, evitando mostrar codigos tecnicos si el catalogo no esta disponible temporalmente.

### Verificacion

- API via proxy web: `NATIONAL_ID -> DNI`, `FOREIGN_ID -> Documento extranjero`, `PASSPORT -> Pasaporte`.
- SQL Server: los tres registros de `dbo.DocumentType` quedaron actualizados con las mismas etiquetas.
- Compilacion Angular y publicacion Docker completadas correctamente.
- Servicios `api` y `web` saludables despues del despliegue.

## 2026-09-24 - Persistencia real y separacion del backend

### Desarrollo realizado

- Separada la solucion en proyectos `Domain`, `Application`, `Infrastructure` y `Api`.
- Incorporado EF Core 8 con mapeos para catalogos, actores, pacientes, seguimientos, contactos, revisiones y `rowversion`.
- La disponibilidad `/health/ready` valida ahora la conexion real con SQL Server.
- Migrados los endpoints desde el almacen en memoria hacia consultas y escrituras persistentes en SQL Server.
- Conservadas las reglas de visibilidad por gestor/coordinadora, historial inmutable y concurrencia optimista al corregir contactos.
- Agregados cuatro pacientes demo al seed idempotente para mantener utilizable la aplicacion despues de recrear la base.
- Actualizado el Dockerfile de la API para restaurar y compilar los cuatro proyectos.

### Verificacion

- `dotnet build api/Tbtb.sln --no-restore`: compilacion correcta, 0 advertencias y 0 errores.

### Siguiente bloque

- Reconstruir Compose y ejecutar pruebas HTTP de persistencia y autorizacion.
- Incorporar pruebas automatizadas de backend y completar agenda, detalle, filtros, correcciones e historial en Angular.

## 2026-09-24 - Cierre funcional, pruebas y operacion reproducible

### Backend y contrato

- Eliminado el almacenamiento en memoria: todos los casos escriben y consultan SQL Server mediante EF Core 8.
- Separados `Domain`, `Application`, `Infrastructure` y `Api`; `Domain` no depende de frameworks y `Application` ya no referencia EF Core.
- Movidas a Application las reglas puras de canales, resultados, instantes ISO con offset y tokens `rowversion`.
- Endurecidas las validaciones de paciente, agenda, contacto, correccion, paginacion, mes, gestor y ciudad.
- Rechazados campos JSON desconocidos; agregados errores `application/problem+json`, conflicto por identidad concurrente y `409 CONTACT_VERSION_CONFLICT`.
- `/health/ready` comprueba conexion y esquema utilizable; el modo de actor demo queda deshabilitado por defecto y Compose lo habilita explicitamente para la demo local.
- Swagger conserva el esquema `DemoActor` y lo aplica a las operaciones documentadas.

### Frontend Angular

- Implementada agenda manual con seleccion de paciente, fecha futura, alta y consulta de seguimientos.
- Agregados filtros simultaneos de contactos por mes, gestor y ciudad.
- Agregados detalle, historial de revisiones y formulario de correccion con motivo y version esperada.
- Agregadas rutas Angular reales para `/patients`, `/patients/new`, `/agenda`, `/contacts` y `/contacts/:id`.
- Agregado detalle navegable `/patients/:id`, recuperable tras recargar la pagina, con identidad, datos de contacto y accesos a agenda/contacto.
- El conflicto conserva los valores, muestra una accion `Recargar contacto` y no reintenta una escritura automaticamente.
- Sustituida la tasa porcentual no definida por un conteo factual de contactos efectivos, respetando la exclusion de CA-6.
- El resumen obtiene el conteo real de seguimientos futuros en vez de mostrar un valor fijo.
- Compilacion Angular de produccion verificada en Docker: bundle inicial 387.14 kB, 86.54 kB estimados transferidos.

### Automatizacion y evidencias

- `dotnet build api/Tbtb.sln --no-restore`: 0 advertencias, 0 errores.
- `dotnet test api/Tbtb.sln --no-restore`: 12 pruebas xUnit aprobadas sobre reglas de CA-2/CA-3.
- `tests/acceptance-smoke.ps1`: aprobados CAT-01, CA01-01, CA02-01, CA04-03, CA03-01 y CA03-02 contra API y SQL Server reales.
- Perfil aislado `tbtb-test`: arnes Compose aprobado con health, esquema, catalogos, aislamiento por gestor y error contractual de mes invalido.
- Agregado `seed-test.sql` con fixture determinista C1-C8; desde volumen limpio se comprobaron total 6 para septiembre, limites inclusivo/exclusivo y ausencia de C5/C6.
- Playwright 1.55 + Chromium: 1 E2E aprobado y repetible, creando datos unicos por API y verificando agenda, contactos, detalle, historial, conflicto real entre dos clientes, conservacion del formulario y accion de recarga.
- Revision visual adicional en navegador: agenda y modal responsivos, sin solapamientos en viewport estrecho.
- Reinicio de la API verificado sin perdida de pacientes; `api`, `db` y `web` saludables despues del reinicio.

### Base de datos y seguridad local

- `dbo.SchemaVersion` registra SHA-256 de cada script; el inicializador omite versiones ya aplicadas y falla ante cambios de checksum.
- Agregado `004-demo-contacts.sql`: la demo recibe contactos ficticios del mes de referencia y del anterior; el perfil de pruebas lo sustituye por su fixture fijo.
- Validado reinicio idempotente: los cuatro scripts fueron reconocidos como ya aplicados.
- El usuario `tbtb_app` conserva lectura/insercion/actualizacion necesarias, con `DENY UPDATE, DELETE` sobre `ContactRevision`.
- Retirada la contraseña local de `appsettings.json`; las credenciales se suministran por variables de entorno ignoradas por Git.
- Agregados `global.json` y reloj `IClock` inyectable; la imagen de compilacion conserva SDK .NET 8.0.415 y el host puede avanzar a un SDK compatible instalado.

### Cobertura declarada

- CA-1, CA-2 y CA-4: recorridos principales implementados y automatizados.
- CA-3: cubierta la correccion operativa, historial y concurrencia; permanece parcial respecto al reporte externo no definido.
- CA-5 y CA-6: continúan fuera de alcance según el plan; no se fabricaron reglas clinicas ni porcentajes de adherencia.
- Riesgo restante conocido: `npm audit` informa 22 vulnerabilidades transitivas (2 bajas, 8 moderadas y 12 altas) en tooling Angular/Playwright; no se aplico `--force` porque implicaria cambios mayores no revisados.

## 2026-09-24 - Formularios reactivos y regresion E2E

- Migrados los formularios de paciente, seguimiento, contacto y correccion desde `NgForm`/`ngModel` a formularios reactivos tipados de Angular.
- Incorporados validadores de longitud, correo, campos obligatorios, ciudad y motivo de correccion en la capa de presentacion; el backend continua como autoridad final.
- La primera ejecucion E2E detecto que el nombre de paciente no era una accion accesible para abrir el detalle. Se corrigio con un boton semantico y se repitio el recorrido completo.
- Compilacion Angular de produccion aprobada en Docker: bundle inicial 393.75 kB, 87.81 kB estimados transferidos.
- Playwright 1.55 + Chromium aprobado nuevamente: 1 prueba, 1.9 s de recorrido y 2.7 s totales.
