# Fase5-FiapX

Monorepo do processamento de vídeos da FIAP X. A pessoa envia um vídeo. Dois processors extraem um frame por segundo, gravam um ZIP e a Video API devolve esse arquivo.

Três serviços .NET e um front sobem juntos no Docker Compose, com a infraestrutura. O desenho longo está em [`planning/planning.md`](planning/planning.md). As decisões estão em [`docs/adrs`](docs/adrs/README.md). O guia para apresentar está em [`apresentacao/index.html`](apresentacao/index.html).

## Apresentação

O vídeo da entrega está em [`apresentacao/video/FIAP X — VideoApresenta.mp4`](apresentacao/video/FIAP%20X%20%E2%80%94%20VideoApresenta.mp4).

O guia publicado na `main` abre direto no navegador:

https://htmlpreview.github.io/?https://raw.githubusercontent.com/CavalheiroCosta/Fase5-FiapX/main/apresentacao/index.html

Na máquina, a mesma pasta abre sem servidor:

```powershell
start .\apresentacao\index.html
```

As setas do teclado trocam de página. A ordem é arquitetura, domínio, nuvem, tecnologias e como executar.

## Arquitetura

O front fala com a Auth e com a Video API. O binário fica no MinIO. As filas carregam referência e resultado. Dois processors consomem a mesma fila. A Video API é a dona do registro.

| Serviço | Descrição | Onde olhar |
| --- | --- | --- |
| Front | Tela em que a pessoa entra, cadastra usuários, envia o vídeo, acompanha o andamento e baixa o ZIP. | `http://localhost:5173` |
| Auth API | Reconhece quem pode usar o sistema e emite o token de acesso. Não processa vídeo. | `http://localhost:5298` |
| Video API | Recebe o vídeo, acompanha o status, lista os envios daquele login, entrega o ZIP e avisa o dono quando o processamento falha. | `http://localhost:5299` |
| Processor | Lê o vídeo, extrai um frame por segundo, gera o ZIP e devolve o resultado. Não atende o usuário. | `http://localhost:5300` e `http://localhost:5301` |
| Postgres usuários | Guarda o cadastro: login, nome, e-mail e a senha protegida. | `localhost:5432`, banco `fiapx_usuarios`, usuário `fiapx`, senha `fiapx` |
| Postgres vídeos | Guarda o registro de cada vídeo: dono, status e a referência dos arquivos. O binário não fica aqui. | `localhost:5433`, banco `fiapx_videos`, usuário `fiapx`, senha `fiapx` |
| MinIO | Mantém os arquivos dos vídeos enviados e o ZIP gerado após o processamento. | API `http://localhost:9000`, console `http://localhost:9001`, usuário `fiapx`, senha `fiapxfiapx` |
| RabbitMQ | Leva o pedido de processamento e o resultado de volta. A mensagem traz a referência, não o arquivo. | painel `http://localhost:15672`, usuário `fiapx`, senha `fiapx` |
| Redis | Guarda a listagem que o usuário vê e a marca que impede o mesmo vídeo em dois processors ao mesmo tempo. | `localhost:6379`, senha `fiapx` |
| Mailpit | Recebe o e-mail de aviso quando um vídeo falha. No ambiente local a mensagem não sai da máquina. | `http://localhost:8025` |
| Prometheus | Coleta as métricas das APIs, dos bancos, da fila, do storage e do Redis. | `http://localhost:9090/targets` |
| Grafana | Mostra, num painel só, o que o Prometheus coletou. | `http://localhost:3000`, usuário `fiapx`, senha `fiapx`, dashboard `FIAP X` |

O login `Adm` / `Adm` abre o cadastro e os atalhos desses painéis. Qualquer outro login envia vídeo, lista o andamento e baixa o ZIP.

### Um vídeo, do envio ao ZIP

1. O front pede login à Auth. A senha certa devolve um token de 30 minutos. Sem token válido não há envio, lista nem download.
2. A Video API grava o arquivo no MinIO, registra o vídeo como aguardando no Postgres e só então publica o identificador e o caminho na fila.
3. Se a fila falha, o vídeo continua pendente. Se o storage falha, não nasce registro. Se o registro falha depois do arquivo, a API apaga o objeto.
4. Um processor marca o vídeo no Redis. Se a marca já existe, confirma a mensagem e não processa de novo.
5. Avisa que começou, extrai um frame por segundo e grava o ZIP. Sucesso só sai com o ZIP já salvo. Erro confirma a mensagem e não volta para a fila.
6. A Video API grava em processamento, concluído ou erro. Vídeo já fechado não reabre. A listagem sai do Redis. No concluído, o download entrega o ZIP. No erro, o e-mail sai depois do status gravado.

`aguardando_processamento` → `em_processamento` → `concluido` (download) ou `erro` (e-mail).

A Auth, a Video e o processor usam as camadas Api, Application, Domain e Infra, em `src/Auth`, `src/Video` e `src/Processor`. Cada projeto tem o próprio `README.md`. O front fica em [`src/Web`](src/Web/README.md). Como ler cada painel está em [`docs/monitoramento.md`](docs/monitoramento.md).

## Domínio

Cada contexto tem dono. A Auth não processa vídeo. O processor não escreve no Postgres e não manda e-mail. A Video API não confere senha: ela lê o JWT no próprio processo.

| Contexto | O que guarda | O que não faz |
| --- | --- | --- |
| Auth API | Usuário, credencial, acesso e token. A conta `Adm` cadastra os demais. Não há autocadastro. | Envio, frames, ZIP e e-mail. |
| Video API | Dono, status e a referência do arquivo. A listagem sai do Redis. O download só existe com `concluido`. | Conferir senha e quebrar o vídeo. |
| Processor | Frames e ZIP de um vídeo que a Video API já aceitou. | Atender o usuário, escrever no Postgres e enviar e-mail. |

| Camada | Responsabilidade |
| --- | --- |
| Api | HTTP. Recebe a requisição e devolve a resposta. |
| Application | Casos de uso. |
| Domain | Regras que não dependem de infra. |
| Infra | Postgres, storage, fila, Redis, e-mail e a assinatura do token, atrás de interface. |

As decisões que valem citar na entrega:

| ADR | Decisão |
| --- | --- |
| [001](docs/adrs/ADR-001-camadas-auth.md) | Auth, Video e processor em Api, Application, Domain e Infra. |
| [006](docs/adrs/ADR-006-token-jwt-hmac.md) | JWT HMAC-SHA256, 30 minutos, login e e-mail. Sem revogação. |
| [008](docs/adrs/ADR-008-video-le-jwt-local.md) | A Video API confere o JWT no próprio processo. Não chama a Auth. |
| [009](docs/adrs/ADR-009-contrato-upload-e-fila.md) | A fila leva `id` e `caminho`. O binário não entra na mensagem. |
| [014](docs/adrs/ADR-014-listagem-no-redis.md) | A listagem sai do Redis. O Postgres continua o registro. |
| [017](docs/adrs/ADR-017-dois-processors-no-compose.md) | Dois processors na mesma fila. A marca `marca:{id}` impede o trabalho duplicado. |

A lista das dezessete está em [`docs/adrs/README.md`](docs/adrs/README.md).

## Nuvem

O fluxo não muda. Mudam os produtos.

| Faixa | Onde roda |
| --- | --- |
| Borda | API Gateway (`/`, `/auth`, `/video`) e um ALB interno, ligado por VPC Link. |
| Serviços | EKS. Front, Auth e Video API. O processor escala pela fila `processamento`, com no mínimo dois pods. |
| Fila | RabbitMQ no ECS, fora do deploy das APIs. Filas `processamento` e `status`. |
| Dados | Dois RDS Multi-AZ (`fiapx_usuarios` e `fiapx_videos`), ElastiCache, S3 e SES. |
| Ops | Managed Prometheus, Managed Grafana e ECR. |

Na Fase 4 um RDS só, com banco e role por serviço, isolava o dado e custava menos. Pico ou falha atingiam os dois contextos. Aqui a instância é separada porque login e vídeo crescem em ritmos diferentes.

## Tecnologias

| Parte | Tecnologia |
| --- | --- |
| Serviços | .NET 10. Auth, Video e processor em Api, Application, Domain e Infra. |
| Acesso | JWT HMAC-SHA256, 30 minutos. Senha com o PasswordHasher do .NET. |
| Front | Vite 6. Página estática. No Compose, `/auth` vai para a Auth e `/video` para a Video API. |
| Banco | PostgreSQL 16. Um banco de usuários e um de vídeos. |
| Fila | RabbitMQ 3. Filas duráveis `processamento` e `status`. |
| Storage | MinIO, API compatível com S3. Bucket `videos`. |
| Processamento | ffmpeg, um frame por segundo, JPEG dentro do ZIP. |
| Cache e marca | Redis 7. `lista:{login}` e `marca:{id}`. |
| E-mail local | Mailpit. SMTP na porta 1025. |
| Monitoria | Prometheus, Grafana e os exportadores de Postgres e Redis. |
| Entrega | GitHub Actions. Imagens `fase5-auth`, `fase5-video`, `fase5-processor` e a do front. |

O CI, em [`.github/workflows/ci.yml`](.github/workflows/ci.yml), dispara em pull request para a `main`, em push na `main` e por `workflow_dispatch`. Restaura, compila em Release, roda os testes, espera o quality gate do SonarCloud e gera as imagens locais `fase5-auth:ci`, `fase5-video:ci` e `fase5-processor:ci`. A cobertura de linhas de cada serviço barra o pull request abaixo de 80%. As imagens não são publicadas.

No push da `main`, o CD desenhado continua esse pipeline: publica as quatro imagens no ECR com o SHA do commit e atualiza os Deployments no EKS. O RabbitMQ permanece no ECS. Não há job de SQL. O schema nasce na subida da Auth e da Video API.

## Como executar

Na pasta deste repositório, com o Docker em execução:

```powershell
docker compose up -d --build
```

Depois que as imagens já existem, `docker compose up -d` basta. O schema de `fiapx_usuarios` e o de `fiapx_videos` nascem na subida da Auth e da Video. Não há script SQL separado. O Compose deixa um e-mail de erro no Mailpit quando a caixa está vazia, para a apresentação.

Fora do Compose, com a Auth em `http://localhost:5298` e a Video em `http://localhost:5299`, o front também sobe em `src/Web`:

```powershell
npm install
npm run dev
```

Na câmera: entre como `Adm` / `Adm`, cadastre um usuário e abra os atalhos. Entre com esse usuário, envie mais de um vídeo, baixe o ZIP quando o status for `concluido` e, no erro, mostre o Mailpit. O Grafana é o dashboard `FIAP X`.

Os testes, na raiz:

```powershell
dotnet test tst/Auth/Tests/Auth.Tests.csproj
dotnet test tst/Video/Tests/Video.Tests.csproj
dotnet test tst/Processor/Tests/Processor.Tests.csproj
```

Storage, fila, Redis e ffmpeg ficam atrás de interface. O guia do agente está em [`AGENTS.md`](AGENTS.md). A ordem do trabalho está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md).
