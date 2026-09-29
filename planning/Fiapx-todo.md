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

A ordem é esta. O preparo da próxima feature está em `planning/handover.md`. Processor, fila de status, listagem, download, e-mail de erro, Redis, publicação da imagem e deploy ficam para as features seguintes.

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

- [ ] Prometheus e Grafana sobem no Compose.
- [ ] Auth API e Video API expõem métricas.
- [ ] O Grafana mostra API, banco, storage e fila do que esta entrega já tem.
- [ ] A feature seguinte encaixa a métrica dela nesse painel, sem outro stack de monitoria.
