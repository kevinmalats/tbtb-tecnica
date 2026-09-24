#!/usr/bin/env bash
set -Eeuo pipefail

base="${API_URL:-http://api:8080}"
actor="11111111-1111-4111-8111-111111111111"
coordinator="33333333-3333-4333-8333-333333333333"
header="X-Demo-Actor-Id: ${actor}"

assert_contains() {
  local body="$1"
  local expected="$2"
  local label="$3"
  if [[ "${body}" != *"${expected}"* ]]; then
    echo "FAILED: ${label}" >&2
    echo "${body}" >&2
    exit 1
  fi
  echo "PASS: ${label}"
}

live="$(curl --fail --silent "${base}/health/live")"
assert_contains "${live}" '"status":"healthy"' 'OPS-01 liveness'

ready="$(curl --fail --silent "${base}/health/ready")"
assert_contains "${ready}" '"status":"healthy"' 'OPS-01 readiness with SQL schema'

catalogs="$(curl --fail --silent -H "${header}" "${base}/api/v1/catalogs")"
assert_contains "${catalogs}" '"name":"DNI"' 'CAT-01 document label'

patients="$(curl --fail --silent -H "${header}" "${base}/api/v1/patients?page=1&pageSize=100")"
assert_contains "${patients}" '"id":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"' 'SEC-03 assigned patient visible'
if [[ "${patients}" == *'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2'* ]]; then
  echo 'FAILED: SEC-03 foreign patient leaked' >&2
  exit 1
fi
echo 'PASS: SEC-03 foreign patient hidden'

status="$(curl --silent --output /tmp/invalid.json --write-out '%{http_code}' -H "${header}" "${base}/api/v1/contacts?month=2026-13")"
[[ "${status}" == '400' ]] || { echo "FAILED: CA04-05 expected 400, got ${status}" >&2; exit 1; }
assert_contains "$(cat /tmp/invalid.json)" '"code":"VALIDATION_ERROR"' 'CA04-05 invalid month contract'

september="$(curl --fail --silent -H "X-Demo-Actor-Id: ${coordinator}" "${base}/api/v1/contacts?month=2026-09&page=1&pageSize=100")"
assert_contains "${september}" '"total":6' 'CA04-02 exact September total'
assert_contains "${september}" 'cccccccc-cccc-4ccc-8ccc-000000000008' 'CA04-01 includes upper in-range boundary'
assert_contains "${september}" 'cccccccc-cccc-4ccc-8ccc-000000000007' 'CA04-03 includes lower boundary'
if [[ "${september}" == *'000000000005'* || "${september}" == *'000000000006'* ]]; then
  echo 'FAILED: CA04-03 leaked an excluded boundary' >&2
  exit 1
fi
echo 'PASS: CA04-03 excludes outside boundaries'

echo 'Compose integration suite completed successfully.'
