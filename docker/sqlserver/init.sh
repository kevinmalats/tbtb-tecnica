#!/usr/bin/env bash
set -Eeuo pipefail

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
if [[ ! -x "${SQLCMD}" ]]; then
  SQLCMD="/opt/mssql-tools/bin/sqlcmd"
fi

if [[ ! -x "${SQLCMD}" ]]; then
  echo "sqlcmd is not available in the SQL Server image." >&2
  exit 1
fi

sqlcmd=("${SQLCMD}" -S db,1433 -U sa -P "${MSSQL_SA_PASSWORD}" -C -b -r1 -v "AppDbPassword=${APP_DB_PASSWORD}")

"${sqlcmd[@]}" -Q "IF DB_ID(N'Tbtb') IS NULL CREATE DATABASE [Tbtb];"

for script in \
  /scripts/001-schema.sql \
  /scripts/002-indexes.sql \
  /scripts/003-application-permissions.sql \
  /scripts/seed-demo.sql
do
  echo "Applying ${script}"
  "${sqlcmd[@]}" -d Tbtb -i "${script}"
done

echo "SQL Server initialization completed."
