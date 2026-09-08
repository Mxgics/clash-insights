$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Write-Host 'Stop the API using Ctrl+C in its terminal. Stopping PostgreSQL without deleting history.'
docker compose stop
if ($LASTEXITCODE -ne 0) { throw 'Database stop failed' }
