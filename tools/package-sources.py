# Replay Local source distribution helper, added 2026-10-24. GPL-2.0-or-later.
"""Bundle the exact Git snapshot and cached, pinned native sources; never run downloaded code."""
from pathlib import Path
import argparse, hashlib, json, subprocess, zipfile
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--cache',required=True,type=Path)
parser.add_argument('--output',required=True,type=Path)
args=parser.parse_args()
repo=Path(__file__).resolve().parents[1]
manifest=json.loads((repo/'licenses/native-sources.json').read_text(encoding='utf-8'))
if args.output.exists():raise SystemExit('Output already exists; select a new destination.')
if subprocess.check_output(['git','status','--porcelain'],cwd=repo,text=True).strip():raise SystemExit('Commit source changes before packaging.')
commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip()
archives=[]
for item in manifest['archives']:
    filename=item['filename']
    if Path(filename).name!=filename:raise SystemExit('Invalid archive filename.')
    source=args.cache/filename
    if not source.is_file() or hashlib.sha256(source.read_bytes()).hexdigest()!=item['sha256']:
        raise SystemExit('Missing or mismatched source: '+filename)
    archives.append((source,'ThirdParty/Sources/'+filename))
code=subprocess.check_output(['git','archive','--format=zip','HEAD'],cwd=repo)
manifest['replayLocalCommit']=commit
manifest['replayLocalSourceArchive']='ReplayLocal-source-'+commit[:12]+'.zip'
readme='''Replay Local — fontes e licenças / corresponding source

O ZIP ReplayLocal-source contém o snapshot exato e scripts de build do aplicativo.
ThirdParty/Sources contém OBS 32.2.2 (fonte oficial completo), obs-deps 2026-07-15
(receitas e patches) e os arquivos de fonte das dependências usados nesse build.
O manifesto registra commits, origem, hashes e seleções de arquivos.

As DLLs do OBS são assinadas. Suas seções PE foram comparadas com o pacote de
obs-deps indicado pelo CMakePresets.json do fonte do OBS: todas as 12 DLLs
distribuídas que vêm desse pacote têm as mesmas seções. Os hashes de arquivo
inteiro diferem depois da assinatura do pacote OBS.

Build do aplicativo: consulte DEPENDENCIES.md e tools/build-local.ps1 no ZIP C#.
Native: extraia OBS e obs-deps. Use as versões e opções do manifesto,
CMakePresets.json e deps.ffmpeg/*.ps1. As receitas incluem patches (FFmpeg,
LAME, SRT etc.). Extraia os fontes nos lugares dos checkouts/downloads das
dependências e aplique os patches dessas receitas. Coloque o framework do
Mbed TLS em mbedtls/framework. O arquivo AMF contém os headers e avisos usados
pela receita, sem exemplos/binários que não participam da compilação. Theora
é o fonte Git da tag v1.1.1: use autogen/configure dessa árvore.
SDK Windows, Visual Studio/MSVC, MSYS2 e CMake são pré-requisitos de build.
Esses scripts são para desenvolvimento; não precisam ser executados para usar
o aplicativo. A cadeia nativa inteira não foi recompilada aqui: o runtime é
copiado sem modificações do pacote oficial verificado do OBS.

O código próprio permanece GPL-2.0-or-later, conforme LICENSE. FFmpeg neste
build declara GPL version 3 or later; a distribuição combinada é fornecida sob
GPL-3.0-or-later, usando a opção de versão posterior das partes GPL-2.0-or-later.
As dependências mantêm seus próprios avisos/licenças. Os fontes, patches e
receitas são fornecidos junto do binário; não dependem de oferta futura.
'''
args.output.parent.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(args.output,'x',compression=zipfile.ZIP_DEFLATED) as out:
    out.writestr(manifest['replayLocalSourceArchive'],code,compress_type=zipfile.ZIP_STORED)
    for source,name in archives:out.write(source,name,compress_type=zipfile.ZIP_STORED)
    out.writestr('LEIA-ME-FONTES.txt',readme)
    out.writestr('corresponding-source-manifest.json',json.dumps(manifest,indent=2))
    for filename in ['LICENSE','CREDITS.md','THIRD-PARTY-NOTICES.md','DEPENDENCIES.md']:
        out.write(repo/filename,filename)
    for file in (repo/'licenses').iterdir():
        if file.is_file():out.write(file,'licenses/'+file.name)
print('Source bundle:',args.output,'commit',commit,'native archives',len(archives))
