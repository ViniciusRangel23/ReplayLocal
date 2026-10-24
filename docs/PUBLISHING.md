# Preparação para publicar o fork

O fork público de código é [ViniciusRangel23/ReplayLocal](https://github.com/ViniciusRangel23/ReplayLocal), com a branch `replay-local`. Este roteiro permanece como referência de publicação e para as verificações pendentes de uma futura release binária. A release portátil usa a tag `portable-2026.10.24`; um canal de suporte privado ainda precisa ser definido.

## Repositório

1. Criar o fork pela função **Fork** de `https://github.com/BasilForge/Kairos` na conta escolhida. O GitHub deve mostrar a ligação com o original.
2. A branch `replay-local` começa com um commit inicial independente, contendo o snapshot limpo e os créditos à base original. Enviar apenas essa branch e configurar a apresentação padrão do fork para mostrá-la. O vínculo de fork deve ser criado pelo GitHub; a nova branch não contém a ancestralidade dos commits originais. Comparações, sincronização e pull requests com o upstream podem exigir trabalho adicional por não haver ancestral comum.
3. Antes do push, revisar o snapshot e confirmar que `git rev-list --count replay-local` retorna `1` no estado inicial. Não usar `--mirror`, `--all` nem enviar tags/branches antigas: outras referências locais do repositório compartilhado podem conservar o original ou commits de trabalho. A `.gitignore` não remove dados do histórico. Não publicar credenciais, configurações reais, gravações pessoais, diretórios de build ou dados de usuários.
4. Adicionar o fork como destino de push somente depois de verificar conta e URL. Preservar o original como upstream. Não enviar alterações para o repositório original.
5. Atualizar README, descrição e links com as URLs reais. Informar que Replay Local é independente e não endossado pelo original.
6. Definir suporte próprio e um canal privado para relatos de segurança. Não encaminhar automaticamente falhas exclusivas do fork ao autor original. Esta preparação não ativa Actions ou publicações automáticas.

## Licença e notificações

- Preservar `LICENSE`, créditos, avisos de copyright e de ausência de garantia, além das licenças de terceiros.
- Manter GPL-2.0-or-later para o código derivado e as modificações do fork.
- Manter avisos destacados de modificação e datas nos arquivos alterados. `MODIFICATIONS.md` relaciona mudanças e registra separadamente materiais cujo formato não aceita comentários.
- Confirmar obrigações de modelos, fontes, imagens e binários redistribuídos. Não presumir que a licença do código cobre marcas ou todos os recursos de terceiros.

## Antes de uma release com executáveis

- Compilar do commit publicado e guardar dependências, instruções e scripts necessários para reproduzir o build.
- Disponibilizar o fonte completo correspondente ao executável, incluindo alterações e scripts de compilação, na mesma release quando essa for a forma escolhida para cumprir a GPL.
- Fornecer `ReplayLocal-Fontes-e-Licencas.zip` junto do portátil: snapshot exato do aplicativo, fonte oficial OBS, receitas e patches obs-deps e fontes das dependências conforme `licenses/native-sources.json`. Para novo runtime, repetir a identificação/conferência e atualizar o manifesto.
- Conferir as condições de redistribuição do .NET, VC++, ONNX, modelos e fontes. Não declarar que todo o pacote está sob uma única licença.
- Gerar manifesto e hashes, conferir o pacote e executar as verificações pertinentes. Informar versões, limitações, ausência de assinatura digital quando aplicável e data real das verificações.
- Usar versão/tag própria do fork. Não substituir tags do Kairos nem anunciar instaladores/atualizações herdados como funcionando neste fork.

Cada pacote deve ser compilado do commit indicado em seu próprio manifesto. Pacotes antigos continuam vinculados a seus commits e não devem ser apresentados como builds posteriores.
