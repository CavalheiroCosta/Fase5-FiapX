# Fase5-FiapX

Monorepo do processamento de vídeos da FIAP X. Três serviços .NET: Auth API, Video API e Video Processor API.

O desenho do sistema está em [`planning/planning.md`](planning/planning.md). A ordem do trabalho está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md). O guia do agente está em [`AGENTS.md`](AGENTS.md). As decisões ficam em [`docs/adrs`](docs/adrs/README.md).

## Serviços

| Serviço | Estado | Guia |
| --- | --- | --- |
| Auth API | Login emite JWT HMAC. O cadastro exige o token da conta `Adm`. | [`planning/auth.md`](planning/auth.md) |
| Video API | `POST /videos` grava no MinIO, registra como aguardando e publica a referência. | [`planning/planning.md`](planning/planning.md) |
| Video Processor API | Ainda não existe neste repositório. | [`planning/planning.md`](planning/planning.md) |

A Auth e a Video usam as camadas Api, Application, Domain e Infra, em `src/Auth` e `src/Video`. Cada projeto tem o próprio `README.md`.

## Ambiente local

O Compose na raiz sobe o PostgreSQL de usuários, o PostgreSQL de vídeos, a Auth API, a Video API, o MinIO, o RabbitMQ, o Prometheus e o Grafana:

```powershell
docker compose up -d
```

| Serviço | Onde olhar |
| --- | --- |
| Auth API | `http://localhost:5298` — banco `fiapx_usuarios` |
| Video API | `http://localhost:5299` — banco `fiapx_videos` em `localhost:5433` |
| MinIO | API `http://localhost:9000`, console `http://localhost:9001` (usuário `fiapx`, senha `fiapxfiapx`) |
| RabbitMQ | AMQP `localhost:5672`, painel `http://localhost:15672` (usuário `fiapx`, senha `fiapx`) |
| Prometheus | `http://localhost:9090/targets` |
| Grafana | `http://localhost:3000` (usuário `fiapx`, senha `fiapx`), dashboard `FIAP X` |

Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432` e a Video API aponta para essas mesmas portas do host.

O envio pede o token da Auth. O arquivo aparece no bucket `videos`, na chave `{id}/{nomeDoArquivo}`. A fila `processamento` recebe o identificador e o caminho `videos/{id}/{nomeDoArquivo}`. A Auth e a Video expõem `GET /metrics` sem token. Como ver cada painel está em [`docs/monitoramento.md`](docs/monitoramento.md). O Redis entra na Feature 3. Listagem, download do ZIP e e-mail de erro são as Features 5, 6 e 7. A ordem está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md).

## Testes

Na raiz do repositório:

```powershell
dotnet test tst/Auth/Tests/Auth.Tests.csproj
dotnet test tst/Video/Tests/Video.Tests.csproj
```

A cobertura de linhas da Auth e a da Video ficam em pelo menos 80%. O workflow em `.github/workflows/ci.yml` restaura, compila em Release, roda esses testes em OpenCover, espera o quality gate do SonarCloud e compila as imagens locais `fase5-auth:ci` e `fase5-video:ci`. As imagens não são publicadas.
