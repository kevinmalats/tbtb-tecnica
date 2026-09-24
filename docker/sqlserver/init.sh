#!/usr/bin/env bash
set -Eeuo pipefail
trap 'echo "SQL Server initialization failed at line ${LINENO}. Review: docker compose logs db-init" >&2' ERR

: "${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
: "${APP_DB_PASSWORD:?APP_DB_PASSWORD is required}"

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
if [[ ! -x "${SQLCMD}" ]]; then
  SQLCMD="/opt/mssql-tools/bin/sqlcmd"
fi

if [[ ! -x "${SQLCMD}" ]]; then
  echo "sqlcmd is not available in the SQL Server image." >&2
  exit 1
fi

sqlcmd=("${SQLCMD}" -S db,1433 -U sa -P "${MSSQL_SA_PASSWORD}" -C -b -r1 -v "AppDbPassword=${APP_DB_PASSWORD}" "DemoReferenceUtc=${DEMO_REFERENCE_UTC:-2026-09-24T15:00:00Z}")

"${sqlcmd[@]}" -Q "IF DB_ID(N'Tbtb') IS NULL CREATE DATABASE [Tbtb];"

"${sqlcmd[@]}" -d Tbtb -Q "
IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersion (
        Version varchar(100) NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        Checksum char(64) NOT NULL,
        AppliedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;"

scripts=(
  /scripts/001-schema.sql
  /scripts/002-indexes.sql
  /scripts/003-application-permissions.sql
  /scripts/seed-demo.sql
)

if [[ "${LOAD_DEMO_CONTACTS:-true}" == "true" ]]; then
  scripts+=(/scripts/004-demo-contacts.sql)
fi

if [[ "${LOAD_TEST_SEED:-false}" == "true" ]]; then
  scripts+=(/scripts/seed-test.sql)
fi

for script in "${scripts[@]}"
do
  version="$(basename "${script}")"
  checksum="$(sha256sum "${script}" | cut -d' ' -f1)"
  stored="$("${sqlcmd[@]}" -d Tbtb -h -1 -W -Q "SET NOCOUNT ON; SELECT Checksum FROM dbo.SchemaVersion WHERE Version = '${version}';" | tr -d '\r[:space:]')"

  if [[ -n "${stored}" && "${stored}" != "${checksum}" ]]; then
    echo "Checksum mismatch for ${version}; an applied script must not be edited." >&2
    exit 1
  fi

  if [[ -z "${stored}" ]]; then
    echo "Applying ${script}"
    "${sqlcmd[@]}" -d Tbtb -i "${script}"
    "${sqlcmd[@]}" -d Tbtb -Q "INSERT dbo.SchemaVersion (Version, Checksum) VALUES ('${version}', '${checksum}');"
  else
    echo "Already applied ${script}"
  fi
done

echo "SQL Server initialization completed."
