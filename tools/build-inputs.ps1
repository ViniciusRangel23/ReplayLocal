function Assert-RuntimeRoot([string] $Root) {
    if (-not (Test-Path -LiteralPath (Join-Path $Root 'bin/64bit/obs.dll'))) { throw "Required obs.dll missing from runtime root: $Root" }
}
function Assert-FileDigest([string] $Path, [string] $Expected) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected) { throw "Artifact digest mismatch: $Path" }
}
function Assert-RuntimeMatchesArchive([string] $Root, [string] $Archive) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -File) {
            $name = [IO.Path]::GetRelativePath([IO.Path]::GetFullPath($Root), $file.FullName).Replace('\','/')
            $entry = $zip.GetEntry($name)
            if ($null -eq $entry -or $entry.Length -ne $file.Length) { throw "Extracted runtime mismatch: $name" }
            $stream = $entry.Open()
            try { $expected = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
            finally { $stream.Dispose() }
            if ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne $expected) { throw "Extracted runtime mismatch: $name" }
        }
    } finally { $zip.Dispose() }
}
