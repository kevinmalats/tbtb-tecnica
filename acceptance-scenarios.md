# Escenarios de aceptación

**Versión:** 0.1.0 · **Fecha:** 2026-09-24.  
**Referencias:** `../01-hallazgos.md`, `../02-plan.md` y `api-contract.yaml`.  
**Estado:** especificaciones previas al desarrollo. Todos los escenarios están pendientes de implementación y ejecución; no constituyen evidencia de pruebas aprobadas.

## 1. Alcance y forma de uso

Se especifican CA-1, CA-2 y CA-4, más la parte operativa de CA-3 prevista en el plan. CA-3 permanece parcial: modificar la consulta de contactos no verifica un reporte de adherencia ni la rectificación de un informe enviado al patrocinador. CA-5 y CA-6 no tienen escenarios implementables en esta entrega.

Cada ID debe conservarse en el nombre o metadatos de las pruebas y en la bitácora. “Integración” significa ejecución contra SQL Server real y la API; “E2E” significa interacción en Angular con persistencia real de prueba. Los escenarios parametrizados representan varios casos, no una única aserción genérica.

Los supuestos de negocio siguen sujetos a respuesta del PO. Este documento concreta su comportamiento observable; no los convierte en requisitos confirmados por TBTB.

## 2. Datos y convenciones comunes

- Reloj fijo general: `2026-09-24T15:00:00Z`, equivalente a las 10:00 de Bogotá. Solo los escenarios que lo indican cambian este reloj.
- Instantes: ISO 8601 con `Z` u offset, hasta tres decimales. Salida UTC. `now` se compara con precisión de milisegundos. Fecha de inicio de tratamiento: fecha civil.
- Gestor A: `11111111-1111-4111-8111-111111111111`; gestor B: `22222222-2222-4222-8222-222222222222`; coordinadora: `33333333-3333-4333-8333-333333333333`.
- Ciudad 1: Bogotá, CO; ciudad 2: Lima, PE. País emisor del documento y país de residencia son datos distintos.
- P-A: `aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1`, asignado a A, ciudad 1, documento CO/NATIONAL_ID/001234. P-B termina en `aaa2`, asignado a B, ciudad 1. P-C termina en `aaa3`, asignado a A, ciudad 2. P-D termina en `aaa4`, asignado a B, ciudad 2. Todos son ficticios, con inicio de tratamiento `2026-08-01`.
- Paciente nuevo de ejemplo: nombre `Paciente de prueba nuevo`, documento CO/NATIONAL_ID/009999, teléfono `+57 300 000 0000`, correo omitido, ciudad 1, inicio `2026-09-01`.
- Cada escenario comienza con una base aislada o restaura sus fixtures; los cambios de un escenario no preparan accidentalmente el siguiente.
- Versiones de ejemplo: `AAAAAAAAAAE=` y `AAAAAAAAAAI=`. En ejecución deben usarse los tokens devueltos por SQL Server; no exigir sus valores literales ni incremento de uno en uno.
- Para errores, comprobar estado HTTP, `application/problem+json`, `status`, `code` y `traceId` no vacío. Con `VALIDATION_ERROR`, exigir `errors` por campo; `$` identifica errores del cuerpo JSON. No comparar mensajes completos salvo textos de UI relevantes.

## 3. CA-1: paciente disponible para seguimiento

| ID | Dado | Cuando | Entonces | Verificación |
| --- | --- | --- | --- | --- |
| CA01-01 | Gestor A y documento nuevo válido. | Registra paciente desde `/patients/new`, abre su detalle y agenda `2026-09-25T10:00:00-05:00`. | Alta `201` con Location recuperable; gestor asignado A; correo `null`. El paciente aparece en lista y detalle; agenda `201` y consulta posterior con `scheduledAt=2026-09-25T15:00:00Z`. No existe agenda antes de solicitarla. | Servicio, integración y E2E; nombre base `CA01_RegisterPatient_MakesPatientAvailableForScheduling`. |
| CA01-02 | Existe CO/PASSPORT/AB001, sin depender de que el actor pueda ver su ficha. | Se registra CO/PASSPORT/` ab001 `; adicionalmente se prueban PE/PASSPORT/AB001 y CO/NATIONAL_ID/AB001. | Primera petición `409 PATIENT_ALREADY_EXISTS` y no crea fila; no revela datos del existente. Las otras dos identidades son distintas y permiten alta. `001234` no se convierte en `1234`. | Servicio e integración; repetir el duplicado con dos solicitudes concurrentes: una alta y un conflicto. |
| CA01-03 | Alta nueva y datos válidos salvo el campo probado. | Parametrizar nombre vacío, documento vacío, teléfono omitido/null/con menos de siete dígitos/con letras, correo no vacío inválido, ciudad desconocida y fecha de inicio futura. | `400 VALIDATION_ERROR`, campo identificado y cero pacientes nuevos. Email omitido, null, vacío o espacios sí permite alta y devuelve null. Campos por encima del máximo fallan; límites válidos se admiten. | Servicio e integración; E2E de teléfono obligatorio y duplicado visible. |
| CA01-04 | Gestor A y ciudad de Lima existente. | Registra paciente con documento emitido en CO y residencia en ciudad 2. | Alta `201`; conserva país emisor CO y ciudad Lima. No exige que ambos países coincidan. | Integración. |
| CA01-05 | P-A visible y reloj general. | Agenda a `15:00:00Z`, un milisegundo antes y un milisegundo después de now. | Los primeros dos casos: `400` sobre scheduledAt sin fila. El tercero: `201`. Sin offset o con fecha inválida: `400`. | Servicio e integración. |

## 4. CA-2: registrar un contacto

| ID | Dado | Cuando | Entonces | Verificación |
| --- | --- | --- | --- | --- |
| CA02-01 | Gestor A, P-A y ningún contacto del escenario. | Envía patientId de P-A, occurredAt `2026-09-10T10:00:00-05:00`, LLAMADA y SIN_RESPUESTA. | `201`; lectura por ID con fecha `2026-09-10T15:00:00Z`, paciente correcto, gestor A, ciudad 1, revisión 1 y token válido. Existe un contacto y una revisión; motivo inicial null y captura según reloj del servidor. | Servicio, integración y E2E; `CA02_RegisterContact_PersistsPatientDateChannelAndResult`. |
| CA02-02 | P-A y reloj general. | Se envía un contacto exactamente en now, un milisegundo posterior y otro anterior al inicio del tratamiento. | En now y anterior al tratamiento: `201`. Futuro: `400 VALIDATION_ERROR` sobre occurredAt y sin contacto/revisión. | Servicio e integración; la validación se verifica en servidor aunque la UI valide antes. |
| CA02-03 | P-A visible. | Se envían las nueve combinaciones de tres canales y tres resultados. Después se prueban canal SMS, resultado DESCONOCIDO y valores null. | Las nueve combinaciones válidas se conservan como enviadas. Las inválidas: `400`, sin escritura. FALLIDO no se transforma en SIN_RESPUESTA. | Prueba parametrizada de servicio e integración. |
| CA02-04 | P-A visible. | Envía occurredAt sin offset, fecha inexistente o cuatro decimales de segundo. | `400 VALIDATION_ERROR`. El mismo instante expresado en Z y en -05:00 se normaliza igual. No se trunca precisión extra silenciosamente. | Contrato e integración. |

## 5. CA-3: corrección operativa con historial, cobertura parcial

Contacto inicial de esta sección: P-A, A, ciudad 1, LLAMADA, SIN_RESPUESTA, ocurrido el `2026-09-10T15:00:00Z`. Cada escenario restaura ese contacto y su revisión 1.

| ID | Dado | Cuando | Entonces | Verificación |
| --- | --- | --- | --- | --- |
| CA03-01 | A abre contacto y conserva su token. | Corrige resultado a CONTACTADO con motivo `Se verificó el resultado de la llamada.` y versión vigente. | `200`, revisión 2 y token distinto. Listado mensual muestra una sola fila con CONTACTADO; historial contiene original y corrección, con autor, fecha de captura y motivo. Revisión 1 permanece idéntica. | Servicio, integración y E2E; `CA03_Correction_PreservesOriginalAndUpdatesMonthlyList`. |
| CA03-02 | Dos clientes leen la misma versión. | Cliente 1 corrige a CONTACTADO; cliente 2 intenta FALLIDO con el token anterior. Repetir con dos escrituras realmente concurrentes desde contextos distintos. | Una corrección gana; la otra recibe `409 CONTACT_VERSION_CONFLICT`. Solo dos revisiones totales, una vigente, sin fila huérfana ni revisión parcial. UI conserva el mensaje y ofrece recargar, sin reintentar automáticamente. Tras recargar, una nueva corrección deliberada con token actual sí puede guardar. | Integración con SQL Server y E2E de dos ventanas; `CA03_ConcurrentCorrection_ReturnsConflictWithoutPartialWrite`. |
| CA03-03 | Contacto de septiembre y versión vigente. | Corrige occurredAt a `2026-08-31T23:59:59.999-05:00`. | Agosto incluye el contacto y septiembre lo excluye; el historial conserva ambas fechas. La revisión anterior no genera una segunda fila ni aumenta el total de septiembre. | Integración y consulta UI. |
| CA03-04 | Contacto visible y versión vigente. | Parametrizar motivo vacío/9 caracteres/501 caracteres, token ausente/Base64 inválido/longitud decodificada diferente de 8 bytes, y corrección sin cambiar fecha/canal/resultado. | Validaciones: `400 VALIDATION_ERROR`; sin cambios con token actual: `400 NO_CHANGES`. Mismo instante con otro offset tampoco es cambio. Token obsoleto bien formado se trata como `409`, antes de evaluar ausencia de cambios. No aumenta el historial. | Servicio e integración. |

La corrección no permite cambiar patientId, managerId ni cityId. La inclusión de estos campos no se ignora: corresponde al rechazo de campos desconocidos de HTTP-01.

## 6. CA-4: consulta mensual y filtros

Fixture exclusivo de esta sección: reloj `2026-10-01T06:00:00Z`, ya iniciado octubre en Bogotá. Todos los contactos siguientes tienen revisión 1, fecha de captura no anterior a la ocurrencia y resultado CONTACTADO. Para septiembre, intervalo esperado `[2026-09-01T05:00:00Z, 2026-10-01T05:00:00Z)`.

| Contacto | Paciente | Gestor | Ciudad | Ocurrencia UTC |
| --- | --- | --- | --- | --- |
| C1 | P-A | A | 1 | 2026-09-10T15:00:00.000Z |
| C2 | P-C | A | 2 | 2026-09-11T15:00:00.000Z |
| C3 | P-B | B | 1 | 2026-09-12T15:00:00.000Z |
| C4 | P-D | B | 2 | 2026-09-13T15:00:00.000Z |
| C5 | P-A | A | 1 | 2026-09-01T04:59:59.999Z |
| C6 | P-A | A | 1 | 2026-10-01T05:00:00.000Z |
| C7 | P-A | A | 1 | 2026-09-01T05:00:00.000Z |
| C8 | P-A | A | 1 | 2026-10-01T04:59:59.999Z |

Los IDs concretos de C1-C8 se fijarán en el seed de prueba. El orden por ID en empates seguirá SQL Server uniqueidentifier, no una ordenación asumida de sus cadenas.

| ID | Dado | Cuando | Entonces | Verificación |
| --- | --- | --- | --- | --- |
| CA04-01 | Coordinadora y fixture mensual. | Consulta month=2026-09, managerId=A, cityId=1. | Exactamente C8, C1 y C7, en ese orden; total=3. C2 cumple solo gestor, C3 solo ciudad, C4 ninguno y C5/C6 están fuera del mes. | Servicio, integración y E2E; `CA04_MonthlyContacts_AppliesManagerAndCityTogether`. |
| CA04-02 | Mismo fixture. | Consulta septiembre sin filtros, solo A y solo ciudad 1, por separado. | Sin filtros: total=6, C8/C4/C3/C2/C1/C7. Solo A: C8/C2/C1/C7. Solo ciudad 1: C8/C3/C1/C7. | Prueba parametrizada de integración. |
| CA04-03 | Mismo fixture y reloj de octubre. | Consulta septiembre; después omite month. | Septiembre incluye el inicio exacto y el último milisegundo; excluye ambos extremos externos. Mes omitido devuelve month=2026-10, timeZone=America/Bogota y C6. | Servicio con reloj fijo e integración. |
| CA04-04 | Mismo fixture sin escrituras concurrentes. | Consulta septiembre con pageSize=2 y páginas 1, 2, 3 y 4. Después añade dos contactos con igual fecha y distintos IDs conocidos. | Primeras páginas: C8/C4, C3/C2, C1/C7; cuarta vacía con total=6. Con empate, el orden por ID es estable, sin repetir/omitir filas en recorrido sin cambios de datos. No se promete snapshot entre solicitudes con nuevas escrituras. | Integración. |
| CA04-05 | Mismo fixture. | Consulta un mes válido sin contactos; después mes `2026-13`, cadena vacía, año 0000, año 9999, ciudad desconocida o managerId desconocido. | Mes válido sin datos: `200`, items=[], total=0. Entradas inválidas: `400 VALIDATION_ERROR`, nunca filtro silenciosamente ignorado. UI muestra vacío o error según corresponda. | Contrato, integración y UI. |

## 7. Permisos, catálogos y contrato transversal

| ID | Dado | Cuando | Entonces | Verificación |
| --- | --- | --- | --- | --- |
| SEC-01 | Cualquier operación de `/api/v1`. | Falta X-Demo-Actor-Id, contiene UUID inválido/desconocido o el adaptador demo está deshabilitado. | `401 DEMO_ACTOR_REQUIRED`, con WWW-Authenticate; ninguna escritura. Health continúa anónimo. | Integración parametrizada sobre las 11 operaciones de negocio. |
| SEC-02 | Coordinadora y recursos existentes visibles. | Intenta alta de paciente, agendar, registrar contacto o corregirlo. | `403 FORBIDDEN`; cero escrituras. UI no ofrece esas acciones, pero el servidor también las deniega. | Integración de cuatro operaciones y E2E. |
| SEC-03 | Gestor A, P-B y contacto de B. | Busca detalle, agenda o historia ajenos; intenta agendar/registrar para P-B o corregir contacto de B. | `404 RESOURCE_NOT_FOUND`, igual que un UUID inexistente y sin revelar contenido. Listado de pacientes omite P-B. A sí puede leer sus recursos y coordinadora puede consultar ambos. | Integración parametrizada. |
| SEC-04 | Gestor A y fixture mensual. | Consulta sin managerId, con A, con B existente y con UUID de gestor inexistente. | Sin filtro/con A: solo A. Con B existente: `403`. Con inexistente: `400`. Coordinadora puede consultar por B. | Integración. |
| CAT-01 | Gestor A y coordinadora en solicitudes independientes. | Consultan catálogos. | Ambos reciben países/ciudades/tipos/canales/resultados. A recibe solo A en managers; coordinadora recibe A y B. El selector inicial de actores proviene de configuración local de demo, no requiere un catálogo anónimo nuevo. | Contrato e integración. |
| HTTP-01 | Actor válido. | Se prueba JSON mal formado, campo inesperado en cuerpo, tipo incorrecto, UUID inválido, campos obligatorios omitidos, page=0 y pageSize=101. También página válida más allá del total y cuerpo con Content-Type distinto de application/json. | Entradas inválidas: `400 VALIDATION_ERROR`; no ignorar managerId inyectado en un alta ni patientId inyectado en una corrección. Página válida fuera de datos: `200` con items=[] y total real. Tipo de contenido incorrecto: `415 UNSUPPORTED_MEDIA_TYPE`. Error inesperado: `500 INTERNAL_ERROR`, sin SQL ni stack trace. | Contrato e integración. |

Los casos de permisos usan peticiones estructuralmente válidas para no depender del orden de errores de model binding. Si un UUID de ruta tiene sintaxis incorrecta, se espera `400`, no el `404` destinado a un UUID válido pero ausente.

## 8. Operación y experiencia de error

### OPS-01: arranque y persistencia reproducibles

**Dado** un entorno de prueba con volumen vacío, configuración documentada y scripts del esquema.  
**Cuando** se arranca la composición de prueba, termina la migración, se cargan datos ficticios y se reinicia sin eliminar el volumen.  
**Entonces** la API no se declara lista antes del esquema; `/health/live` responde `200 {"status":"healthy"}` mientras el proceso esté operativo; `/health/ready` responde `503 DEPENDENCY_UNAVAILABLE` si SQL o el esquema requerido faltan y `200` cuando ambos están disponibles. El reinicio no duplica datos ni elimina una corrección realizada. Si un script falla, la API no arranca como si la migración hubiera terminado bien.

Verificación: integración de entorno. Health se consulta en la red Docker mediante `http://api:8080`; la URL pública de Angular no necesita exponerlo.

### UI-01: error del servidor visible de punta a punta

**Dado** el formulario de contacto y una solicitud válida para la validación local.  
**Cuando** el servidor rechaza occurredAt como futuro usando un reloj de prueba controlado, o una segunda ventana envía una corrección con versión obsoleta.  
**Entonces** la UI termina el estado de carga, muestra el error de fecha junto al campo o el conflicto con acción de recarga, conserva los valores introducidos para revisión y no comunica éxito. El contador de contactos/revisiones confirma que no hubo escritura inválida. No se sustituye el servidor por un mock en la evidencia E2E principal.

Verificación: Playwright + API + SQL Server del entorno de pruebas. Los dobles de HTTP sirven para pruebas unitarias de presentación, pero no prueban este recorrido completo.

## 9. Trazabilidad prevista

| CA / objetivo | Escenarios | Hallazgos relacionados | Estado actual |
| --- | --- | --- | --- |
| CA-1 | CA01-01 a CA01-05 | H-08, H-09, H-12 | Especificado, no ejecutado. |
| CA-2 | CA02-01 a CA02-04 | H-04, H-10, H-11, H-12 | Especificado, no ejecutado. |
| CA-3 parcial | CA03-01 a CA03-04 | H-06, H-07, H-10 | Especificado, no ejecutado; cobertura parcial prevista. |
| CA-4 | CA04-01 a CA04-05 | H-10, H-11, H-12 | Especificado, no ejecutado. |
| Acceso/contrato | SEC-01 a SEC-04, CAT-01, HTTP-01 | H-08, H-12, H-13 | Especificado, no ejecutado. |
| Operación/error | OPS-01, UI-01 | Requisitos transversales de la prueba | Especificado, no ejecutado. |
| CA-5 / CA-6 | Sin escenarios de implementación | H-02, H-03, H-05, H-14 | Fuera del alcance acordado. |

## 10. Precisiones introducidas al formalizar el contrato

Estas decisiones concretan el plan y deben revisarse como tales: precisión máxima de milisegundos; rechazo de campos de cuerpo desconocidos; formato completo de historial; error `415` para tipo de contenido incorrecto; orden estable del listado de pacientes; página fuera de resultados vacía; rango de años del filtro mensual; falta de garantía de idempotencia de POST. No se añaden funcionalidades clínicas ni se amplía la cobertura declarada.

El contrato usa [OpenAPI 3.0.3](https://spec.openapis.org/oas/v3.0.3.html). Su validez documental y la coherencia de ejemplos pueden comprobarse antes de programar; las reglas de negocio, los permisos y los resultados de integración solo se demostrarán al existir la aplicación.
