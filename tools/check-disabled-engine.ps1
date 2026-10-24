param([Parameter(Mandatory)][string] $Exe, [Parameter(Mandatory)][string] $Evidence)
$ErrorActionPreference = 'Stop'
$Exe = [IO.Path]::GetFullPath($Exe)
$Evidence = [IO.Path]::GetFullPath($Evidence)
if (Get-Process -Name ReplayLocal -ErrorAction SilentlyContinue) { throw 'A personal app is already running; refusing to interfere' }
function Ask-Engine([string] $Command) {
    $pipe = [IO.Pipes.NamedPipeClientStream]::new('.', 'ReplayLocal', [IO.Pipes.PipeDirection]::InOut)
    try {
        $pipe.Connect(1000)
        $reader = [IO.StreamReader]::new($pipe)
        $writer = [IO.StreamWriter]::new($pipe)
        $writer.AutoFlush = $true
        $writer.WriteLine($Command)
        return $reader.ReadLineAsync().WaitAsync([TimeSpan]::FromSeconds(5)).GetAwaiter().GetResult()
    } finally { $pipe.Dispose() }
}
New-Item -ItemType Directory -Force $Evidence | Out-Null
$data = Join-Path $Evidence 'data'
New-Item -ItemType Directory -Force $data | Out-Null
@{capture=@{confirmed=$false;enabled=$false};audio=@{mic=$false};general=@{language='pt-BR';startWithWindows=$false};output=@{folder=(Join-Path $data 'Clips')}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $data 'settings.json') -Encoding utf8
$runPath = 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run'
$beforeRun = [Microsoft.Win32.Registry]::GetValue($runPath, 'ReplayLocal', $null)
$start = [Diagnostics.ProcessStartInfo]::new($Exe)
$start.UseShellExecute = $false; $start.CreateNoWindow = $true
$start.Environment['REPLAYLOCAL_DATA'] = $data
$start.ArgumentList.Add('--background')
$process = [Diagnostics.Process]::Start($start)
try {
    $state = $null
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 500
        if ($process.HasExited) { throw "Engine exited: $($process.ExitCode)" }
        try { $state = Ask-Engine 'status' | ConvertFrom-Json; break } catch [TimeoutException] { }
    }
    if ($null -eq $state) { throw 'Engine did not answer' }
    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Evidence 'status.json') -Encoding utf8
    if ($state.captureEnabled -ne $false -or $null -ne $state.engine) { throw 'Capture unexpectedly active' }
    if ($state.error) { throw "Engine initialization error: $($state.error)" }
    $caps = Ask-Engine 'caps' | ConvertFrom-Json
    $caps | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Evidence 'caps.json') -Encoding utf8
    if ($caps.encoders.Count -lt 1) { throw 'Native encoders not enumerated' }
    Start-Sleep -Seconds 3
    $process.Refresh(); $cpuBefore = $process.TotalProcessorTime.TotalMilliseconds
    $clock = [Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds 5
    $process.Refresh()
    $gpu = $null
    try {
        $gpu = (Get-CimInstance -ClassName Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine -Filter "Name LIKE 'pid_$($process.Id)_%'" -ErrorAction Stop | Measure-Object UtilizationPercentage -Sum).Sum
    } catch { }
    $sample = @{pid=$process.Id;sampleSeconds=$clock.Elapsed.TotalSeconds;cpuPercentOneCore=($process.TotalProcessorTime.TotalMilliseconds-$cpuBefore)/$clock.Elapsed.TotalMilliseconds*100;workingSetMB=$process.WorkingSet64/1MB;privateMB=$process.PrivateMemorySize64/1MB;gpuEnginePercent=$gpu;captureEnabled=$state.captureEnabled}
    $sample | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Evidence 'idle.json') -Encoding utf8
    Write-Output 'PASS disabled native engine; screen/mic sources absent; encoders enumerated.'
} finally {
    if (-not $process.HasExited) {
        try { $null = Ask-Engine 'exit' } catch { }
        if (-not $process.WaitForExit(10000)) { $process.Kill(); throw 'Engine failed to exit gracefully' }
    }
    $afterRun = [Microsoft.Win32.Registry]::GetValue($runPath, 'ReplayLocal', $null)
    if ($beforeRun -ne $afterRun) { throw 'Autostart entry changed during check' }
    $process.Dispose()
}
