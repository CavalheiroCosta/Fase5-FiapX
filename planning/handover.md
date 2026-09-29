# Handover — próxima feature

Preparo para quem continua o monorepo. A ordem do trabalho está em `planning/Fiapx-todo.md`. O desenho está em `planning/planning.md`. As decisões já tomadas estão em `docs/adrs`. O guia do agente está em `AGENTS.md`.

As features deste corte estão feitas. A Video API já avisa o dono quando o vídeo fica `erro`. Publicação da imagem e deploy ficam fora até serem pedidos.

## O que já está pronto

- CI em `.github/workflows/ci.yml`: build Release, testes OpenCover da Auth, da Video e do processor, falha abaixo de 80% de linhas em cada um, SonarCloud e imagens locais `fase5-auth:ci`, `fase5-video:ci` e `fase5-processor:ci`. As imagens não são publicadas. Deploy fica fora.
- Auth API em `src/Auth`. Login emite JWT HMAC por 30 minutos, com login e e-mail. O cadastro exige o token da conta `Adm`. `GET /metrics` não exige token.
- Video API em `src/Video`, nas quatro camadas. `POST /videos` exige o token. A ordem é MinIO, registro como aguardando no Postgres de vídeos, fila `processamento` com identificador e caminho. Se a fila falha, o vídeo continua pendente. Ela consome a fila `status` e grava `em_processamento`, `concluido` ou `erro`. Vídeo já fechado não reabre. `GET /videos` exige o token e lê a lista `lista:{login}` no Redis. Se a chave não existe, lê o Postgres de vídeos e preenche o Redis. O envio e o status atualizam a entrada quando a chave já existe. `GET /videos/{id}/download` exige o token e entrega o ZIP do storage quando o status é `concluido`. A referência sai do Postgres. Outro login, vídeo inexistente ou status diferente de `concluido` respondem 404. Depois de gravar `erro`, a Video API envia um e-mail ao endereço do registro. Se o envio falha, o status permanece `erro`. `GET /metrics` não exige token. As decisões estão em `docs/adrs/ADR-013-aplica-status-no-registro.md`, `docs/adrs/ADR-014-listagem-no-redis.md`, `docs/adrs/ADR-015-download-do-zip.md` e `docs/adrs/ADR-016-email-de-erro.md`.
- Processor em `src/Processor`, nas quatro camadas. Consome `processamento`, marca `marca:{id}` no Redis, extrai um frame por segundo com ffmpeg, grava o ZIP e publica em `status`. Erro confirma a mensagem e não volta para `processamento`. `GET /metrics` não exige token. O diagrama está em `src/Processor/Api/README.md`. As decisões estão em `docs/adrs/ADR-011-contrato-fila-status.md` e `docs/adrs/ADR-012-metricas-do-processor.md`.
- O Compose sobe o PostgreSQL de usuários, o PostgreSQL de vídeos, a Auth, a Video API, o processor, o MinIO, o RabbitMQ, o Redis, o Mailpit, o Prometheus e o Grafana.
- A coleção em `Hacka/postman`, fora deste repositório, faz o login, o envio, a listagem, o download e a leitura de `/metrics`. O environment `Hacka-FiapX-Local` guarda `baseUrl`, `videoBaseUrl`, `processorBaseUrl`, `prometheusUrl`, `grafanaUrl`, `mailpitUrl` e o `token`. `Métricas da Auth`, `Métricas da Video` e `Métricas do processor` não exigem token. `Alvos do Prometheus` confere os jobs em UP, inclusive `processor` e `redis`.
- O dashboard `FIAP X` mostra API, banco, storage, fila, processor, Redis, o status aplicado pela Video, a listagem, o download e o e-mail de erro. A prova está em `docs/monitoramento.md`.

```powershell
docker compose up -d
```

| Serviço | Endereço |
| --- | --- |
| Auth | `http://localhost:5298`, banco `fiapx_usuarios` |
| Video API | `http://localhost:5299`, banco `fiapx_videos` em `localhost:5433` |
| Processor | `http://localhost:5300` |
| MinIO | API `http://localhost:9000`, console `http://localhost:9001`, usuário `fiapx`, senha `fiapxfiapx`, bucket `videos` |
| RabbitMQ | painel `http://localhost:15672`, usuário `fiapx`, senha `fiapx`, filas `processamento` e `status` |
| Redis | `localhost:6379`, senha `fiapx` |
| Mailpit | painel `http://localhost:8025`, SMTP `localhost:1025` |
| Prometheus | `http://localhost:9090/targets` |
| Grafana | `http://localhost:3000`, usuário `fiapx`, senha `fiapx`, dashboard `FIAP X` |

Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432` e a Video API e o processor usam as portas do host acima.

Os testes não sobem PostgreSQL, MinIO, RabbitMQ, Redis, Prometheus nem Grafana. Storage, fila, Redis e ffmpeg ficam atrás de interface. A cobertura de linhas da Auth, a da Video e a do processor ficam em pelo menos 80%.

## O que vem depois

Publicação da imagem e deploy ficam fora até serem pedidos. Não há outra feature neste corte.

## O que não entra

Não implemente:

- Baixar o vídeo original.
- Mais de um processor no Compose.
- Revogação de token, autocadastro e papel extra no JWT.
- Publicação da imagem e deploy.
- A pasta `Hacka/Planning/Referencias`. Ela fica fora deste repositório.

## Como continuar

- Trabalhe em branch. Não faça commit na `main`. A `main` recebe código pelo merge do pull request.
- Mensagem: `Tag(projeto): O que foi feito`. Um projeto por commit. A Video API usa `Video`. Compose e painel usam `Arq`. Documentação de `planning/` usa `Doc`.
- O título do pull request usa o mesmo formato. O corpo segue `.github/PULL_REQUEST_TEMPLATE.md`.
- Decisão nova de arquitetura vira ADR em `docs/adrs` e uma linha em `docs/adrs/README.md`, no mesmo pull request.
