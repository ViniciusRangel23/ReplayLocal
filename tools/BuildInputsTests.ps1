$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-inputs.ps1')
$temp = Join-Path ([IO.Path]::GetTempPath()) ('replay-inputs-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $missing = $false
    try { Assert-RuntimeRoot (Join-Path $temp 'missing') } catch { $missing = $_.Exception.Message -like '*obs.dll*' }
    if (-not $missing) { throw 'Missing_runtime_is_reported FAILED' }
    Write-Output 'PASS Missing_runtime_is_reported'
    $file = Join-Path $temp 'artifact.zip'
    [IO.File]::WriteAllText($file, 'controlled input')
    $rejected = $false
    try { Assert-FileDigest $file ('0' * 64) } catch { $rejected = $_.Exception.Message -like '*digest*' }
    if (-not $rejected) { throw 'Wrong_digest_is_rejected FAILED' }
    Write-Output 'PASS Wrong_digest_is_rejected'
    $extracted = Join-Path $temp 'extracted'
    New-Item -ItemType Directory -Force (Join-Path $extracted 'bin/64bit') | Out-Null
    $dll = Join-Path $extracted 'bin/64bit/obs.dll'
    Set-Content -LiteralPath $dll -Value 'verified fixture DLL'
    $zip = Join-Path $temp 'official-fixture.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($extracted, $zip)
    $zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    Set-Content -LiteralPath $dll -Value 'tampered fixture DLL'
    Assert-FileDigest $zip $zipHash
    $rejected = $false
    try { Assert-RuntimeMatchesArchive $extracted $zip } catch { $rejected = $_.Exception.Message -like '*Extracted runtime mismatch*' }
    if (-not $rejected) { throw 'Modified_extracted_DLL_is_rejected FAILED' }
    Write-Output 'PASS Modified_extracted_DLL_is_rejected (archive unchanged)'
    Write-Output '3/3 passed'
} finally { Remove-Item -LiteralPath $temp -Recurse -Force }
