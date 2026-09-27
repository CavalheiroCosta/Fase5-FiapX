# Planejamento — Processamento de Vídeos

Este documento registra o desenho do sistema e as melhorias em relação ao protótipo e ao diagrama inicial.

O domínio das três APIs está em definição. Os serviços serão em .NET, num monorepo. Os dados ficam em PostgreSQL, um banco por contexto. As duas filas existem nas duas implantações: local e nuvem. O acesso do usuário viaja num token, exigido nos endpoints. O Redis guarda a listagem de status e a marca de vídeo em processamento. O sistema inteiro precisa rodar local, sem depender da nuvem. Há testes unitários em tudo, com cobertura mínima de 80%. O CI/CD entra na demonstração do projeto; a referência está nos repositórios da Fase 4, em `Planning/Referencias`. No local, a fila é RabbitMQ, o storage é MinIO e a infraestrutura sobe num Docker Compose com Prometheus e Grafana.

## Contextos

| Contexto | Estado |
| --- | --- |
| Auth API | Em definição |
| Video API | Em definição |
| Video Processor API | Em definição |

## Auth API

API de autenticação e autorização. Cuida dos acessos do usuário e realiza a autenticação.

O restante do sistema só atende quem esta API reconhece. Ela responde quem é o usuário e se o acesso dele está válido. Não processa vídeo, não guarda arquivo e não acompanha status de processamento.

### Conceitos

- **Usuário:** pessoa que usa o sistema. É a dona dos vídeos que enviar. Tem um e-mail, usado pela Video API quando um processamento falha.
- **Credencial:** usuário e senha usados para provar a identidade.
- **Acesso:** permissão daquele usuário para entrar e usar o sistema.
- **Identidade autenticada:** confirmação de quem está agindo depois de um acesso válido. Os outros contextos usam essa identidade para saber de quem é cada vídeo.
- **Token:** o que o usuário envia em cada requisição depois do login. Carrega a identidade autenticada, inclusive o usuário e o e-mail.

### O que faz

- Cria e mantém o acesso de um usuário (quem pode entrar, com qual credencial).
- Autentica: confere usuário e senha e, se estiverem corretos, emite o token.
- O login recebe a credencial e devolve o token. Esse endpoint não exige token.
- Os endpoints de envio, listagem e download exigem o token na requisição. Sem token válido, a ação não acontece.
- Persiste usuário, credencial e acesso no Postgres de usuários. Só esta API escreve nessa base.

### Regras

- Toda ação de vídeo parte de uma requisição com token válido.
- Senha errada ou usuário inexistente não gera token.
- O token carrega o usuário e o e-mail, para que a listagem, o download e o aviso de erro sejam do dono daquele vídeo.
- Autorização, neste momento, significa “acesso válido ou não”. Não há papéis (administrador, operador, etc.).

### Fora deste contexto

- Envio, listagem, download e status de vídeo.
- Processamento dos frames e geração do ZIP.
- Enviar o e-mail de erro. Quem avisa é a Video API.

### Em aberto

- O usuário se cadastra sozinho, ou os acessos são criados para ele?
- Um acesso pode ser encerrado (sair) e revogado, ou basta autenticar de novo na próxima vez?

## Video API

Serviço central dos vídeos. Verifica o status, recebe o envio e realiza o download.

O usuário envia o token em cada uma dessas ações. A identidade autenticada vem desse token, emitido pela Auth API. O arquivo fica em storage externo. A quebra do vídeo e a criação do ZIP ficam na Video Processor API. A listagem de status é lida do Redis.

Esta API é a dona do registro do vídeo: de quem é, em que ponto está e onde está o arquivo. O usuário consulta o andamento aqui.

### Conceitos

- **Vídeo:** envio de um usuário. Guarda o dono, o status e a referência do arquivo no storage.
- **Envio:** ato de submeter um vídeo. A Video API guarda o arquivo no storage e abre o registro.
- **Status:** situação do vídeo para o dono. Muda conforme o processor informa o andamento.
- **Download:** entrega do ZIP gerado a partir daquele vídeo.
- **Storage externo:** lugar dos arquivos, fora das duas APIs. No local é o MinIO. Guarda o vídeo enviado e o ZIP produzido.

### Status

- **Aguardando processamento:** o vídeo já foi aceito e está no storage. O processor ainda não começou.
- **Em processamento:** o processor avisou que começou.
- **Concluído:** o processor terminou com sucesso e o ZIP está no storage. O download fica disponível.
- **Erro:** o processor terminou com falha. O download do ZIP não fica disponível. A Video API avisa o dono por e-mail.

### O que faz

- Recebe o vídeo numa requisição com token e grava o arquivo no storage externo.
- Registra o vídeo no Postgres de vídeos, no nome desse usuário, com status aguardando processamento.
- Publica na fila de processamento a referência do vídeo (identificador e caminho no storage).
- Consome a fila de status e atualiza o Postgres e o Redis quando o processor avisa que começou, que concluiu ou que falhou.
- Quando o aviso é de erro, envia um e-mail ao dono do vídeo.
- Lista os vídeos do usuário do token. A lista sai do Redis.
- Entrega o download do ZIP quando o status é concluído.

### Regras

- Sem token válido na requisição, não há envio, listagem nem download.
- Cada vídeo pertence a um único usuário. A listagem e o download mostram só os vídeos desse usuário.
- O arquivo original e o ZIP ficam no storage. O Postgres de vídeos guarda a referência, não o binário.
- Quem grava status, dono e referências é a Video API. O processor não escreve no Postgres.
- A publicação na fila de processamento acontece depois do registro em aguardando. Se a publicação falhar, o vídeo continua pendente e pode ser enfileirado de novo.
- A fila carrega a referência do vídeo, não o arquivo.
- Download existe apenas com status concluído.
- O e-mail de erro sai depois que o status do vídeo já está em erro. O processor não envia esse e-mail.

### Fora deste contexto

- Conferir usuário e senha e emitir o token. Isso é a Auth API. Aqui o endpoint lê o token da requisição.
- Quebrar o vídeo em frames e montar o ZIP.
- Gravar o ZIP no storage. Quem grava o resultado é a Video Processor API.

### Em aberto

- O usuário pode baixar o vídeo original, ou só o ZIP?

## Video Processor API

Responsável pelo processamento do vídeo: quebrar o vídeo, criar o ZIP, salvar o ZIP no storage externo e devolver o resultado para a Video API.

O resultado pode ser sucesso ou erro. Também avisa a Video API quando começa a processar.

Não atende o usuário. Não decide quem pode ver o vídeo. Não lista status. Trabalha um vídeo que a Video API já aceitou.

### Conceitos

- **Processamento:** trabalho de quebrar o vídeo e produzir o ZIP.
- **Quebra do vídeo:** extração dos frames a partir do arquivo original no storage.
- **ZIP:** resultado do processamento. É o arquivo que o usuário baixa.
- **Resultado:** o que esta API devolve à Video API. Três momentos: começou, sucesso ou erro.

### O que faz

- Consome a fila de processamento e pega um vídeo já aceito pela Video API.
- Marca esse vídeo no Redis antes de trabalhar. Se a marca já existir, não processa de novo.
- Publica na fila de status que o processamento começou.
- Lê o vídeo no storage, quebra em frames e cria o ZIP.
- Salva o ZIP no storage externo.
- Publica na fila de status o sucesso, com a referência do ZIP, ou o erro.

### Regras

- Só processa vídeo que a Video API encaminhou.
- O início do trabalho é informado antes do término, para o status sair de aguardando e ir para em processamento.
- Sucesso só vale com o ZIP já salvo no storage.
- Erro é um resultado explícito. A Video API passa o vídeo para status de erro e envia o e-mail ao dono.
- O processor não altera o dono do vídeo, não escreve no Postgres e não atende download.
- Vários processors podem consumir a mesma fila de processamento. Cada mensagem é de um vídeo. O Redis garante que o mesmo vídeo não seja processado por dois ao mesmo tempo. O resultado volta na fila de status com o identificador daquele vídeo.
- A marca no Redis sai quando o processamento termina, em sucesso ou em erro.

### Fora deste contexto

- Login, listagem e download.
- Decidir se o usuário pode submeter aquele vídeo.
- Enviar e-mail. O processor só publica o erro na fila de status. A Video API avisa o usuário.

### Em aberto

- Um erro de processamento pode ser tentado de novo, ou o vídeo permanece em erro?

## Banco de dados

Dois Postgres, um por contexto. O storage externo não é banco: lá ficam o vídeo e o ZIP.

### Postgres de usuários

Dono: Auth API. A Video API não lê nem escreve aqui. Ela recebe só a identidade autenticada.

Guarda o usuário, o e-mail, a credencial e o acesso.

### Postgres de vídeos

Dono: Video API. Nem a Auth API nem a Video Processor API escrevem aqui.

Guarda, para cada vídeo:

- identificador
- usuário dono
- status (aguardando processamento, em processamento, concluído, erro)
- referência do arquivo original no storage
- referência do ZIP no storage, quando o status é concluído

O PostgreSQL é o registro do vídeo. A listagem que o usuário vê sai do Redis. No download, a Video API busca o ZIP no storage pela referência guardada nesta base.

## Filas

Duas filas. As duas carregam referência e resultado, não o binário. O arquivo continua no storage.

### Fila de processamento

- Publica: Video API, depois de gravar o arquivo no storage e o registro como aguardando processamento.
- Consome: Video Processor API.
- Conteúdo: identificador do vídeo e caminho do arquivo original no storage.

Mais de um processor pode consumir esta fila. Cada um pega um vídeo diferente. É o que permite processar vários ao mesmo tempo e segurar o pico sem perder o envio: o request do usuário termina quando a mensagem está na fila.

### Fila de status

- Publica: Video Processor API.
- Consome: Video API, que aplica a mudança no Postgres de vídeos.
- Conteúdo: identificador do vídeo e o momento (começou, sucesso ou erro). No sucesso, também a referência do ZIP.

O processor não atualiza o status direto no banco. A Video API continua dona do registro. No erro, ela grava o status e envia o e-mail ao dono.

As duas filas são as mesmas no local e na nuvem: mesmos publicadores, consumidores e conteúdos. O que muda é onde a fila roda. Os serviços em .NET falam com as filas por um contrato só, para trocar a implantação sem mudar o fluxo do vídeo.

### Local

RabbitMQ, no Docker Compose. As duas filas lógicas ficam nele. O painel do RabbitMQ mostra as mensagens sem depender da nuvem.

### Nuvem

O fluxo é o mesmo: a Video API publica o processamento, o processor publica o status, a Video API aplica no PostgreSQL e, no erro, envia o e-mail. O produto na nuvem ainda não foi escolhido. Os serviços continuam falando com a fila pelo mesmo contrato.

## Redis

O Redis não substitui o PostgreSQL nem o storage. Dois usos.

### Listagem de status

Dono do uso: Video API.

Guarda, por usuário, a lista dos vídeos com identificador, status e, quando concluído, a referência do ZIP. O endpoint de listagem exige token e lê essa lista.

A Video API atualiza essa entrada quando consome a fila de status (começou, sucesso ou erro) e quando aceita um vídeo novo. O PostgreSQL continua sendo o registro. Se o Redis não tiver a lista, a Video API lê o PostgreSQL e preenche o Redis de novo.

### Um vídeo por vez

Dono do uso: Video Processor API.

Antes de quebrar o vídeo, o processor marca o identificador no Redis. Outro processor que receber a mesma mensagem vê a marca e não processa esse vídeo. Vídeos diferentes seguem em paralelo. A marca sai no sucesso e no erro.

## Ambiente local

Um Docker Compose sobe a infraestrutura. Os três serviços .NET apontam para ela.

- **PostgreSQL:** banco de usuários e banco de vídeos.
- **Redis:** listagem de status e marca de vídeo em processamento.
- **RabbitMQ:** fila de processamento e fila de status.
- **MinIO:** storage do vídeo original e do ZIP. A API é a do S3.
- **Prometheus:** coleta as métricas dos serviços.
- **Grafana:** lê o Prometheus e mostra o estado do sistema.

## Tecnologia

- **Repositório:** [Fase5-FiapX](https://github.com/CavalheiroCosta/Fase5-FiapX), em `Fase5-FiapX`, no mesmo nível de `Planning`. O trabalho acontece em branch. A `main` recebe o código pelo merge da branch.
- **Serviços:** .NET para Auth API, Video API e Video Processor API, no mesmo repositório.
- **Referências de CI/CD:** `Planning/Referencias` (Fase4-Infra, Fase4-Billing, Fase4-Execution, Fase4-BD). O foco agora é o ambiente local. O pipeline precisa existir para a demonstração.
- **Banco:** PostgreSQL. Um banco de usuários e um banco de vídeos.
- **Fila:** RabbitMQ no local, com as filas de processamento e de status. Na nuvem, o mesmo contrato e outro produto, ainda sem escolha.
- **Storage:** MinIO no local. Vídeo original e ZIP. API compatível com S3.
- **Redis:** listagem de status na Video API e marca de um vídeo por vez no processor.
- **Monitoria:** Prometheus e Grafana no mesmo Compose.
- **Acesso:** token emitido pela Auth API e exigido na requisição dos endpoints de envio, listagem e download.
- **Execução:** tudo roda local, sem depender da nuvem.
- **Testes:** unitários em tudo, cobertura mínima de 80%.
