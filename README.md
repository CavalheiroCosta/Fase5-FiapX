# Fase5-FiapX

Monorepo do processamento de vídeos da FIAP X. Três serviços .NET: Auth API, Video API e Video Processor API.

O desenho do sistema está em [`planning/planning.md`](planning/planning.md). A ordem do trabalho está em [`planning/Fiapx-todo.md`](planning/Fiapx-todo.md). O guia do agente está em [`AGENTS.md`](AGENTS.md). As decisões ficam em [`docs/adrs`](docs/adrs/README.md).

## Serviços

| Serviço | Estado | Guia |
| --- | --- | --- |
| Auth API | Host no ar. O próximo passo é o cadastro de usuários. | [`planning/auth.md`](planning/auth.md) |
| Video API | Ainda não existe neste repositório. | [`planning/planning.md`](planning/planning.md) |
| Video Processor API | Ainda não existe neste repositório. | [`planning/planning.md`](planning/planning.md) |

A Auth usa as camadas Api, Application, Domain e Infra. Hoje só a Api existe, em `src/Auth/Api`. Cada projeto tem o próprio `README.md`.

## Ambiente local

O primeiro Docker Compose sobe o PostgreSQL de usuários, junto com o cadastro da Auth. Banco de vídeos, Redis, RabbitMQ, MinIO, Prometheus e Grafana entram depois. Ainda não há Compose neste repositório.

## Testes

Na raiz do repositório:

```powershell
dotnet test tst/Auth/Tests/Auth.Tests.csproj
```

A cobertura de linhas da Auth fica em pelo menos 80%. O workflow em `.github/workflows/ci.yml` restaura, compila em Release, roda esses testes em OpenCover, espera o quality gate do SonarCloud e compila a imagem local `fase5-auth:ci`. A imagem não é publicada.
