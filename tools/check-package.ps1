param([Parameter(Mandatory)][string] $Path)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Path)
$files = @(Get-ChildItem -LiteralPath $root -Recurse -File)
foreach ($file in $files) {
    if ($file.Name -match '(?i)(webview|velopack|squirrel|whisper|ggml|captions.*\.zip)|^(Update|Kairos|obs64)\.exe$') { throw "Forbidden component: $($file.Name)" }
}
foreach ($name in 'ReplayLocal.exe','ReplayLocal.dll','obs.dll','avcodec-62.dll','onnxruntime.dll','LICENSE','README.personal.md','dependency-manifest.json','SHA256SUMS.txt') {
    if (-not (Test-Path -LiteralPath (Join-Path $root $name) -PathType Leaf)) { throw "Missing package file: $name" }
}
$manifest = Get-Content -LiteralPath (Join-Path $root 'dependency-manifest.json') -Raw | ConvertFrom-Json
$seen = @{}
foreach ($entry in $manifest.files) {
    $full = [IO.Path]::GetFullPath((Join-Path $root $entry.path))
    if (-not $full.StartsWith($root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escapes package' }
    if ($seen.ContainsKey($entry.path)) { throw 'Duplicate manifest path' }
    $seen[$entry.path] = $true
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Missing manifest entry: $($entry.path)" }
    if ((Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) { throw "Hash mismatch: $($entry.path)" }
}
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\','/')
    if ($relative -notin @('dependency-manifest.json','SHA256SUMS.txt') -and -not $seen.ContainsKey($relative)) { throw "Unlisted file: $relative" }
}
Write-Output "Package verified: $($seen.Count) files; forbidden components absent."
