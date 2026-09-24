# 02. Plan de solución

**Prueba:** TBTB Global, Desarrollador, versión 2.0, septiembre de 2026.  
**Track:** Connect: C#, ASP.NET Core Web API sobre .NET 8, SQL Server y Angular.  
**Entrada:** `01-hallazgos.md` y enunciado de la prueba, especialmente las partes II-IV y los anexos A y C.  
**Estado:** plan base de la implementacion. El avance y las evidencias ejecutadas se registran en `03-bitacora.md`; los limites CA-3 parcial y CA-5/CA-6 fuera de alcance se conservan.  
**Entorno:** desarrollo y demostración local con Docker Compose.

## 1. Objetivo y alcance cerrado

Construir un flujo completo para registrar un paciente, seleccionarlo para agendar manualmente un seguimiento, registrar un contacto realizado y consultar los contactos del mes mediante filtros por gestor y ciudad. Incluir una corrección de contacto con conservación del original y detección de correcciones simultáneas.

La solución será un monolito modular con arquitectura limpia en backend y separación equivalente de dominio, aplicación, infraestructura y presentación en frontend. La separación se aplicará mediante dependencias explícitas, sin dividir el sistema en microservicios.

| Criterio | Compromiso de esta entrega | Límite del compromiso |
| --- | --- | --- |
| CA-1 | Cobertura prevista completa: registrar paciente y dejarlo seleccionable en una agenda manual mínima, con posibilidad de guardar un seguimiento. | No generar calendarios clínicos automáticos ni enviar recordatorios. La agenda manual evita asumir que guardar un paciente basta para demostrar que se puede agendar. |
| CA-2 | Cobertura prevista completa: registrar un contacto con paciente, fecha, canal y resultado; recuperar lo guardado. | Un contacto realizado no equivale a cumplir un plazo clínico. |
| CA-3 | Cobertura prevista parcial: corregir fecha, canal o resultado, conservar historial y reflejar la revisión vigente en el listado mensual operativo. | El PRD no identifica inequívocamente el reporte. No se declara cobertura completa del reporte mensual de adherencia ni rectificación de entregas al laboratorio. |
| CA-4 | Cobertura prevista completa: consultar el mes en curso y filtrar por gestor y ciudad simultáneamente. | Gestor ejecutor y ciudad histórica del contacto, bajo H-11. |
| CA-5 | Fuera de alcance. | La marca de ilocalizable queda especificada como posible resolución, pero no se calcula ni se muestra. |
| CA-6 | Fuera de alcance. | Falta calendario médico y definición de actividad histórica; no se mostrará un porcentaje ficticio. |

Los estados anteriores son compromisos del plan. La bitácora solo podrá usar “cubierto” después de vincular implementación y pruebas satisfactorias. CA-3 permanecerá parcial aun cuando sus pruebas técnicas pasen, salvo que el PO confirme que el listado operativo satisface el reporte solicitado.

### Exclusiones explícitas

- Autorregistro: requiere resolver la inscripción incompleta y su conversión en paciente.
- Calendario automático, tolerancias clínicas y cálculo de adherencia: no están definidos en el PRD.
- Clasificación de ilocalizabilidad y cierre de reportes: no son necesarios para demostrar la rebanada seleccionada.
- Envío real de llamadas, WhatsApp, correos o notificaciones: registrar un canal no significa integrarse con él.
- Exportaciones al laboratorio, consentimiento y políticas definitivas de retención: falta definición del responsable del programa.
- Inicio de sesión productivo, recuperación de contraseñas y proveedor externo de identidad: la demostración usará actores precargados bajo un modo local explícito.
- Edición, traslado de ciudad, reasignación y baja de pacientes: requerirían historial adicional; los datos de identidad y asignación permanecen estables en esta entrega.
- Administración de catálogos, borrado de contactos, aplicación móvil y funcionamiento sin conexión.
- Despliegue público, infraestructura de alta disponibilidad y optimizaciones que no respondan al volumen del ejercicio.

## 2. Tratamiento de los hallazgos

Los supuestos de `01-hallazgos.md` siguen siendo interpretaciones provisionales. Esta matriz explica cómo se aplicarán o qué falta para resolverlos; una exclusión no equivale a resolver la necesidad del negocio.

| Hallazgo | Decisión y forma de abordarlo | Aplicación y comprobación |
| --- | --- | --- |
| H-01. Autorregistro incompleto | Separar conceptualmente inscripción pendiente de paciente habilitado. La conversión exigiría completar los datos obligatorios. | Diferido. No habrá formulario público ni pacientes incompletos creados por autorregistro. |
| H-02. Plazo de seguimiento | Solicitar calendario, tolerancia y definición de contacto efectivo. Una agenda manual no sustituye una regla médica. | Diferido. Se pueden guardar seguimientos manuales, pero no inferir adherencia a partir de ellos. |
| H-03. Activo al cierre | Un futuro reporte necesitará intervalos efectivos de actividad y reglas para altas, bajas y reactivaciones; no bastará consultar el estado actual. | Diferido. No agregar un indicador de actividad cuyo significado no esté definido. |
| H-04. Resultados | Catálogo provisional: `CONTACTADO`, `SIN_RESPUESTA`, `FALLIDO`. Los tres se admiten en los canales `LLAMADA`, `WHATSAPP`, `CORREO`; el gestor clasifica el intento. | En alcance. Rechazar códigos desconocidos. No derivar resultados a partir de confirmaciones técnicas de entrega. |
| H-05. Tres intentos | Regla futura: ordenar por momento efectivo, cruzar meses y canales, contar ausencia de respuesta, ignorar fallos técnicos y reiniciar con contacto efectivo. Los empates necesitarán una regla estable adicional acordada. | Diferido. Antes de implementarlo, confirmar empates, correcciones retroactivas y el momento de evaluación del estado. |
| H-06. Correcciones | Original y revisiones inmutables, motivo obligatorio, actor y fecha de registro; corrección transaccional con versión esperada. | En alcance técnico. Probar historial, atomicidad y conflicto de concurrencia. |
| H-07. Reporte corregido | La consulta mensual usa la revisión vigente. Una fecha corregida puede mover el contacto de un mes a otro. | CA-3 parcial. Un reporte entregado al laboratorio requeriría una versión o rectificación identificada; ese flujo queda diferido. |
| H-08. Identidad y obligatorios | Identidad única por país emisor, tipo y número normalizado. Ciudad de residencia y su país se mantienen separados del país emisor del documento. Correo opcional. | En alcance. Duplicado: conflicto visible; validar campos y relaciones de catálogo. |
| H-09. Disponibilidad para agenda | Incluir agenda manual mínima: paciente, gestor asignado y fecha futura. No se genera al registrar automáticamente. | En alcance. Registrar paciente y luego guardar un seguimiento seleccionándolo desde la interfaz. |
| H-10. Tiempo | API recibe instantes con offset; guarda UTC. Mes del programa según `America/Bogota`; contacto pasado permitido, futuro rechazado. Separar ocurrencia de captura. | En alcance. Reloj inyectable y pruebas de límites de mes. Inicio de tratamiento es fecha civil, sin conversión horaria. |
| H-11. Filtros históricos | Guardar gestor ejecutor y ciudad del paciente en el contacto. Al no existir cambios de ciudad en esta entrega, se asume que la ciudad registrada también era la del contacto retroactivo. | En alcance, con esa limitación explícita. Filtros combinados por intersección; las correcciones no cambian paciente, gestor ni ciudad. |
| H-12. Roles | Gestor: pacientes asignados y sus contactos. Coordinadora: consulta del programa. Resolver actor en servidor, no desde el cuerpo de cada operación. | Autorización funcional en alcance; identidad local simulada, no autenticación productiva. Probar accesos permitidos y denegados. |
| H-13. Privacidad | Solo pacientes ficticios, sin exportación al patrocinador, sin datos personales en logs. Solicitar las políticas pendientes antes de cualquier uso real. | En alcance de la demo. No se afirma cumplimiento normativo ni se inventan plazos de retención. |
| H-14. Cálculo del porcentaje | Regla futura: pacientes únicos de la misma población al corte, “no aplica” con denominador cero y dos decimales para presentación. El criterio exacto de redondeo se acordará con el PO. | Diferido junto con CA-6. Resolver H-02 y H-03 antes de construir el cálculo. |

## 3. Arquitectura del backend

### Proyectos y dependencias

| Proyecto | Responsabilidad | Dependencias permitidas |
| --- | --- | --- |
| `Tbtb.Domain` | Entidades, valores y reglas propias del contacto, paciente y seguimiento. | Biblioteca estándar; sin ASP.NET Core, SQL ni EF Core. |
| `Tbtb.Application` | Casos de uso, autorización funcional, validación, modelos de entrada/salida internos y puertos. | Domain. |
| `Tbtb.Infrastructure` | EF Core 8, SQL Server, repositorios concretos, consulta mensual y reloj del sistema. | Application y Domain. |
| `Tbtb.Api` | Controladores, DTO HTTP, traducción de errores, actor local y composición por inyección de dependencias. | Application; referencia Infrastructure solo para registrar adaptadores en el arranque. |

El flujo será controlador → caso de uso → dominio/puerto → adaptador SQL. Los controladores no contendrán consultas ni decisiones de negocio. Las entidades EF o de dominio no se devolverán directamente al cliente.

Casos de uso: `RegisterPatient`, `ListPatients`, `ScheduleFollowUp`, `ListFollowUps`, `RegisterContact`, `CorrectContact`, `GetContactHistory`, `ListMonthlyContacts` y `GetCatalogs`.

Puertos concretos por necesidad: `IPatientRepository`, `IFollowUpRepository`, `IContactRepository`, `IMonthlyContactQuery`, `IUnitOfWork`, `ICurrentActor` e `IClock`. El puerto de consulta devuelve una proyección; no expone `IQueryable`. EF Core queda detrás de los adaptadores. No incorporar repositorio genérico, bus de eventos ni biblioteca de mediación para este tamaño de solución.

La guía de [arquitecturas web de Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) respalda la inversión de dependencias hacia el núcleo. Los nombres y la distribución concreta anteriores son decisiones de este ejercicio.

### Corrección de un contacto

1. Cargar contacto y revisión vigente; validar actor y versión esperada.
2. Validar nuevos fecha/canal/resultado y motivo; rechazar una corrección sin cambios.
3. Añadir revisión con número siguiente y actualizar la revisión vigente del contacto en una sola transacción.
4. Detectar una actualización concurrente mediante `rowversion`; ante conflicto, revertir la transacción completa y responder `409 CONTACT_VERSION_CONFLICT`.
5. Leer el listado desde la revisión vigente y el historial desde todas las revisiones ordenadas.

EF Core soporta el control optimista con tokens de concurrencia; se capturará el conflicto para convertirlo en un error del contrato, según la [documentación de concurrencia de EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency). No se reintentará automáticamente una corrección sobre datos que el usuario no revisó.

## 4. Arquitectura del frontend

Angular con componentes standalone, formularios reactivos tipados y rutas por funcionalidad: pacientes, agenda y contactos. El frontend es responsable de interacción y validación de formulario; el backend sigue siendo la autoridad de las reglas y permisos.

La interfaz utilizará **Tailwind CSS 4** como sistema de utilidades visuales, integrado mediante PostCSS. La paleta principal será celeste/cian por su asociación con salud y confianza, complementada con verde, ámbar y coral para estados operativos. Los componentes conservarán accesibilidad, contraste y comportamiento adaptable sin trasladar reglas de negocio a las clases de presentación.

| Capa por funcionalidad | Contenido | Regla de dependencia |
| --- | --- | --- |
| `domain/` | Modelos TypeScript y tipos de valor usados por la interfaz. | Sin Angular, HTTP ni DOM. |
| `application/` | Casos de uso de interacción y puertos de acceso a datos. Clases TypeScript que reciben puertos por constructor. | Domain; sin `HttpClient` ni componentes. |
| `infrastructure/` | Adaptadores HTTP, DTO y mapeos de transporte a modelos. | Implementa puertos de Application; aquí se utiliza `HttpClient`. |
| `presentation/` | Páginas, componentes, formularios y facades de estado de pantalla. | Application y Domain; no importa adaptadores concretos. |

La composición en `app.config.ts` o proveedores de ruta enlaza los puertos con los adaptadores mediante `InjectionToken` y factorías. Las facades son servicios Angular inyectados que exponen carga, resultado, errores y acciones a los componentes; mantienen el estado de UI fuera de los casos de uso puros. La [inyección de dependencias de Angular](https://angular.dev/guide/di) permite registrar y sustituir esas implementaciones.

No crear un framework propio para cada operación: compartir modelos solo cuando dos funcionalidades los necesiten. `core/` contendrá actor local, manejo HTTP y configuración; `shared/`, componentes visuales reutilizados. Ninguna pantalla realizará llamadas HTTP directas.

Estados obligatorios: cargando, vacío, datos disponibles, validación fallida, operación fallida y conflicto de corrección. Se desactiva el envío mientras está pendiente; después de un `409`, se ofrece recargar el contacto y revisar cambios. No reintentar escrituras automáticamente.

## 5. Modelo de datos

SQL Server con scripts SQL numerados como fuente de verdad del esquema. EF Core se usa para acceso a datos, sin una segunda cadena de migraciones ni `EnsureCreated`. Todos los campos son obligatorios salvo los marcados con `?`.

| Entidad | Campos y tipos SQL | Restricciones y relaciones |
| --- | --- | --- |
| `Country` | `Code char(2)`, `Name nvarchar(80)` | PK Code. Semillas CO, PE y EC. |
| `City` | `Id int`, `CountryCode char(2)`, `Name nvarchar(120)` | PK Id; FK Country; unicidad país + nombre. Catálogo de demostración, no inventario nacional completo. |
| `DocumentType` | `Code varchar(20)`, `Name nvarchar(80)` | PK Code. Catálogo provisional `NATIONAL_ID`, `FOREIGN_ID`, `PASSPORT`, sin validaciones nacionales inventadas. |
| `Actor` | `Id uniqueidentifier`, `DisplayName nvarchar(120)`, `Role varchar(20)` | PK Id; rol `GESTOR` o `COORDINADORA`. Precargados, sin contraseñas. |
| `Patient` | `Id uniqueidentifier`, `FullName nvarchar(150)`, `DocumentCountryCode char(2)`, `DocumentTypeCode varchar(20)`, `DocumentNumber nvarchar(40)`, `NormalizedDocumentNumber nvarchar(40)`, `Phone nvarchar(30)`, `Email nvarchar(254)?`, `CityId int`, `TreatmentStartDate date`, `AssignedManagerId uniqueidentifier`, `CreatedAtUtc datetime2(3)`, `CreatedBy uniqueidentifier` | PK Id; FK a catálogos y Actor. UNIQUE país emisor + tipo + número normalizado. Gestor asignado y creador coinciden en el alta local. |
| `FollowUp` | `Id uniqueidentifier`, `PatientId uniqueidentifier`, `ManagerId uniqueidentifier`, `ScheduledAtUtc datetime2(3)`, `CreatedAtUtc datetime2(3)`, `CreatedBy uniqueidentifier` | PK Id; FK Patient y Actor. Agenda manual; sin estado de cumplimiento clínico ni generación automática. |
| `Contact` | `Id uniqueidentifier`, `PatientId uniqueidentifier`, `ManagerId uniqueidentifier`, `CityAtContactId int`, `CurrentRevision int`, `CreatedAtUtc datetime2(3)`, `CreatedBy uniqueidentifier`, `RowVersion rowversion` | PK Id; FK Patient, Actor y City; revisión vigente >= 1. Identidad, paciente, gestor y ciudad son inmutables. |
| `ContactRevision` | `ContactId uniqueidentifier`, `RevisionNumber int`, `OccurredAtUtc datetime2(3)`, `Channel varchar(16)`, `Result varchar(20)`, `CorrectionReason nvarchar(500)?`, `RecordedAtUtc datetime2(3)`, `RecordedBy uniqueidentifier` | PK compuesta ContactId + RevisionNumber; FK Contact y Actor. Primera revisión sin motivo; posteriores con motivo no vacío. CHECK de canal/resultado y revisión >= 1. |
| `SchemaVersion` | `Version varchar(100)`, `Checksum char(64)`, `AppliedAtUtc datetime2(3)` | PK Version. Registro del ejecutor de scripts; rechaza cambios en scripts ya aplicados. |

Relaciones principales: Patient pertenece a City y a un gestor; Patient tiene muchos FollowUp y Contact; Contact tiene muchas ContactRevision y selecciona exactamente una por `CurrentRevision`. La FK de revisiones apunta al contacto. La existencia de la revisión vigente se garantiza en la transacción de escritura y con una prueba de integración; no se introduce una FK circular que complique el alta.

### Validaciones y reglas de escritura

- Nombre, teléfono y documento: recortar espacios externos, rechazar vacío y respetar límites. No convertir el documento a número ni eliminar ceros iniciales. Normalización de identidad: trim y mayúsculas invariantes; conservar puntuación interna para no fusionar identidades por una regla no confirmada.
- Teléfono: entre 7 y 30 caracteres, al menos 7 dígitos y solo dígitos, espacios, `+`, guiones y paréntesis. Es una validación sintáctica provisional, no verificación telefónica nacional.
- Correo opcional: vacío se trata como ausencia; si se informa, validar sintaxis y longitud.
- Fecha de inicio: fecha válida y no posterior al día de negocio actual, como supuesto de pacientes que ya iniciaron tratamiento.
- Agenda: paciente existente y asignado al gestor; instante estrictamente futuro. No tiene cancelación ni reprogramación en esta entrega.
- Contacto: paciente existente y asignado al actor; ocurrencia no futura. No impedir fechas anteriores al inicio del tratamiento, porque el PRD no define esa restricción.
- Corrección: únicamente gestor ejecutor, motivo entre 10 y 500 caracteres, token de versión obligatorio. Las revisiones no se editan ni eliminan desde la aplicación.
- No hay eliminación en cascada de pacientes, contactos o revisiones. El usuario SQL de la API no tendrá permisos para actualizar o borrar revisiones; el ejecutor del esquema usa credenciales distintas.
- La demo no ofrece garantía de inviolabilidad frente a un administrador de base de datos. El alcance es historial preservado por el modelo, los permisos de aplicación y las operaciones expuestas.

### Consulta y posibles índices

La consulta mensual combinará `Contact`, su `ContactRevision` vigente, `Patient`, `Actor` y `City`, proyectando solo los campos de la tabla de resultados. Se filtrará `OccurredAtUtc >= inicioUtc AND OccurredAtUtc < finUtc`, con límites calculados desde el mes de Bogotá. Se añadirán las condiciones de gestor y ciudad solo si están presentes.

Orden estable: fecha efectiva descendente y ContactId descendente. Paginación de 20 filas por defecto, máximo 100. Conteo y lista comparten exactamente los filtros.

Índices iniciales: unicidad de identidad del paciente; PK compuesta de revisiones; índice de revisiones por `(OccurredAtUtc, ContactId)` incluyendo número de revisión, canal y resultado; índice de contactos por `(ManagerId, CityAtContactId, Id)` incluyendo revisión vigente y paciente. El índice temporal puede leer revisiones antiguas que luego se descartan por el join; su conveniencia y el orden de columnas deberán justificarse con la consulta y el plan de ejecución, sin afirmar una mejora no medida. Con 400 pacientes, claridad y corrección pesan más que multiplicar índices.

## 6. Contrato HTTP

Base `/api/v1`. JSON camelCase. Identificadores UUID, fechas civiles `YYYY-MM-DD`, instantes ISO 8601 con `Z` u offset obligatorio. Respuestas de instantes en UTC. El actor proviene del contexto del servidor y no de `managerId` o `createdBy` en el cuerpo.

### Endpoints

| Método y ruta | Entrada | Salida satisfactoria | Errores relevantes |
| --- | --- | --- | --- |
| `GET /catalogs` | Sin cuerpo | `200` países, ciudades, tipos de documento, canales, resultados y gestores visibles al actor. | `401` actor ausente o inválido. |
| `POST /patients` | fullName, documentCountryCode, documentTypeCode, documentNumber, phone, email?, cityId, treatmentStartDate | `201` PatientDto y Location `/api/v1/patients/{id}`. | `400` campos/catálogos, `403` rol, `409 PATIENT_ALREADY_EXISTS`. |
| `GET /patients` | page=1, pageSize=20 | `200 {items,total,page,pageSize}`; gestor ve asignados, coordinadora todos. | `400` paginación, `401`. |
| `GET /patients/{id}` | ID | `200` PatientDto autorizado. | `404` inexistente o no visible. |
| `POST /follow-ups` | patientId, scheduledAt | `201` FollowUpDto, incluyendo gestor resuelto. | `400` fecha, `403` rol, `404` paciente inexistente/no visible. |
| `GET /follow-ups` | patientId obligatorio | `200` lista de seguimientos del paciente, ordenada por fecha. | `400` ID, `404` paciente inexistente/no visible. |
| `POST /contacts` | patientId, occurredAt, channel, result | `201` ContactDto vigente, revisión 1. | `400` campos/fecha, `403` rol, `404` paciente inexistente/no visible. |
| `GET /contacts` | month? en `YYYY-MM`, managerId?, cityId?, page=1, pageSize=20 | `200 {items,total,page,pageSize,month,timeZone}`. Mes omitido: mes actual de negocio. | `400` filtros/mes/paginación, `401`. |
| `GET /contacts/{id}` | ID | `200` ContactDto vigente con token de versión para abrir o recargar una corrección. | `404` inexistente/no visible. |
| `POST /contacts/{id}/corrections` | occurredAt, channel, result, reason, expectedVersion | `200` ContactDto actualizado con nuevo token. | `400` validación/token ausente o inválido/sin cambios, `403` corrección ajena visible, `404` contacto no visible, `409 CONTACT_VERSION_CONFLICT`. |
| `GET /contacts/{id}/history` | ID | `200` revisiones, motivo, autor y captura, de menor a mayor número. | `404` inexistente/no visible. |
| `GET /health/live`, `GET /health/ready` | Sin cuerpo | `200`; ready comprueba conexión y versión requerida de esquema. | Ready: `503` dependencia no disponible. Fuera de `/api/v1`. |

En la consulta mensual un gestor siempre queda restringido a sus propios contactos; pedir otro gestor devuelve `403`. La coordinadora puede combinar cualquier gestor y ciudad válidos. Identificadores de filtros no presentes en catálogos devuelven `400`, no se ignoran.

`PatientDto`: id, fullName, documentCountryCode, documentTypeCode, documentNumber, phone, email, cityId, cityName, treatmentStartDate, assignedManagerId. `FollowUpDto`: id, patientId, managerId, scheduledAt. `ContactDto`: id, patientId, patientName, managerId, managerName, cityId, cityName, occurredAt, channel, result, revision, version. `version` es el `rowversion` codificado en Base64, un token opaco; no es una fecha.

### Errores visibles

Formato Problem Details con `type`, `title`, `status`, `code`, `traceId` y `errors` opcional por campo. No exponer stack traces, SQL ni datos personales. `400`: petición inválida; `401`: falta identidad; `403`: acción no permitida; `404`: recurso ausente/no visible; `409`: duplicado o conflicto; `500`: mensaje genérico y correlación para diagnóstico.

Caso principal: dos ventanas leen el mismo contacto; la primera corrige; la segunda recibe `409` y un mensaje que explica que debe recargar la versión actual. No se pierde la primera corrección ni se guarda parcialmente la segunda. Caso de validación: el servidor rechaza un contacto futuro y la pantalla muestra el mensaje junto a la fecha sin crear el contacto.

### Identidad de demostración

Con `DemoAuthEnabled=true`, un adaptador exclusivo del entorno local acepta `X-Demo-Actor-Id`, comprueba que existe entre los actores sembrados y crea el contexto. La UI ofrece un selector rotulado “Actor de demostración”. Esto permite probar permisos, pero cualquier usuario local puede cambiar de actor: no se presenta como autenticación segura. Fuera del modo local el adaptador queda deshabilitado y la API no admite estas credenciales.

## 7. Pantallas y navegación

| Ruta | Comportamiento y estados |
| --- | --- |
| `/patients` | Lista paginada autorizada y enlace al alta para gestor. Estado vacío y error de carga. |
| `/patients/new` | Formulario tipado, catálogos, validación, prevención de doble envío y mensaje de documento duplicado. Al guardar, navegación al detalle. |
| `/patients/:id` | Datos del paciente, seguimientos manuales y acciones de agendar/registrar contacto para el gestor asignado. |
| `/contacts` | Mes actual por defecto, selector de mes, filtros por gestor y ciudad, tabla paginada. Coordinadora consulta todos; gestor ve los propios. |
| `/contacts/:id/correct` | Formulario con revisión vigente y motivo; preserva token leído y muestra conflicto si cambió. Solo autor habilitado. |
| `/contacts/:id/history` | Original y revisiones con responsable, momento y motivo. Visible para gestor autorizado y coordinadora. |

La agenda y el registro de contacto pueden ser formularios dentro del detalle del paciente, sin crear pantallas adicionales. Los guards sirven para la navegación; los permisos se verifican de nuevo en la API. El listado se denomina “Contactos del mes”, nunca “Reporte de adherencia”.

## 8. Docker y ejecución reproducible

### Versiones base

- Backend: .NET 8 y EF Core 8, exigencia del track.
- Frontend: Angular 21.2.x, Node 22.12 o superior dentro de la rama 22 y TypeScript 5.9.x. Se selecciona una combinación admitida por la [tabla oficial de compatibilidad Angular](https://angular.dev/reference/versions), no una actualización independiente de cada herramienta.
- Base de datos: SQL Server 2022 Developer para el ejercicio local.
- Contenedores Linux, Docker Compose v2 y equipo compatible con la imagen SQL Server seleccionada. Verificar plataforma y memoria antes de construir.

Al crear el proyecto se fijarán parches exactos en `global.json`, referencias NuGet, `package-lock.json` y etiquetas/digests de imágenes. No usar `latest`. Ese registro de versiones es parte de la primera tarea técnica y todavía no está ejecutado.

### Servicios

| Servicio | Construcción/función | Dependencia y exposición |
| --- | --- | --- |
| `db` | Imagen SQL Server; volumen nombrado para datos; healthcheck que ejecuta una consulta. | Red interna, sin publicar 1433 por defecto. Credenciales desde entorno. |
| `migrate` | Ejecutable pequeño que aplica scripts numerados, registra checksum y termina; modo seed explícito para datos ficticios. | Espera `db` saludable. Tiene permisos de DDL. Si falla, la API no arranca. |
| `api` | Dockerfile multietapa: SDK para restaurar/compilar, runtime ASP.NET 8 para ejecutar. | Espera migración satisfactoria. Puerto interno 8080; usuario SQL restringido. |
| `web` | Dockerfile multietapa: Node compila Angular y Nginx sirve archivos y redirige `/api` al servicio `api`. | Publica `127.0.0.1:4200`; fallback de rutas SPA. Espera API lista. |
| `tests` | Perfil `test` con SDK, Node y navegador de prueba según la suite. | Usa base `db-test` aislada y configuración de prueba; no escribe en el volumen de desarrollo. |

Compose usará condiciones `service_healthy` y `service_completed_successfully` para distinguir disponibilidad de la base de finalización de migraciones, de acuerdo con la [documentación de arranque de Docker Compose](https://docs.docker.com/compose/how-tos/startup-order/). La API manejará también fallos de conexión posteriores al arranque; `depends_on` no sustituye ese manejo.

`compose.yaml` permitirá ejecutar la demostración compilada. `compose.dev.yaml` sustituirá los procesos de API y web por `dotnet watch` y servidor Angular accesible dentro del contenedor, con montaje del código y volúmenes separados para dependencias. El navegador consumirá rutas relativas `/api`; el proxy resuelve `api` dentro de la red Docker, no desde el navegador.

### Operación que deberá documentar el README

1. Copiar `.env.example` a `.env` y establecer contraseñas locales para administrador y usuario de aplicación. No versionar `.env` ni secretos en imágenes.
2. Ejecutar `docker compose up --build -d` para migrar, cargar datos de demostración y levantar la aplicación.
3. Abrir `http://localhost:4200`, seleccionar actor de demostración y seguir el recorrido documentado.
4. Para edición con recarga: `docker compose -f compose.yaml -f compose.dev.yaml up --build`.
5. Ejecutar el arnés de pruebas sobre un proyecto Compose aislado, con código de salida distinto de cero ante fallos.
6. `docker compose down` detiene el entorno y conserva datos; documentar por separado el borrado deliberado del volumen, sin hacerlo parte del arranque normal.

Los comandos anteriores son el contrato operativo implementado y verificado. Los puertos son configurables para resolver colisiones y los scripts SQL se ejecutan en `db-init`, no desde cada instancia de la API.

### Esquema y semillas

`scripts/001-schema.sql` creará entidades y restricciones; `002-indexes.sql`, índices; `003-application-permissions.sql`, permisos. Los scripts de esquema serán transaccionales donde SQL Server lo permita, aplicados una sola vez, con bloqueo para impedir dos ejecutores concurrentes. Cambios futuros usarán un nuevo número.

`scripts/seed-demo.sql` contendrá actores y pacientes ficticios con identificadores estables y datos repetibles sin duplicar filas. Recibirá un instante de referencia de demostración para distribuir contactos en el mes actual y el anterior. No borrará correcciones del usuario al reiniciarse. El perfil de pruebas usará su propio seed y reloj fijo, independiente de la fecha del equipo.

## 9. Especificaciones, arneses y pruebas

SDD se aplicará como secuencia de especificación verificable → contrato → ejemplo de aceptación → implementación → evidencia. Cada cambio posterior en una regla actualizará el plan y quedará explicado en bitácora antes de modificar el comportamiento.

| Criterio/objetivo | Pruebas previstas y evidencia |
| --- | --- |
| CA-1 | `CA01_RegisterPatient_MakesPatientAvailableForScheduling`: alta, recuperación y seguimiento guardado. Prueba adicional de identidad duplicada y teléfono obligatorio. Recorrido E2E desde formulario hasta agenda. |
| CA-2 | `CA02_RegisterContact_PersistsPatientDateChannelAndResult`: servicio + integración SQL con lectura posterior. Validar paciente no visible, canal inválido y fecha futura. E2E de alta de contacto. |
| CA-3 parcial | `CA03_Correction_PreservesOriginalAndUpdatesMonthlyList`: dos revisiones y lectura vigente. `CA03_ConcurrentCorrection_ReturnsConflictWithoutPartialWrite`: dos contextos SQL, una corrección ganadora. Probar cambio de mes al corregir fecha. Estos casos no verifican adherencia. |
| CA-4 | `CA04_MonthlyContacts_AppliesManagerAndCityTogether`: incluir fila que cumple ambos, solo gestor, solo ciudad y ninguna. Añadir límites inicio inclusivo/fin exclusivo, revisión obsoleta excluida y orden estable entre páginas. |
| Permisos | Gestor A no opera paciente de B; coordinadora consulta pero no registra ni corrige; ID de actor inexistente rechazado. |
| Error de punta a punta | E2E de conflicto de corrección y validación rechazada por el servidor; mensajes visibles y ausencia de escritura inválida. |
| Reproducibilidad | Base vacía: scripts, semillas, arranque y smoke de navegación. Segundo arranque: no duplica seed y conserva datos. |
| Límites de arquitectura | Revisar referencias de proyectos y ejecutar reglas de lint de imports: dominio/aplicación del frontend no importan Angular o infraestructura; componentes no importan HttpClient. |

Backend: xUnit en Domain/Application e integración contra SQL Server real del perfil de pruebas. No sustituir SQL Server por EF InMemory para concurrencia, restricciones o fechas. Frontend: pruebas de casos de uso/facades con dobles de puertos; Playwright para los recorridos E2E. Fijar sus versiones al configurar el arnés.

Los E2E tendrán pacientes ficticios propios y datos reinicializables en la base de pruebas. Los resultados deberán guardarse con nombre de suite y estado para referenciarlos en `03-bitacora.md`; no se inventarán rutas de evidencias o commits antes de existir.

Puerta de salida: compilación backend/frontend, pruebas ligadas a CA-1/CA-2/CA-4, pruebas de la parte implementada de CA-3, verificación de errores, revisión de dependencias y arranque desde cero. Un criterio comprometido que falle no se declara cubierto.

## 10. Estructura prevista del repositorio

```text
/
  01-hallazgos.md
  02-plan.md
  03-bitacora.md
  README.md
  .env.example
  .gitignore
  compose.yaml
  compose.dev.yaml
  compose.test.yaml
  api/
    Tbtb.sln
    global.json
    Dockerfile
    src/
      Tbtb.Domain/
      Tbtb.Application/
      Tbtb.Infrastructure/
      Tbtb.Api/
    tests/
      Tbtb.UnitTests/
      Tbtb.IntegrationTests/
  web/
    Dockerfile
    nginx.conf
    package-lock.json
    src/app/
      core/
      shared/
      features/
        patients/{domain,application,infrastructure,presentation}/
        follow-ups/{domain,application,infrastructure,presentation}/
        contacts/{domain,application,infrastructure,presentation}/
    e2e/
  scripts/
    001-schema.sql
    002-indexes.sql
    003-application-permissions.sql
    seed-demo.sql
    seed-test.sql
    runner/
  docs/
    acceptance-scenarios.md
    api-contract.yaml
```

Las llaves del árbol representan cuatro carpetas, no nombres literales. No crear archivos vacíos para aparentar arquitectura. `api-contract.yaml` contendrá OpenAPI y deberá coincidir con los endpoints; `acceptance-scenarios.md` ampliará los ejemplos sin redefinir reglas del plan.

## 11. Secuencia de trabajo y presupuesto

La prueba estima 4-6 horas de esfuerzo y fija 24 horas de reloj. El alcance aquí descrito, con arquitectura en ambos lados, agenda, correcciones y Docker, se estima en **7 horas y 20 minutos de preparación, implementación y verificación posteriores a esta documentación, más una hora de margen: 8 horas y 20 minutos en total**. Es una estimación más exigente que la del enunciado, no una promesa de completar en seis horas. La hora del correo y el tiempo ya consumido no se conocen; deberán contrastarse antes de iniciar.

| Orden | Actividad | Resultado comprobable | Tiempo |
| --- | --- | --- | --- |
| 0 | Revisar supuestos y presupuesto; registrar hallazgos y plan en Git | Commit documental anterior a todo código, scaffolding, Docker, SQL y pruebas. | 20 min |
| 1 | Verificar Docker, plataforma y versiones; preparar solución, proyectos y contenedores | Dependencias correctas, imágenes fijadas y SQL saludable. | 50 min |
| 2 | Esquema, ejecutor, permisos, semillas y mapeos EF | Base reproducible y segunda ejecución sin duplicación. | 55 min |
| 3 | Casos de uso, contrato HTTP, identidad local y validación | API del alcance disponible y pruebas unitarias principales. | 80 min |
| 4 | Revisión de contactos y concurrencia | Historial, lectura vigente y conflicto probado con SQL Server. | 45 min |
| 5 | Angular por funcionalidades y composición de dependencias | Registro, agenda, contactos, filtros, corrección e historial navegables. | 90 min |
| 6 | Integración y E2E; límites de fecha, permisos y errores | Pruebas por CA y recorridos completos satisfactorios. | 65 min |
| 7 | Arranque limpio, README y bitácora; repaso para defensa | Pasos reproducibles y matriz con evidencias reales. | 35 min |
| 8 | Margen de resolución de fallos | Corregir problemas sin ampliar funciones. | 60 min |
| | **Total previsto** | **Incluye margen; no incluye el análisis ya realizado.** | **500 min (8 h 20 min)** |

Si el tiempo disponible no alcanza, se modificará explícitamente este plan antes de comenzar o en un commit de cambio de alcance durante el trabajo. Primer recorte posible: retirar correcciones/historial y declarar CA-3 enteramente fuera, conservando CA-1, CA-2 y CA-4. No se eliminarán pruebas para sostener artificialmente el alcance. Ese recorte no está aplicado en la versión actual.

## 12. Riesgos y mitigaciones

| Riesgo | Señal de alerta | Mitigación |
| --- | --- | --- |
| Tiempo superior al esperado | No completar arranque y esquema dentro de las primeras dos horas. | Revisar presupuesto restante, aplicar recorte documentado y priorizar flujo completo. |
| Docker/SQL Server incompatibles con el equipo | Imagen no inicia, arquitectura incompatible o falta de recursos. | Verificación al inicio; registrar impedimento y alternativa del entorno sin cambiar silenciosamente SQL Server. |
| Capas excesivas | Muchos mapeos o abstracciones sin uso concreto. | Casos de uso pequeños, puertos específicos, mapeo manual y un único proceso backend. |
| Pérdida de correcciones | Dos ventanas actualizan el mismo contacto. | Token optimista, transacción y prueba con contextos concurrentes. |
| Interpretación excesiva de CA-3 | Confundir listado con reporte de adherencia. | Mantener estado parcial y explicar el límite en README y defensa. |
| Semillas dependientes del día | Demostración aparece vacía al cambiar de mes. | Seed de demo con referencia explícita y pruebas con reloj fijo. |
| Datos retroactivos de ciudad | Paciente se mudó antes del contacto registrado. | Supuesto visible de ciudad estable para la demo; no afirmar reconstrucción histórica fuera de ese supuesto. |
| Modo de identidad local mal interpretado | Selector de actor presentado como inicio de sesión real. | Etiquetado visible, activación exclusiva local y documentación del límite. |
| Diferencia entre contrato y código | UI espera campos/errores distintos a los que responde la API. | Contrato versionado y pruebas de integración que comprueben DTO y estados HTTP. |
