param([Parameter(Mandatory)][string] $Exe, [Parameter(Mandatory)][string] $Evidence)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$Exe = [IO.Path]::GetFullPath($Exe)
$Evidence = [IO.Path]::GetFullPath($Evidence)
New-Item -ItemType Directory -Force $Evidence | Out-Null
$data = Join-Path $Evidence 'data'
New-Item -ItemType Directory -Force $data | Out-Null
@{capture=@{confirmed=$false;enabled=$false};audio=@{mic=$false};general=@{language='pt-BR';startWithWindows=$false};output=@{folder=(Join-Path $data 'Clips')}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $data 'settings.json') -Encoding utf8
$start = [Diagnostics.ProcessStartInfo]::new($Exe)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.Environment['REPLAYLOCAL_DATA'] = $data
$start.Environment['KAIROS_FFMPEG'] = [IO.Path]::GetFullPath((Join-Path $repo '../runtime/obs/bin/64bit'))
$start.Environment['DOTNET_ROOT'] = [IO.Path]::GetFullPath((Join-Path $repo '../toolchain/dotnet'))
foreach ($arg in '--perf','local-ui',$Evidence) { $start.ArgumentList.Add($arg) }
$process = [Diagnostics.Process]::Start($start)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'UI check timed out' }
$stdout.Result | Set-Content -LiteralPath (Join-Path $Evidence 'stdout.log') -Encoding utf8
$stderr.Result | Set-Content -LiteralPath (Join-Path $Evidence 'stderr.log') -Encoding utf8
if ($process.ExitCode -ne 0) { throw "UI check failed: $($process.ExitCode)" }
if ($stdout.Result -match 'UI-ERROR') { throw 'UI exception found during rendering' }
if ($stdout.Result -notmatch 'LOCAL-UI capture=False confirmed=False mic=False autostart=False') { throw 'Disabled state not proven' }
foreach ($page in 'status','capture','audio','audio-apps','clips','general') { if (-not (Test-Path -LiteralPath (Join-Path $Evidence "$page.png"))) { throw "Missing page: $page" } }
Write-Output 'PASS six app-only renders; capture/mic/autostart disabled.'
