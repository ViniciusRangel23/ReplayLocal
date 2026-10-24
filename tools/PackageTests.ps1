$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('replay-local-package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
try {
    foreach ($name in 'WebView2Loader.dll','whisper.dll','ggml-cpu.dll','replay-local-captions.zip') {
        $file = Join-Path $root $name
        Set-Content -LiteralPath $file -Value 'synthetic forbidden component'
        $rejected = $false
        try { & (Join-Path $PSScriptRoot 'check-package.ps1') -Path $root }
        catch { if ($_.Exception.Message -notlike 'Forbidden component:*') { throw }; $rejected = $true }
        if (-not $rejected) { throw "Forbidden component accepted: $name" }
        [IO.File]::Delete($file)
        Write-Output "PASS Package_rejects_forbidden_component: $name"
    }
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    if (-not $resolved.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected cleanup target' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
