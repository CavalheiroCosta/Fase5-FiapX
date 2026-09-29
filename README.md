# Fase5-FiapX

Monorepo do processamento de vídeos da FIAP X. Três serviços .NET: Auth API, Video API e Video Processor API.

O desenho do sistema está em [`planning/planning.md`](planning/planning.md). A ordem do trabalho está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md). O guia do agente está em [`AGENTS.md`](AGENTS.md). As decisões ficam em [`docs/adrs`](docs/adrs/README.md).

## Serviços

| Serviço | Estado | Guia |
| --- | --- | --- |
| Auth API | Login emite JWT HMAC. O cadastro exige o token da conta `Adm`. | [`planning/auth.md`](planning/auth.md) |
| Video API | `POST /videos` grava no MinIO, registra como aguardando e publica a referência. Consome a fila `status` e grava o status no Postgres. `GET /videos` lista o login a partir do Redis. | [`planning/planning.md`](planning/planning.md) |
| Video Processor API | Consome a fila `processamento`, marca no Redis, grava o ZIP e publica na fila `status`. | [`planning/planning.md`](planning/planning.md) |

A Auth, a Video e o processor usam as camadas Api, Application, Domain e Infra, em `src/Auth`, `src/Video` e `src/Processor`. Cada projeto tem o próprio `README.md`.

## Ambiente local

O Compose na raiz sobe o PostgreSQL de usuários, o PostgreSQL de vídeos, a Auth API, a Video API, o processor, o MinIO, o RabbitMQ, o Redis, o Prometheus e o Grafana:

```powershell
docker compose up -d
```

| Serviço | Onde olhar |
| --- | --- |
| Auth API | `http://localhost:5298` — banco `fiapx_usuarios` |
| Video API | `http://localhost:5299` — banco `fiapx_videos` em `localhost:5433` |
| Processor | `http://localhost:5300` |
| MinIO | API `http://localhost:9000`, console `http://localhost:9001` (usuário `fiapx`, senha `fiapxfiapx`) |
| RabbitMQ | AMQP `localhost:5672`, painel `http://localhost:15672` (usuário `fiapx`, senha `fiapx`) |
| Redis | `localhost:6379`, senha `fiapx` |
| Prometheus | `http://localhost:9090/targets` |
| Grafana | `http://localhost:3000` (usuário `fiapx`, senha `fiapx`), dashboard `FIAP X` |

Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432` e a Video API aponta para essas mesmas portas do host.

O envio pede o token da Auth. O arquivo aparece no bucket `videos`, na chave `{id}/{nomeDoArquivo}`. A fila `processamento` recebe o identificador e o caminho `videos/{id}/{nomeDoArquivo}`. O processor consome essa fila, grava `{id}/{id}.zip` e publica na fila `status`. A Video API aplica esse resultado no Postgres. `GET /videos` exige o token e devolve a lista daquele login, lida do Redis. A Auth, a Video e o processor expõem `GET /metrics` sem token. Como ver cada painel está em [`docs/monitoramento.md`](docs/monitoramento.md). Download do ZIP e e-mail de erro são as Features 6 e 7. A ordem está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md).

## Testes

Na raiz do repositório:

```powershell
dotnet test tst/Auth/Tests/Auth.Tests.csproj
dotnet test tst/Video/Tests/Video.Tests.csproj
dotnet test tst/Processor/Tests/Processor.Tests.csproj
```

A cobertura de linhas da Auth, da Video e do processor fica em pelo menos 80%. O workflow em `.github/workflows/ci.yml` restaura, compila em Release, roda esses testes em OpenCover, espera o quality gate do SonarCloud e compila as imagens locais `fase5-auth:ci`, `fase5-video:ci` e `fase5-processor:ci`. As imagens não são publicadas.
