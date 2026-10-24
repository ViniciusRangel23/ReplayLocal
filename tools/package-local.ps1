param([Parameter(Mandatory)][string] $RuntimeRoot, [Parameter(Mandatory)][string] $Output)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
. (Join-Path $PSScriptRoot 'build-inputs.ps1')
$RuntimeRoot = [IO.Path]::GetFullPath($RuntimeRoot)
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Output must be a new directory' }
Assert-RuntimeRoot $RuntimeRoot
$obsArchive = Join-Path (Split-Path $RuntimeRoot) 'obs.zip'
Assert-FileDigest $obsArchive '4d6e40e3ab155f56b30de517380566a206d74b63cdf5ad49aa596924768f97e1'
Assert-RuntimeMatchesArchive $RuntimeRoot $obsArchive
& (Join-Path $PSScriptRoot 'build-local.ps1') -Locked
$dotnet = Join-Path $repo '../toolchain/dotnet/dotnet.exe'
& $dotnet restore (Join-Path $repo 'src/Kairos/Kairos.csproj') -r win-x64 -p:SelfContained=true --locked-mode --nologo
if ($LASTEXITCODE -ne 0) { throw 'Self-contained runtime restore failed' }
$stage = $Output + '.building-' + [Guid]::NewGuid().ToString('N')
& $dotnet publish (Join-Path $repo 'src/Kairos/Kairos.csproj') -c Release -r win-x64 --self-contained true --no-restore -o $stage --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
& (Join-Path $PSScriptRoot 'stage-runtime.ps1') -Dest $stage -Obs $RuntimeRoot
if ($LASTEXITCODE -ne 0) { throw 'Runtime staging failed' }
New-Item -ItemType Directory -Force (Join-Path $stage 'licenses') | Out-Null
foreach ($name in 'LICENSE','THIRD-PARTY-NOTICES.md','README.personal.md','CREDITS.md','MODIFICATIONS.md') { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $stage }
New-Item -ItemType Directory -Force (Join-Path $stage 'docs') | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'docs/PUBLISHING.md') -Destination (Join-Path $stage 'docs/PUBLISHING.md')
Copy-Item -Path (Join-Path $repo 'licenses/*') -Destination (Join-Path $stage 'licenses')
Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT 'LICENSE.txt') -Destination (Join-Path $stage 'licenses/dotnet-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT 'ThirdPartyNotices.txt') -Destination (Join-Path $stage 'licenses/dotnet-ThirdPartyNotices.txt')
# Microsoft app-local CRT from the installed, signed VS redistribution files; no installation or registry writes.
$crt = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Redist\MSVC\14.44.35112\x64\Microsoft.VC143.CRT'
foreach ($name in 'msvcp140.dll','msvcp140_1.dll','msvcp140_2.dll','msvcp140_atomic_wait.dll','msvcp140_codecvt_ids.dll','vcruntime140.dll','vcruntime140_1.dll','concrt140.dll') {
    $file = Join-Path $crt $name
    if ((Get-AuthenticodeSignature -LiteralPath $file).Status -ne 'Valid') { throw "Invalid Microsoft CRT signature: $name" }
    Copy-Item -LiteralPath $file -Destination $stage
}
# A short first-run guide is part of the integrity manifest.
@"
Replay Local — Windows x64 portátil

1. Extraia a pasta inteira. Não execute diretamente dentro do ZIP.
2. Abra ReplayLocal.exe. Não precisa instalar .NET, OBS ou compilar o projeto.
3. Em Captura, escolha a fonte, confirme, ative a captura e salve.
   Em Áudio, escolha o microfone e os aplicativos e clique em Salvar.

Jogo + microfone + Discord + Spotify podem ser gravados em faixas separadas.
Abra o clipe pela Biblioteca para editar as faixas e exportar.
Fechar pelo X apenas oculta o app; use bandeja → Sair para encerrar.
Para atualizar, saia do app e extraia a nova versão em outra pasta.
Não apague suas configurações ou clipes para atualizar.

Configurações: %APPDATA%\ReplayLocal
Clipes: %USERPROFILE%\Videos\ReplayLocal (padrão; você pode alterar).
Código e downloads: https://github.com/ViniciusRangel23/ReplayLocal

Fork independente do Kairos/BasilForge. Veja CREDITS.md e LICENSE.
O pacote combinado usa GPL-3.0-or-later por incluir FFmpeg GPLv3+;
o código próprio permanece GPL-2.0-or-later. Veja licenses/GPL-3.0.txt.
O ZIP de fontes e licenças acompanha o download em arquivo separado.
O executável não tem assinatura digital do mantenedor.
Não há garantia de compatibilidade com todos os jogos ou anticheats.
"@ | Set-Content -LiteralPath (Join-Path $stage 'LEIA-ME.txt') -Encoding utf8
$entries = @(Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
    @{path=[IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$_.Length}
})
$manifest = @{
    product='Replay Local'; baseCommit='04fd8bfc85f87b41a0c49e0fce6a778968e60401'; sourceCommit=(& git -C $repo rev-parse HEAD)
    obs=@{version='32.2.2';url='https://github.com/obsproject/obs-studio/releases/download/32.2.2/OBS-Studio-32.2.2-Windows-x64.zip';sha256='4d6e40e3ab155f56b30de517380566a206d74b63cdf5ad49aa596924768f97e1'}
    dotnet=@{sdk='10.0.402';runtime='10.0.13';rid='win-x64';source='https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'}
    crt=@{version='14.44.35112';source='Microsoft Visual Studio Build Tools app-local redistribution';signature='Valid Microsoft'}
    nuget='NuGet.org only; exact graph in source packages.lock.json';files=$entries
}
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $stage 'dependency-manifest.json') -Encoding utf8
$entries | ForEach-Object { "$($_.sha256)  $($_.path)" } | Set-Content -LiteralPath (Join-Path $stage 'SHA256SUMS.txt') -Encoding utf8
& (Join-Path $PSScriptRoot 'check-package.ps1') -Path $stage
Move-Item -LiteralPath $stage -Destination $Output
Write-Output "Portable package: $Output"
