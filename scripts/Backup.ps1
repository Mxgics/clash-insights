param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (!$OutputPath) { $OutputPath = Join-Path 'backups' ((Get-Date -Format 'yyyyMMdd-HHmmss') + '.dump') }
$clashBackup = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path (Split-Path $clashBackup -Parent) -Force | Out-Null
docker compose exec -T db pg_dump -U clash -d clashinsights -Fc -f /tmp/clash-backup.dump
if ($LASTEXITCODE -ne 0) { throw 'Backup failed' }
$clashContainer = docker compose ps -q db
docker cp "${clashContainer}:/tmp/clash-backup.dump" $clashBackup
if ($LASTEXITCODE -ne 0) { throw 'Backup copy failed' }
Write-Host "Backup saved: $clashBackup"
