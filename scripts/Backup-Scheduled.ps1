[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Destination,

    [ValidateRange(1, 3650)]
    [int]$RetentionCount = 14
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

function Invoke-DockerText {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $output = & docker @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw "$FailureMessage (docker exit code $exitCode)" }
    return (($output | ForEach-Object { "$_" }) -join "`n").Trim()
}

if (![System.IO.Path]::IsPathFullyQualified($Destination)) {
    throw 'Destination must be an absolute path.'
}

if (Test-Path -LiteralPath $Destination -PathType Leaf) {
    throw 'Destination must be a directory, not a file.'
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$destinationRoot = (Get-Item -LiteralPath $Destination).FullName
$lockPath = Join-Path $destinationRoot '.clash-insights-backup.lock'
try {
    $backupLock = [System.IO.File]::Open(
        $lockPath,
        [System.IO.FileMode]::OpenOrCreate,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None
    )
}
catch [System.IO.IOException] {
    throw "Another scheduled backup is already using destination $destinationRoot"
}

try {
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss-fffZ')
$backupName = "clash-insights-$timestamp"
$partialPath = Join-Path $destinationRoot ".$backupName.partial"
$verifiedPath = Join-Path $destinationRoot "$backupName.dump"
$unverifiedPath = Join-Path $destinationRoot "$backupName-unverified.dump"
$verificationDatabase = 'clash_restore_' + [Guid]::NewGuid().ToString('N')
$containerDump = "/tmp/$backupName.dump"
$verificationDatabaseCreated = $false
$verified = $false

try {
    & ./scripts/Backup.ps1 -OutputPath $partialPath
    if (!(Test-Path -LiteralPath $partialPath -PathType Leaf)) {
        throw 'Backup command completed without creating the expected dump.'
    }
    if ((Get-Item -LiteralPath $partialPath).Length -le 0) {
        throw 'Backup dump is empty.'
    }

    $null = Invoke-DockerText -Arguments @(
        'compose', 'exec', '-T', 'db',
        'createdb', '-U', 'clash', $verificationDatabase
    ) -FailureMessage 'Could not create the isolated verification database'
    $verificationDatabaseCreated = $true

    $null = Invoke-DockerText -Arguments @(
        'compose', 'cp', $partialPath, "db:$containerDump"
    ) -FailureMessage 'Could not copy the dump into the database container'

    $null = Invoke-DockerText -Arguments @(
        'compose', 'exec', '-T', 'db',
        'pg_restore', '-U', 'clash', '-d', $verificationDatabase,
        '--exit-on-error', $containerDump
    ) -FailureMessage 'Could not restore the dump into the isolated verification database'

    $query = 'SELECT count(*), min("ObservedAt"), max("ObservedAt") FROM "Snapshots"'
    $original = Invoke-DockerText -Arguments @(
        'compose', 'exec', '-T', 'db',
        'psql', '-U', 'clash', '-d', 'clashinsights', '-Atc', $query
    ) -FailureMessage 'Could not read the source snapshot verification values'
    $restored = Invoke-DockerText -Arguments @(
        'compose', 'exec', '-T', 'db',
        'psql', '-U', 'clash', '-d', $verificationDatabase, '-Atc', $query
    ) -FailureMessage 'Could not read the restored snapshot verification values'

    if ($original -ne $restored) {
        throw 'Restored snapshot count or timestamp range differs from the source database.'
    }

    Move-Item -LiteralPath $partialPath -Destination $verifiedPath
    $verified = $true
    Write-Host "Verified backup saved: $verifiedPath"
    Write-Host "Snapshot count and time range: $restored"
}
catch {
    if (Test-Path -LiteralPath $partialPath -PathType Leaf) {
        Move-Item -LiteralPath $partialPath -Destination $unverifiedPath
        Write-Warning "Verification failed. The unverified dump was preserved at: $unverifiedPath"
    }
    throw
}
finally {
    if ($verificationDatabaseCreated) {
        if ($verificationDatabase -notmatch '^clash_restore_[a-f0-9]{32}$') {
            throw 'Unsafe restore verification database name.'
        }
        & docker compose exec -T db dropdb -U clash $verificationDatabase 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not remove verification database $verificationDatabase. Remove it manually after confirming the name."
        }
    }

    & docker compose exec -T db rm -f -- $containerDump 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not remove temporary container dump $containerDump."
    }
}

if (!$verified) { throw 'Backup verification did not complete.' }

$verifiedNamePattern = '^clash-insights-\d{8}-\d{6}-\d{3}Z\.dump$'
$expired = Get-ChildItem -LiteralPath $destinationRoot -File |
    Where-Object { $_.Name -match $verifiedNamePattern } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -Skip $RetentionCount

$removed = 0
foreach ($file in $expired) {
    if (![string]::Equals($file.Directory.FullName, $destinationRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to prune a backup outside the destination: $($file.FullName)"
    }
    Remove-Item -LiteralPath $file.FullName -Force
    $removed++
}

Write-Host "Retention complete: kept the newest $RetentionCount verified backup(s); removed $removed old backup(s)."
}
finally {
    $backupLock.Dispose()
}
