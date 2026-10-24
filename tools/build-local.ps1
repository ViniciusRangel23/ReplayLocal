param([switch] $Locked)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$dotnet = Join-Path $repo '../toolchain/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { throw 'Verified local .NET SDK not found' }
$env:DOTNET_ROOT = Split-Path $dotnet
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_HOME = Join-Path $repo '../build/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $repo '../build/nuget'
$restoreArgs = @('restore', (Join-Path $repo 'Kairos.sln'))
if ($Locked) { $restoreArgs += '--locked-mode' }
& $dotnet @restoreArgs
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
& $dotnet build (Join-Path $repo 'Kairos.sln') -c Release --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
