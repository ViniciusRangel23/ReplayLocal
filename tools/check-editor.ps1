param([Parameter(Mandatory)][string] $Exe, [Parameter(Mandatory)][string] $Evidence)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$Exe=[IO.Path]::GetFullPath($Exe); $Evidence=[IO.Path]::GetFullPath($Evidence)
New-Item -ItemType Directory -Force $Evidence | Out-Null
foreach($fixture in 'clip-h264','clip-av1-2tracks') {
    $data=Join-Path $Evidence ($fixture+'-data'); New-Item -ItemType Directory -Force $data | Out-Null
    @{capture=@{confirmed=$false;enabled=$false};audio=@{mic=$false};general=@{language='pt-BR';startWithWindows=$false};output=@{folder=(Join-Path $data 'Clips')}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $data 'settings.json') -Encoding utf8
    $start=[Diagnostics.ProcessStartInfo]::new($Exe)
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    $start.Environment['REPLAYLOCAL_DATA']=$data
    $start.Environment['KAIROS_FFMPEG']=[IO.Path]::GetFullPath((Join-Path $repo '../runtime/obs/bin/64bit'))
    $start.Environment['DOTNET_ROOT']=[IO.Path]::GetFullPath((Join-Path $repo '../toolchain/dotnet'))
    foreach($arg in '--perf','editor',(Join-Path $repo "tests/Kairos.Tests/Fixtures/Share/$fixture.mp4"),'2','1',(Join-Path $Evidence $fixture)) {$start.ArgumentList.Add($arg)}
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
    $timedOut=-not $process.WaitForExit(25000)
    if($timedOut) {$process.Kill();$process.WaitForExit()}
    $stdout.Result | Set-Content -LiteralPath (Join-Path $Evidence "$fixture.stdout.log") -Encoding utf8
    $stderr.Result | Set-Content -LiteralPath (Join-Path $Evidence "$fixture.stderr.log") -Encoding utf8
    if($timedOut) {throw "Editor layout/playback timed out: $fixture"}
    if($process.ExitCode -ne 0 -or $stdout.Result -match 'UI-ERROR' -or $stdout.Result -notmatch 'PERF played' -or $stdout.Result -notmatch 'PERF editor-closed') {throw "Editor lifecycle failed: $fixture"}
    $process.Dispose()
    Write-Output "PASS editor opens, plays muted, pauses and closes: $fixture (pt-BR)."
}
