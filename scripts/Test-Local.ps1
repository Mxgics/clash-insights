$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$line=Get-Content -LiteralPath '.env' | Where-Object {$_ -match '^CLASH_DB_PASSWORD='} | Select-Object -First 1
if(!$line){throw 'Run Start-Local.ps1 first'}
$password=$line.Substring('CLASH_DB_PASSWORD='.Length)
$env:CLASH_TEST_CONNECTION="Host=127.0.0.1;Port=55432;Database=clashinsights;Username=clash;Password=$password"
dotnet restore tests/ClashInsights.Tests --locked-mode
if($LASTEXITCODE -ne 0){throw 'Locked restore failed'}
dotnet test tests/ClashInsights.Tests --no-restore
if($LASTEXITCODE -ne 0){throw 'Backend tests failed'}
