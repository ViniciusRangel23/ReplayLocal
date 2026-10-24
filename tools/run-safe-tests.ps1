param([Parameter(Mandatory)][string] $Filter)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$dotnet = Join-Path $repo '../toolchain/dotnet/dotnet.exe'
$env:DOTNET_ROOT = Split-Path $dotnet
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = Join-Path $repo '../build/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $repo '../build/nuget'
$env:KAIROS_DATA = Join-Path $repo '../build/test-data'
$env:KAIROS_FFMPEG = Join-Path $repo '../runtime/obs/bin/64bit'
$env:REPLAYLOCAL_DATA = $env:KAIROS_DATA
$testDll = Join-Path $repo 'tests/Kairos.Tests/bin/x64/Release/net10.0-windows/win-x64/Kairos.Tests.dll'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$output = & $dotnet $testDll $Filter 2>&1
$code = $LASTEXITCODE
$output | Write-Output
if ($code -ne 0) { throw "Tests failed: $Filter ($code)" }
$summary = ($output | Select-String '(\d+)/(\d+) passed' | Select-Object -Last 1)
if (-not $summary -or [int]$summary.Matches[0].Groups[2].Value -eq 0) { throw "No tests selected: $Filter" }
