# TODO — FIAP X

O que vamos fazer, na ordem. O desenho completo está em `planning/planning.md`.

O CI e a Auth API estão feitos. Daqui para a frente o trabalho é por feature. Cada feature deixa o ambiente local observável: o que ela grava dá para ver na hora, e a monitoria já cobre o que existe. A feature seguinte acrescenta dado nesse mesmo lugar.

## CI

Um workflow no monorepo, em `.github/workflows/ci.yml`. A referência é o job de CI da Fase 4 (Billing e Execution): build, teste com cobertura, SonarCloud e build da imagem. Publicar a imagem e fazer deploy só acontecem no push da `main`, e ficam fora deste corte.

O primeiro corte prova a Auth. O pull request quebra se ela não compila ou se a cobertura fica abaixo de 80%. Sonar e imagem entram depois, no mesmo workflow.

### Agora

- [x] Solution .NET 10, como na Fase 4, com o projeto da Auth API e o projeto de testes dela.
- [x] O workflow dispara em pull request para a `main`, em push na `main` e por `workflow_dispatch`.
- [x] Passos: checkout, setup do .NET 10, restore e build em Release.
- [x] Testes da Auth com cobertura no formato OpenCover.
- [x] O próprio workflow lê o relatório e falha se a cobertura de linhas da Auth for menor que 80%. Não depende do Sonar para barrar o pull request.
- [x] Esse job termina antes do merge.

### Em seguida, no mesmo workflow

- [x] SonarCloud com quality gate, no modelo da Fase 4. Exige `SONAR_TOKEN` e as variáveis do projeto. O workflow espera o quality gate. A cobertura de linhas da Auth continua barrando o pull request sem depender do Sonar.
- [x] Build da imagem Docker da Auth no CI, sem publicar. A tag local `fase5-auth:ci` prova que a imagem nasce. Publicação e deploy continuam fora.

### Fora deste corte

- Publicar a imagem no registro.
- Deploy.

## Auth API

Serviço .NET. Cadastra os usuários, cuida do acesso e emite o token. Não processa vídeo. O guia deste corte está em `planning/auth.md`.

A conta administradora é login `Adm`, senha `Adm`, nome `Administrador` e e-mail `adm@adm.com`. Ela cadastra os demais usuários, com login, senha, nome e e-mail. Não há autocadastro.

### Agora

- [x] Camadas Api, Application, Domain e Infra, no modelo da Fase 4.
- [x] Cadastro sem token: criar, consultar, listar, alterar e remover.
- [x] Compose com o PostgreSQL de usuários. A Auth grava nele. Banco de vídeos, fila, MinIO, Redis e monitoria ficam fora. A decisão está em `docs/adrs/ADR-003-postgres-usuarios-no-compose.md`.
- [x] Persistir Guid, login, nome, e-mail, hash da senha e acesso nesse banco, atrás de uma interface. O Guid nasce na gravação. A senha usa o PasswordHasher do .NET. A conta `Adm` nasce com o serviço.
- [x] Testes unitários do cadastro e das recusas, dentro da cobertura de 80%.

### Em seguida

- [x] Login com usuário e senha. Esse endpoint não exige token.
- [x] Senha errada ou usuário inexistente não emite token.
- [x] Token válido carrega o usuário e o e-mail.
- [x] Leitura do token, recuperando o usuário e o e-mail.
- [x] Cadastro passa a exigir o token da conta `Adm`.
- [x] Testes unitários do login, da recusa e da leitura do token, dentro da cobertura de 80%.

### Decidido

- O token é um JWT assinado com HMAC, válido por 30 minutos, com login e e-mail. A decisão está em `docs/adrs/ADR-006-token-jwt-hmac.md`.
- Não há revogação. Expirado, o usuário autentica de novo.

## Features

A ordem é esta. O preparo da próxima feature está em `planning/handover.md`. Publicação da imagem e deploy ficam depois da Feature 7.

### Feature 1 — Upload do vídeo

A Video API recebe o vídeo numa requisição com o token da Auth. O arquivo vai para o storage. A fila de processamento recebe só a referência, não o binário. Quem consome essa fila ainda não existe neste corte.

O planning já define a ordem: o vídeo é registrado no Postgres de vídeos como aguardando processamento e, depois disso, a Video API publica o identificador e o caminho. Se a publicação falhar, o vídeo continua pendente.

No local, o storage é o MinIO e a fila é o RabbitMQ, os dois no Compose. Dá para ver o resultado sem a nuvem:

- O console do MinIO mostra o arquivo enviado. A API do storage continua sendo a do S3.
- O painel do RabbitMQ mostra a mensagem na fila de processamento. A mensagem traz o identificador e o caminho.

- [x] A Video API recebe o vídeo com o token.
- [x] Grava o arquivo no MinIO.
- [x] Registra o vídeo no Postgres de vídeos como aguardando, antes de publicar.
- [x] Publica na fila de processamento o identificador e o caminho.
- [x] O Compose sobe MinIO e RabbitMQ com esta feature.
- [x] No console do MinIO, o arquivo enviado aparece.
- [x] No painel do RabbitMQ, a mensagem aparece na fila de processamento.
- [x] Testes unitários, com storage e fila atrás de interface, dentro da cobertura de 80%.
- [x] A coleção do Postman envia o vídeo com o token gravado no login.

Fora desta feature: consumir a fila, quebrar o vídeo, gerar o ZIP, fila de status, listagem, download e e-mail.

### Feature 2 — Monitoramento

Prometheus e Grafana entram no mesmo Compose e passam a mostrar o que já existe: Auth API, upload do vídeo, PostgreSQL, MinIO e RabbitMQ. O painel não espera o sistema inteiro.

Cada feature nova acrescenta métrica nesse mesmo Grafana. O monitoramento não recomeça.

- [x] Prometheus e Grafana sobem no Compose.
- [x] Auth API e Video API expõem métricas.
- [x] O Grafana mostra API, banco, storage e fila do que esta entrega já tem.
- [x] A feature seguinte encaixa a métrica dela nesse painel, sem outro stack de monitoria.
- [x] A coleção do Postman lê `/metrics` da Auth e da Video e confere os alvos do Prometheus.

### Feature 3 — Processor

A Video Processor API consome a fila `processamento`, marca o vídeo no Redis, grava o ZIP no MinIO e publica o resultado na fila `status`. A Video API ainda não aplica esse resultado. O processor não escreve no Postgres e não envia e-mail.

No local, o ZIP aparece no console do MinIO e a mensagem aparece no painel do RabbitMQ. A marca fica no Redis enquanto o trabalho dura.

O serviço nasce em `src/Processor`, nas quatro camadas. ffmpeg, storage, fila e Redis ficam atrás de interface.

- [x] Consome a mensagem já publicada: `id` e `caminho`.
- [x] Marca o identificador no Redis antes de trabalhar. Se a marca já existir, não processa de novo. A marca sai no sucesso e no erro.
- [x] A fila durável se chama `status`. A mensagem é JSON com `id` e `momento` (`comecou`, `sucesso` ou `erro`). No `sucesso`, também o `caminho` do ZIP.
- [x] Publica `comecou` antes do término. Em seguida publica `sucesso` ou `erro`.
- [x] Lê o vídeo no MinIO, extrai os frames e grava o ZIP. A chave do objeto é `{id}/{id}.zip`. O caminho publicado é `videos/{id}/{id}.zip`. Sucesso só com o ZIP já salvo.
- [x] Erro de processamento não volta para a fila `processamento`. Não há nova tentativa neste corte.
- [x] O Compose sobe o processor e o Redis, com um consumidor.
- [x] No console do MinIO, o ZIP aparece. No painel do RabbitMQ, a mensagem aparece na fila `status`.
- [x] Testes unitários, dentro da cobertura de 80%. O CI passa a barrar o pull request pela cobertura de linhas do processor.
- [x] A métrica do processor entra no Grafana da Feature 2.
- [x] O contrato da fila `status` vira ADR.

Fora desta feature: aplicar o status no Postgres, listagem, download, e-mail e mais de um processor no Compose.

### Feature 4 — Status no registro

A Video API consome a fila `status` e atualiza o Postgres de vídeos. O processor continua sem escrever no banco.

Os status gravados seguem o valor que já existe no envio: `em_processamento`, `concluido` e `erro`, ao lado de `aguardando_processamento`.

- [x] `comecou` passa o vídeo para `em_processamento`.
- [x] `sucesso` passa para `concluido` e grava a referência do ZIP.
- [x] `erro` passa para `erro`.
- [x] `sucesso` e `erro` fecham o vídeo mesmo se o `comecou` não tiver sido aplicado.
- [x] A mesma mensagem, entregue de novo, não reabre um vídeo já `concluido` ou `erro`.
- [x] No banco `fiapx_videos`, o status muda. No sucesso, a referência do ZIP deixa de ser nula. A fila `status` esvazia.
- [x] Testes unitários, com a fila atrás de interface, dentro da cobertura de 80%.
- [x] A métrica desse consumo entra no mesmo Grafana.

Fora desta feature: Redis de listagem, endpoint de listagem, download e e-mail.

### Feature 5 — Listagem no Redis

A listagem que o usuário vê sai do Redis. O PostgreSQL continua sendo o registro. O Redis desta feature é o mesmo que a Feature 3 subiu para a marca do processor.

A Video API guarda, pelo login do token, a lista com identificador, status e, quando `concluido`, o caminho do ZIP. Ela atualiza essa entrada ao aceitar um vídeo e ao aplicar a fila `status`.

- [x] `GET /videos` exige o token e devolve só os vídeos daquele login.
- [x] A lista é lida do Redis.
- [x] Se o Redis não tiver a lista, a Video API lê o Postgres de vídeos e preenche o Redis de novo.
- [x] O envio e a aplicação de status passam a atualizar essa entrada.
- [x] Sem token válido, não há listagem.
- [x] Testes unitários, com o Redis atrás de interface, dentro da cobertura de 80%.
- [x] A coleção do Postman lista os vídeos com o token gravado no login.
- [x] A métrica da listagem entra no mesmo Grafana.

Fora desta feature: download e e-mail.

### Feature 6 — Download do ZIP

A Video API entrega o ZIP quando o status é `concluido`. O arquivo sai do storage pela referência guardada no Postgres de vídeos. A listagem da Feature 5 já mostra esse caminho.

- [x] `GET /videos/{id}/download` exige o token.
- [x] Com status `concluido`, a resposta é o ZIP daquele vídeo.
- [x] Sem token válido, não há download.
- [x] Vídeo de outro login, vídeo inexistente ou status diferente de `concluido` não entrega o arquivo.
- [x] Testes unitários, com o storage atrás de interface, dentro da cobertura de 80%.
- [x] A coleção do Postman baixa o ZIP com o token gravado no login.
- [x] A métrica do download entra no mesmo Grafana.

Fora desta feature: baixar o vídeo original e e-mail de erro.

### Feature 7 — E-mail de erro

A Video API avisa o dono depois que o status do vídeo já está `erro`. O processor não envia esse e-mail. O destinatário é o e-mail gravado no registro do vídeo.

No local, a mensagem fica num capturador SMTP no mesmo Compose. O produto é o Mailpit. A mensagem não sai da máquina.

- [ ] O envio acontece depois da gravação do status `erro`.
- [ ] Se o envio falha, o status permanece `erro`. Não há outra fila para o e-mail.
- [ ] O Compose sobe o Mailpit com esta feature.
- [ ] No painel do Mailpit, a mensagem aparece para o e-mail do dono.
- [ ] Testes unitários, com o e-mail atrás de interface, dentro da cobertura de 80%.
- [ ] A métrica do envio entra no mesmo Grafana.

Fora desta feature: publicação da imagem e deploy.
