# Fase5-FiapX

Monorepo do processamento de vídeos da FIAP X. Três serviços .NET: Auth API, Video API e Video Processor API.

O desenho do sistema está em [`planning/planning.md`](planning/planning.md). A ordem do trabalho está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md). O guia do agente está em [`AGENTS.md`](AGENTS.md). As decisões ficam em [`docs/adrs`](docs/adrs/README.md).

## Serviços

| Serviço | Estado | Guia |
| --- | --- | --- |
| Auth API | Login emite JWT HMAC. O cadastro exige o token da conta `Adm`. | [`planning/auth.md`](planning/auth.md) |
| Video API | Ainda não existe neste repositório. | [`planning/planning.md`](planning/planning.md) |
| Video Processor API | Ainda não existe neste repositório. | [`planning/planning.md`](planning/planning.md) |

A Auth usa as camadas Api, Application, Domain e Infra, em `src/Auth`. Cada projeto tem o próprio `README.md`.

## Ambiente local

O Compose na raiz sobe o PostgreSQL de usuários e a Auth API:

```powershell
docker compose up -d
```

A Auth fica em `http://localhost:5298` e grava no banco `fiapx_usuarios`. Banco de vídeos, Redis, RabbitMQ, MinIO, Prometheus e Grafana entram depois. Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432`.

## Testes

Na raiz do repositório:

```powershell
dotnet test tst/Auth/Tests/Auth.Tests.csproj
```

A cobertura de linhas da Auth fica em pelo menos 80%. O workflow em `.github/workflows/ci.yml` restaura, compila em Release, roda esses testes em OpenCover, espera o quality gate do SonarCloud e compila a imagem local `fase5-auth:ci`. A imagem não é publicada.
