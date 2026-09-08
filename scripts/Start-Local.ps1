param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (!(Test-Path -LiteralPath '.env')) {
    $clashPassword = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    Set-Content -LiteralPath '.env' -Value "CLASH_DB_PASSWORD=$clashPassword"
}
$clashLine = Get-Content -LiteralPath '.env' | Where-Object { $_ -match '^CLASH_DB_PASSWORD=' } | Select-Object -First 1
if (!$clashLine) { throw 'Missing CLASH_DB_PASSWORD in .env' }
$clashPassword = $clashLine.Substring('CLASH_DB_PASSWORD='.Length)
$env:ConnectionStrings__Clash = "Host=127.0.0.1;Port=55432;Database=clashinsights;Username=clash;Password=$clashPassword"
docker compose up -d --wait
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL startup failed' }
if (!$SkipBuild) {
    Push-Location web
    try {
        npm ci
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw 'Angular build failed' }
    } finally { Pop-Location }
    $clashAssets = [System.IO.Path]::GetFullPath('src/ClashInsights.Api/wwwroot')
    $clashRepo = [System.IO.Path]::GetFullPath((Get-Location).Path) + [System.IO.Path]::DirectorySeparatorChar
    if (!$clashAssets.StartsWith($clashRepo, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid build output path' }
    if (Test-Path -LiteralPath $clashAssets) { Remove-Item -LiteralPath $clashAssets -Recurse -Force }
    New-Item -ItemType Directory -Path 'src/ClashInsights.Api/wwwroot' -Force | Out-Null
    Copy-Item -Path 'web/dist/web/browser/*' -Destination 'src/ClashInsights.Api/wwwroot' -Recurse -Force
}
if (!(Test-Path -LiteralPath 'src/ClashInsights.Api/wwwroot/index.html')) { throw 'Built UI missing; run without -SkipBuild' }
dotnet restore src/ClashInsights.Api --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed' }
Write-Host 'Clash Insights: http://127.0.0.1:5188 (Ctrl+C stops collection)'
dotnet run --project src/ClashInsights.Api --no-launch-profile
