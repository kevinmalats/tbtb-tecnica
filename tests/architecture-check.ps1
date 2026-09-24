$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-NotContains([string]$Path, [string]$Pattern, [string]$Message) {
  if (Select-String -Path (Join-Path $root $Path) -Pattern $Pattern -Quiet) { throw $Message }
}

Assert-NotContains 'api/src/Tbtb.Api/Program.cs' 'AsNoTracking|SaveChanges|\.Patients|\.Contacts|\.FollowUps' 'La API contiene acceso directo a persistencia.'
Assert-NotContains 'web/src/app/app.component.ts' 'HttpClient|core/api\.service' 'La presentacion Angular depende del adaptador HTTP.'
Assert-NotContains 'web/src/app/presentation/workspace.facade.ts' 'core/api\.service|HttpClient' 'La fachada Angular depende del adaptador HTTP.'
Assert-NotContains 'web/src/app/application/workspace.port.ts' '@angular/common/http|core/api\.service' 'El puerto Angular depende de infraestructura HTTP.'

Write-Host 'Fronteras de arquitectura verificadas.'
