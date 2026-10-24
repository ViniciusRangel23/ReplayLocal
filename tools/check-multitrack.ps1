param([Parameter(Mandatory)][string]$Exe,[Parameter(Mandatory)][string]$Evidence,[string]$NativeRuntime)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$Exe=[IO.Path]::GetFullPath($Exe);$Evidence=[IO.Path]::GetFullPath($Evidence)
New-Item -ItemType Directory -Force $Evidence | Out-Null
$oldEvidence=$env:REPLAYLOCAL_TEST_EVIDENCE;$oldNative=$env:REPLAYLOCAL_NATIVE_RUNTIME
try {
    $env:REPLAYLOCAL_TEST_EVIDENCE=$Evidence
    & (Join-Path $PSScriptRoot 'run-safe-tests.ps1') -Filter MultitrackIntegrationTests *> (Join-Path $Evidence 'integration.log')
    if($NativeRuntime){
        $env:REPLAYLOCAL_NATIVE_RUNTIME=[IO.Path]::GetFullPath($NativeRuntime)
        & (Join-Path $PSScriptRoot 'run-safe-tests.ps1') -Filter NativeApplicationAudioTests *> (Join-Path $Evidence 'native.log')
    }
    $data=Join-Path $Evidence 'ui-data';New-Item -ItemType Directory -Force $data | Out-Null
    @{capture=@{confirmed=$false;enabled=$false};audio=@{mic=$false};general=@{language='pt-BR';startWithWindows=$false};output=@{folder=(Join-Path $data 'Clips')}}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $data 'settings.json') -Encoding utf8
    $start=[Diagnostics.ProcessStartInfo]::new($Exe)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    $start.Environment['REPLAYLOCAL_DATA']=$data
    $start.Environment['KAIROS_PERF_POLL']='none'
    $start.Environment['KAIROS_FFMPEG']=[IO.Path]::GetFullPath((Join-Path $repo '../runtime/obs/bin/64bit'))
    $start.Environment['DOTNET_ROOT']=[IO.Path]::GetFullPath((Join-Path $repo '../toolchain/dotnet'))
    foreach($arg in '--perf','editor',(Join-Path $Evidence 'four-sources.mp4'),'1','1',(Join-Path $Evidence 'editor-four-sources')){$start.ArgumentList.Add($arg)}
    $process=[Diagnostics.Process]::Start($start)
    try {
        $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit(40000)){$process.Kill();$process.WaitForExit();throw 'Multitrack editor timed out'}
        $stdout.Result|Set-Content (Join-Path $Evidence 'editor.stdout.log') -Encoding utf8
        $stderr.Result|Set-Content (Join-Path $Evidence 'editor.stderr.log') -Encoding utf8
        if($process.ExitCode -ne 0 -or $stdout.Result -match 'UI-ERROR' -or $stdout.Result -notmatch 'PERF played' -or $stdout.Result -notmatch 'PERF editor-closed'){throw 'Multitrack editor lifecycle failed'}
    } finally {$process.Dispose()}
    'PASS synthetic multitrack round trips, measurements and muted editor lifecycle.'
} finally {$env:REPLAYLOCAL_TEST_EVIDENCE=$oldEvidence;$env:REPLAYLOCAL_NATIVE_RUNTIME=$oldNative}
