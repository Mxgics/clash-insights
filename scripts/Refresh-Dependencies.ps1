$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host 'Regenerating API NuGet lockfile...'
dotnet restore src/ClashInsights.Api --force-evaluate
if ($LASTEXITCODE -ne 0) { throw 'API dependency refresh failed' }

Write-Host 'Regenerating test NuGet lockfile...'
dotnet restore tests/ClashInsights.Tests --force-evaluate
if ($LASTEXITCODE -ne 0) { throw 'Test dependency refresh failed' }

Write-Host 'Regenerating npm lockfile...'
Push-Location web
try {
    # A coordinated Angular update temporarily makes the previous lockfile's exact
    # peer set inconsistent. Force applies the manifest as the source of truth;
    # the following clean install validates the newly resolved peer graph normally.
    npm install --package-lock-only --force
    if ($LASTEXITCODE -ne 0) { throw 'npm lockfile refresh failed' }

    Write-Host 'Installing the refreshed locked npm graph...'
    npm ci
    if ($LASTEXITCODE -ne 0) { throw 'npm locked install failed' }
} finally {
    Pop-Location
}

Write-Host 'Dependency lockfiles refreshed. Review the resulting diff before committing.'
