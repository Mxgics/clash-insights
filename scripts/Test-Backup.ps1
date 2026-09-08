$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$backup = Join-Path 'backups' ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-verified.dump')
& ./scripts/Backup.ps1 -OutputPath $backup
if($LASTEXITCODE -ne 0){throw 'Backup failed'}
$testDb='clash_restore_'+[Guid]::NewGuid().ToString('N')
docker compose exec -T db createdb -U clash $testDb
if($LASTEXITCODE -ne 0){throw 'Could not create restore verification database'}
try {
 docker compose cp $backup db:/tmp/restore-verification.dump
 if($LASTEXITCODE -ne 0){throw 'Dump copy failed'}
 docker compose exec -T db pg_restore -U clash -d $testDb --exit-on-error /tmp/restore-verification.dump
 if($LASTEXITCODE -ne 0){throw 'Restore failed'}
 $query='SELECT count(*), min("ObservedAt"), max("ObservedAt") FROM "Snapshots"'
 $original=docker compose exec -T db psql -U clash -d clashinsights -Atc $query
 $restored=docker compose exec -T db psql -U clash -d $testDb -Atc $query
 if($LASTEXITCODE -ne 0 -or $original -ne $restored){throw 'Restored snapshot counts or timestamps differ'}
 Write-Host "Verified restored snapshot count and time range: $restored"
} finally {
 if($testDb -notmatch '^clash_restore_[a-f0-9]{32}$'){throw 'Unsafe restore test database name'}
 docker compose exec -T db dropdb -U clash $testDb
}
