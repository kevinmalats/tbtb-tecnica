param([string]$BaseUrl = 'http://127.0.0.1:8080')

$ErrorActionPreference = 'Stop'
$actor = '11111111-1111-4111-8111-111111111111'
$headers = @{ 'X-Demo-Actor-Id' = $actor }

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAILED: $message" }
    Write-Host "PASS: $message"
}

function Invoke-Json([string]$method, [string]$path, $body = $null) {
    $parameters = @{ Uri = "$BaseUrl$path"; Method = $method; Headers = $headers; ContentType = 'application/json' }
    if ($null -ne $body) { $parameters.Body = $body | ConvertTo-Json -Compress }
    Invoke-RestMethod @parameters
}

$catalogs = Invoke-Json GET '/api/v1/catalogs'
Assert-True ($catalogs.documentTypes.name -contains 'DNI') 'CAT-01 exposes translated document labels'

$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$patientBody = @{
    fullName = 'Paciente prueba automatizada'; documentCountryCode = 'CO'; documentTypeCode = 'PASSPORT'
    documentNumber = "AUTO-$suffix"; phone = '+57 300 000 0000'; email = $null; cityId = 1
    treatmentStartDate = (Get-Date).ToString('yyyy-MM-dd')
}
$patient = Invoke-Json POST '/api/v1/patients' $patientBody
Assert-True ($patient.assignedManagerId -eq $actor -and $null -eq $patient.email) 'CA01-01 persists patient and assigns current manager'
try {
    Invoke-Json POST '/api/v1/patients' $patientBody | Out-Null
    throw 'FAILED: duplicate patient unexpectedly succeeded'
} catch { Assert-True ([int]$_.Exception.Response.StatusCode -eq 409) 'CA01-02 rejects duplicate identity' }
$listed = Invoke-Json GET '/api/v1/patients?page=1&pageSize=100'
Assert-True ($listed.items.id -contains $patient.id) 'CA01-01 returns patient in assigned list'

$future = [DateTimeOffset]::UtcNow.AddHours(2).ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$followUp = Invoke-Json POST '/api/v1/follow-ups' @{ patientId = $patient.id; scheduledAt = $future }
$agenda = Invoke-Json GET "/api/v1/follow-ups?patientId=$($patient.id)"
Assert-True ($agenda.id -contains $followUp.id) 'CA01-01 schedules and retrieves follow-up'

$occurred = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$contact = Invoke-Json POST '/api/v1/contacts' @{ patientId = $patient.id; occurredAt = $occurred; channel = 'LLAMADA'; result = 'SIN_RESPUESTA' }
Assert-True ($contact.patientId -eq $patient.id -and $contact.revision -eq 1) 'CA02-01 persists contact fields'
try {
    Invoke-Json POST '/api/v1/contacts' @{ patientId = $patient.id; occurredAt = [DateTimeOffset]::UtcNow.AddMinutes(5).ToString('yyyy-MM-ddTHH:mm:ss.fffZ'); channel = 'SMS'; result = 'DESCONOCIDO' } | Out-Null
    throw 'FAILED: invalid contact unexpectedly succeeded'
} catch { Assert-True ([int]$_.Exception.Response.StatusCode -eq 400) 'CA02-03 rejects future instant and unknown codes' }

$month = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTime]::UtcNow, 'SA Pacific Standard Time').ToString('yyyy-MM')
$monthly = Invoke-Json GET "/api/v1/contacts?month=$month&managerId=$actor&cityId=1&page=1&pageSize=100"
Assert-True ($monthly.items.id -contains $contact.id) 'CA04-03 applies manager and city filters together'
try {
    Invoke-Json GET "/api/v1/contacts?month=$month&managerId=22222222-2222-4222-8222-222222222222" | Out-Null
    throw 'FAILED: cross-manager filter unexpectedly succeeded'
} catch { Assert-True ([int]$_.Exception.Response.StatusCode -eq 403) 'SEC-04 forbids another manager filter' }

$corrected = Invoke-Json POST "/api/v1/contacts/$($contact.id)/corrections" @{
    occurredAt = $occurred; channel = 'LLAMADA'; result = 'CONTACTADO'
    reason = 'Resultado confirmado durante la prueba automatizada.'; expectedVersion = $contact.version
}
$history = Invoke-Json GET "/api/v1/contacts/$($contact.id)/history"
Assert-True ($corrected.revision -eq 2 -and $history.Count -eq 2 -and $history[0].result -eq 'SIN_RESPUESTA' -and $history[1].result -eq 'CONTACTADO') 'CA03-01 preserves original revision and exposes correction'

try {
    Invoke-Json POST "/api/v1/contacts/$($contact.id)/corrections" @{
        occurredAt = $occurred; channel = 'CORREO'; result = 'FALLIDO'
        reason = 'Segundo cliente utiliza una version que ya fue reemplazada.'; expectedVersion = $contact.version
    } | Out-Null
    throw 'FAILED: stale correction unexpectedly succeeded'
} catch {
    $response = $_.Exception.Response
    Assert-True ($null -ne $response -and [int]$response.StatusCode -eq 409) 'CA03-02 rejects stale rowversion with 409'
}

Write-Host 'Acceptance smoke suite completed successfully.'
