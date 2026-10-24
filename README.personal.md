<!-- Replay Local fork documentation modified 2026-10-23; upstream BasilForge/Kairos. See MODIFICATIONS.md. -->

# Replay Local — versão pessoal portátil

Derivado de BasilForge/Kairos 1.0.1, revisão `04fd8bfc85f87b41a0c49e0fce6a778968e60401`. Código GPL-2.0-or-later; créditos e licenças preservados. Os créditos detalhados e as mudanças estão em [CREDITS.md](CREDITS.md) e [MODIFICATIONS.md](MODIFICATIONS.md). Compilação pessoal sem assinatura digital; não é uma certificação de ausência de malware.

Tema visual: superfícies em cinza neutro, destaque vermelho `#EF4444`, hover `#F87171`, ícone vermelho sobre fundo `#181818`. Sucesso e aviso mantêm verde e âmbar.

## Abrir e usar

Extraia a pasta inteira e abra **ReplayLocal.exe**. Não precisa instalar .NET. Windows x64 e driver de vídeo compatível são necessários.

1. A captura começa desligada. Em **Captura**, escolha tela/janela/jogo, marque a confirmação da fonte, ative captura e salve as alterações.
2. Em **Áudio**, o microfone começa desligado. Se quiser usá-lo, escolha um dispositivo específico. O dispositivo “padrão” não autoriza o microfone. Confira os estados antes de gravar.
3. Configure duração do replay, qualidade e atalhos. Os atalhos livres escolhidos aparecem no app. Pausar replay mantém as fontes abertas; **Desligar captura agora** libera tela e microfone e suspende os comandos de voz.
4. Biblioteca, reprodução e editor funcionam com captura desligada. Use o editor para recortar, alterar velocidade, texto, desenho, desfoque e filtros. A exportação/cópia fica no computador.

Sem contas, login, cookies sociais, publicação automática, navegador embutido ou atualizador do autor. Autostart começa desligado; só habilite se quiser. Atualizações desta versão são manuais.

## Áudio em faixas independentes

Na página **Áudio**, ative **Separar áudio por aplicativo**, atualize a lista e adicione até quatro aplicativos. Escolha os nomes e os volumes; o microfone continua sendo uma seleção separada e exige um dispositivo específico. Aplicar mudanças nas fontes reinicia o buffer de replay. Encerre a gravação manual antes de mudar essa configuração. Nenhum aplicativo é adicionado automaticamente. Janelas auxiliares de teclado, desenho e sobreposição não contam como sessões distintas. Se duas sessões independentes do mesmo executável tiverem janelas próprias, o app mantém a ambiguidade e não escolhe uma arbitrariamente. Quando Discord ou Spotify mudam apenas a pasta de versão, o fork reconhece a mesma instalação: Discord padrão na mesma raiz `Discord/app-X.Y.Z`, ou Spotify da Microsoft Store na mesma família de pacote e arquitetura. A reconexão usa o caminho e o PID atuais; não escolhe um executável arbitrário só pelo nome. Reinstalação em outra pasta requer selecionar o aplicativo novamente. O aplicativo precisa estar aberto e oferecer uma janela identificável, que pode estar oculta na bandeja. A identificação usa uma consulta limitada do Windows ao caminho do executável, sem enumerar módulos nem pedir acesso à memória do processo. Isso evita descartar aplicativos somente por negarem essa permissão adicional; permissões ainda negadas pelo Windows continuam sendo respeitadas.

Para escolher o jogo automaticamente, ative a chave no campo **Som de jogo**. Essa fonte ocupa uma das quatro vagas de aplicativos e mantém uma única faixa com nome estável. Ao entrar em tela cheia ou janela sem bordas, o aplicativo em primeiro plano passa a ser o alvo; Alt+Tab mantém o último alvo enquanto sua janela/processo continua identificável. Outro aplicativo em tela cheia pode assumir a faixa, inclusive um navegador. Uma janela comum maximizada não basta. Aplicativos já configurados manualmente em outra faixa não são duplicados na fonte automática. Sem alvo válido, aparece “Aguardando aplicativo em tela cheia” e a faixa permanece silenciosa. A interface informa o executável detectado. Adicionar ou mudar uma fonte exige **Salvar**; fontes novas ainda não aplicadas mostram essa instrução.

Som de jogo e Microfone têm campos próprios no topo da página, com chave, volume e barrinha de nível. O campo Microfone também permite escolher o dispositivo. Cada aplicativo adicionado tem sua própria barrinha. Verde indica áudio recebido; amarelo e vermelho indicam nível alto. “Sem sinal” indica uma fonte ativa sem nível audível; “Sem leitura” acompanha uma fonte desligada, indisponível ou ainda não aplicada. Confira o estado exibido junto à fonte e clique em **Salvar** após configurar. Os níveis de aplicativos/jogo usam a consulta de status existente (aproximadamente duas vezes por segundo); o microfone usa o fluxo de níveis existente. Os medidores não iniciam capturas extras. Desativar Som de jogo preserva sua configuração; o botão **×** remove a fonte e libera a vaga.

O clipe contém uma mistura geral e as fontes individuais. A linha do tempo do editor cria uma linha para cada fonte presente no clipe, com seu nome, abaixo de Vídeo. A mistura geral não duplica as fontes. Clique no nome ou no bloco de uma faixa para abrir seus controles de volume, silenciar, incluir e Solo; Esc limpa a seleção. As linhas acompanham os cortes e a velocidade do vídeo, sem deslocamento independente do áudio. O ícone de alto-falante cortado indica faixa silenciada/excluída ou volume zero; S indica Solo na escuta. Adicionar fontes na gravação afeta somente clipes futuros. No editor, ajuste cada faixa ou o trecho selecionado; **Solo** serve somente para escuta. Ao salvar, escolha uma mistura final (padrão) ou **Manter faixas de áudio separadas**. Silenciar uma fonte também deixa silenciosa sua faixa individual exportada. O arquivo original continua disponível quando você salva uma cópia.

Arquivos externos sem identificação começam com somente a primeira faixa incluída; você pode incluir as outras. Para clipes antigos de três faixas, há uma opção explícita de interpretação como clipe antigo do Replay Local. A separação não recupera fontes que já foram gravadas misturadas.

Uma guarda antes da mistura rejeita blocos de outro processo, inclusive se o OBS tentar reconectar a um executável de mesmo nome. O processo original é mantido por um handle do Windows; um PID reutilizado não herda a autorização. Janelas ambíguas ficam sem áudio. Ocultar a janela para a bandeja não desconecta uma fonte já capturada enquanto o processo/janela existir. Um aplicativo que destrói sua janela e nunca mais oferece uma janela identificável precisa reabri-la para ser selecionado.

## Dados e componentes opcionais

- Configurações/logs: `%APPDATA%\ReplayLocal`.
- Clipes por padrão: `%USERPROFILE%\Videos\ReplayLocal` (pode escolher outra pasta).
- Cópias preparadas: `%LOCALAPPDATA%\ReplayLocal\Share`.
- Voz é local e opcional; usa os modelos ONNX incluídos e o microfone escolhido. Desligar captura também suspende voz.
- Legendas automáticas foram removidas, junto com Whisper, download de modelo e helper de transcrição. O editor mantém o texto manual.
- Não há função de download de modelos nesta versão. Os modelos ONNX de comandos de voz já estão incluídos.
- A gravação mantém libobs e encoders por hardware, incluindo NVENC.


Esta versão não lê nem migra configurações/cookies/clipes do Kairos original. Para remover, feche pelo ícone da bandeja, desative autostart se o ativou e apague a pasta portátil. Os clipes e dados pessoais permanecem até você apagá-los.

## Verificação e reprodução do build

`dependency-manifest.json` registra revisão do código, origem dos runtimes e SHA-256 de cada arquivo. `SHA256SUMS.txt` repete os hashes para ferramentas externas. O relatório de validação entregue ao lado do pacote lista testes e limitações. Os hashes verificam integridade, não certificam segurança.

O arquivo de fonte corresponde ao commit indicado no manifesto. O build usa SDK .NET **10.0.402**, NuGet.org com lockfiles, OBS **32.2.2** oficial. Veja `DEPENDENCIES.md`, `tools/build-local.ps1`, `tools/package-local.ps1`. Os scripts exigem caminhos explícitos e dependências locais; não instalam drivers ou ferramentas globalmente.

Não foram testadas capturas da sua tela, microfone ou jogos. Compatibilidade e desempenho dos encoders no seu hardware exigem um teste específico com você. Os testes de mídia usam fixtures controladas e reprodução muda; isso não é benchmark de jogo.
