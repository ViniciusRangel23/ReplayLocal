<!-- Replay Local fork documentation modified 2026-10-24; upstream BasilForge/Kairos. See MODIFICATIONS.md. -->

# Replay Local

Replay instantâneo, gravação e edição local de clipes para Windows x64.

**Este é um fork independente do [Kairos, criado por BasilForge](https://github.com/BasilForge/Kairos).** A base é o Kairos 1.0.1, commit [`04fd8bfc85f87b41a0c49e0fce6a778968e60401`](https://github.com/BasilForge/Kairos/tree/04fd8bfc85f87b41a0c49e0fce6a778968e60401). A autoria original e os avisos de licença são preservados. A branch de publicação começa com um único snapshot do estado atual, sem os commits internos anteriores. Replay Local não é uma versão oficial do Kairos nem implica endosso de BasilForge.

English: Replay Local is an independent fork of BasilForge/Kairos, with local workflows and per-application audio tracks. Original authorship and the GPL license are preserved.

## Modificações deste fork

- Fluxo local, sem contas sociais, publicação em redes sociais, navegador embutido ou atualizador automático.
- Remoção de legendas automáticas, Whisper e downloads de modelos. O editor mantém texto manual; os modelos opcionais de comandos de voz vêm no pacote.
- Captura inicialmente desligada e microfone condicionado à escolha de um dispositivo específico.
- Até quatro fontes de áudio de aplicativos, mais o microfone, em faixas independentes.
- Fonte **Som de jogo** com seleção automática de aplicativo em tela cheia ou janela sem bordas. Ela ocupa uma das quatro vagas de aplicativos.
- Seleção de microfone e indicadores de nível na página Áudio.
- Reconexão do Discord e Spotify da Store quando a versão muda dentro da mesma instalação, preservando as faixas.
- Uma linha por fonte na timeline do editor, com volume, inclusão, silenciar e solo para escuta. As linhas acompanham os cortes do vídeo.
- Exportação com áudio misturado ou mantendo as faixas separadas.
- Tema em cinza neutro e vermelho, com nome Replay Local.

O motor de gravação continua baseado em libobs/OBS Studio e FFmpeg. As contribuições originais não foram recriadas do zero. Veja [créditos](CREDITS.md), [alterações por arquivo](MODIFICATIONS.md) e [licenças de terceiros](THIRD-PARTY-NOTICES.md).

## Áudio por aplicativo e edição multifaixas

Estas são as principais extensões desenvolvidas neste fork sobre a base do Kairos:

| Funcionalidade | Como funciona no Replay Local |
|---|---|
| Fontes independentes | Até **quatro aplicativos e um microfone**, com nome, ativação e volume por fonte. |
| Som de jogo | Uma fonte acompanha o aplicativo detectado em tela cheia ou janela sem bordas e mantém o alvo ao dar Alt+Tab, enquanto ele continuar identificável. Ocupa uma das quatro vagas de aplicativos. |
| Microfone | Campo próprio para escolher o dispositivo, ativar/desativar e ajustar o volume. |
| Feedback de áudio | Barrinha de nível para Som de jogo, microfone e cada aplicativo selecionado, com indicação de fonte desligada, indisponível, sem sinal ou com mudanças pendentes. |
| Reconexão após atualização | Discord padrão e Spotify da Microsoft Store são reencontrados quando muda somente a pasta de versão dentro da instalação reconhecida. As faixas e seus ajustes são preservados. |
| Linhas de áudio na timeline | O editor cria uma linha nomeada para cada fonte presente no clipe, abaixo da linha de vídeo. Mais fontes gravadas resultam em mais linhas de áudio. |
| Ajustes no editor | Clique na faixa para controlar volume, inclusão na mistura, silenciar e solo. Também há ajuste de volume por fonte no trecho de vídeo selecionado. Desfazer e refazer acompanham as alterações de áudio. |
| Exportação | Escolha um vídeo com áudio misturado ou um vídeo que mantenha as faixas separadas para edição posterior. **Solo serve apenas para escuta**, sem substituir os ajustes usados na exportação. |

**Exemplo:** jogo + microfone + Discord + Spotify gravados separadamente resultam em **quatro linhas de áudio no editor**, além da linha de vídeo. O mix geral do arquivo não duplica essas fontes na timeline.

As faixas acompanham os cortes, a velocidade e a posição do vídeo; não têm deslocamento ou cortes independentes. A quantidade de linhas depende do que foi gravado naquele arquivo. Adicionar um aplicativo depois não altera clipes antigos, e sons já gravados misturados não podem ser separados por esta função.

Na captura, selecione as fontes, clique em **Salvar** e confira os níveis com a captura ativa. Os medidores reutilizam as leituras do gravador e não abrem capturas extras só para mostrar as barras. As mudanças nas fontes podem reiniciar o buffer de replay; encerre gravações manuais antes de aplicá-las.

A identificação usa a instalação selecionada e o processo atual, sem aceitar qualquer arquivo só por ter o mesmo nome. Aplicativos fechados, instalações movidas para outra pasta ou identidades ambíguas podem continuar indisponíveis. Um navegador em tela cheia também pode ser detectado por **Som de jogo**.

## Funcionalidades herdadas do Kairos

Replay instantâneo, gravação manual, atalhos, biblioteca, captura de tela/janela/jogo, codificação de vídeo e o editor de base vêm do projeto original. As extensões acima se apoiam nesse trabalho. O suporte a codificação por hardware, incluindo NVENC, continua dependendo do hardware e dos drivers disponíveis.

## Usar

O código deste fork está disponível em [ViniciusRangel23/ReplayLocal](https://github.com/ViniciusRangel23/ReplayLocal), na branch `replay-local`. Ainda não há uma release pública com executável deste fork. Os downloads do Kairos original são versões do projeto original, não do Replay Local.

Em um pacote portátil do Replay Local, extraia a pasta inteira e abra `ReplayLocal.exe`. Em **Captura**, confirme a fonte, ative a captura e salve. Em **Áudio**, configure as fontes e clique em **Salvar**. Abra os clipes pela **Biblioteca** para editar.

Para trocar de versão, saia da anterior pela bandeja → **Sair**. Fechar pelo X apenas oculta a janela. Leia o [guia de uso e limitações](README.personal.md). As configurações ficam em `%APPDATA%\ReplayLocal`, e os clipes ficam, por padrão, em `%USERPROFILE%\Videos\ReplayLocal`.

## Limitações e privacidade

O fluxo do aplicativo não oferece upload, download de modelos ou atualizações automáticas. Isso não equivale a uma certificação de ausência de vulnerabilidades. O pacote inclui bibliotecas nativas e executáveis auxiliares do OBS e do .NET.

Não há garantia de compatibilidade com anticheat, desempenho em todos os jogos ou ausência absoluta de falhas. A captura de jogos usa a tecnologia de captura do OBS. A captura real do microfone/jogo de cada usuário precisa ser verificada pelo próprio usuário.

Os resultados de desempenho e as imagens da documentação original do Kairos não são medições ou demonstrações desta versão. As instruções, imagens e notas de versão antigas do Kairos foram retiradas desta árvore; a documentação original continua disponível na revisão base do repositório original.

## Compilar e verificar

O projeto usa C#, WPF, .NET 10 e Windows x64. As versões usadas nos builds locais são SDK .NET 10.0.402, runtime .NET 10.0.13 e OBS Studio 32.2.2. Os pacotes NuGet têm lockfiles.

Veja [DEPENDENCIES.md](DEPENDENCIES.md), `tools/build-local.ps1`, `tools/package-local.ps1` e `tools/check-package.ps1`. Esses scripts usam dependências locais verificadas e caminhos de um workspace de desenvolvimento; não são instaladores e podem exigir adaptação em outra máquina. O empacotamento também requer os redistribuíveis Microsoft VC++ indicados no script.

Os testes de lógica e fixtures controladas têm uma lista revisada em `tools/run-regressions.ps1`. Outros modos de teste podem iniciar captura; leia o teste antes de executá-lo. Uma release deve indicar o commit exato, o que foi testado e suas limitações, sem tratar resultados antigos como garantia para builds novos.

## Licença e autoria

O código derivado do Kairos e as modificações do Replay Local são distribuídos sob **GPL-2.0-or-later**. Veja [LICENSE](LICENSE). Os avisos originais de autoria, licença e ausência de garantia devem ser preservados; as dependências mantêm suas próprias licenças.

Distribuir um executável exige cumprir as condições da GPL para o código-fonte correspondente e as obrigações das dependências incluídas. Um fork no GitHub, sozinho, não resolve todas as obrigações de uma release binária. O [roteiro de publicação](docs/PUBLISHING.md) registra as verificações pendentes antes de publicar código e executáveis.

A [documentação original da revisão base](https://github.com/BasilForge/Kairos/blob/04fd8bfc85f87b41a0c49e0fce6a778968e60401/README.md) permanece disponível no repositório original.
