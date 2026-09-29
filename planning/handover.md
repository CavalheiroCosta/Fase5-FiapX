# Handover — próxima feature

Preparo para quem continua o monorepo. A ordem do trabalho está em `planning/Fiapx-todo.md`. O desenho está em `planning/planning.md`. As decisões já tomadas estão em `docs/adrs`. O guia do agente está em `AGENTS.md`.

A próxima feature é a **Feature 3 — Processor**. O upload e o monitoramento já existem.

## O que já está pronto

- CI em `.github/workflows/ci.yml`: build Release, testes OpenCover da Auth e da Video, falha abaixo de 80% de linhas em cada uma, SonarCloud e imagens locais `fase5-auth:ci` e `fase5-video:ci`. As imagens não são publicadas. Deploy fica fora.
- Auth API em `src/Auth`. Login emite JWT HMAC por 30 minutos, com login e e-mail. O cadastro exige o token da conta `Adm`. `GET /metrics` não exige token.
- Video API em `src/Video`, nas quatro camadas. `POST /videos` exige o token. A ordem é MinIO, registro como aguardando no Postgres de vídeos, fila `processamento` com identificador e caminho. Se a fila falha, o vídeo continua pendente. `GET /metrics` não exige token. As decisões estão em `docs/adrs/ADR-007-postgres-videos-no-compose.md`, `docs/adrs/ADR-008-video-le-jwt-local.md`, `docs/adrs/ADR-009-contrato-upload-e-fila.md` e `docs/adrs/ADR-010-monitoramento-no-compose.md`.
- O Compose sobe o PostgreSQL de usuários, o PostgreSQL de vídeos, a Auth, a Video API, o MinIO, o RabbitMQ, o Prometheus e o Grafana. O MinIO local é `alpine/minio:RELEASE.2025-10-15T17-29-55Z`, porque `minio/minio` saiu do Docker Hub.
- A coleção em `Hacka/postman`, fora deste repositório, faz o login, o envio e a leitura de `/metrics`. O environment `Hacka-FiapX-Local` guarda `baseUrl`, `videoBaseUrl`, `prometheusUrl`, `grafanaUrl` e o `token`. `Métricas da Auth` e `Métricas da Video` não exigem token. `Alvos do Prometheus` confere os jobs em UP. O request `Enviar vídeo` usa o campo `arquivo` e grava `videoId` e `caminho`.
- O dashboard `FIAP X` já mostra API, banco, storage e fila. A prova está em `docs/monitoramento.md`. A métrica do processor entra nesse mesmo arquivo, `compose/monitoramento/grafana/dashboards/fiapx.json`.

```powershell
docker compose up -d
```

| Serviço | Endereço |
| --- | --- |
| Auth | `http://localhost:5298`, banco `fiapx_usuarios` |
| Video API | `http://localhost:5299`, banco `fiapx_videos` em `localhost:5433` |
| MinIO | API `http://localhost:9000`, console `http://localhost:9001`, usuário `fiapx`, senha `fiapxfiapx`, bucket `videos` |
| RabbitMQ | painel `http://localhost:15672`, usuário `fiapx`, senha `fiapx`, fila `processamento` |
| Prometheus | `http://localhost:9090/targets` |
| Grafana | `http://localhost:3000`, usuário `fiapx`, senha `fiapx`, dashboard `FIAP X` |

Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432` e a Video API usa as portas do host acima.

Os testes não sobem PostgreSQL, MinIO, RabbitMQ, Prometheus nem Grafana. Storage e fila ficam atrás de interface. A cobertura de linhas da Auth e a da Video ficam em pelo menos 80%.

## O que a Feature 3 entrega

A Video Processor API consome a fila `processamento`, marca o vídeo no Redis, grava o ZIP no MinIO e publica o resultado na fila `status`. A Video API ainda não aplica esse resultado.

No local, o ZIP aparece no console do MinIO e a mensagem aparece no painel do RabbitMQ. A marca fica no Redis enquanto o trabalho dura. A métrica do processor entra no Grafana que já existe.

O checklist está em `planning/Fiapx-todo.md`.

## O que não entra nesta feature

Não implemente:

- Aplicar o status no Postgres, listagem, download e e-mail.
- Mais de um processor no Compose.
- Revogação de token, autocadastro e papel extra no JWT.
- Publicação da imagem e deploy.
- A pasta `Hacka/Planning/Referencias`. Ela fica fora deste repositório.

## Como continuar

- Trabalhe em branch. Não faça commit na `main`. A `main` recebe código pelo merge do pull request.
- Mensagem: `Tag(projeto): O que foi feito`. Um projeto por commit. O processor usa `Processor`. Compose e Redis usam `Arq`. Métrica dentro do processor usa `Processor`. Documentação de `planning/` usa `Doc`.
- O título do pull request usa o mesmo formato. O corpo segue `.github/PULL_REQUEST_TEMPLATE.md`.
- Decisão nova de arquitetura vira ADR em `docs/adrs` e uma linha em `docs/adrs/README.md`, no mesmo pull request. O contrato da fila `status` é uma dessas decisões.
- Ao fechar a feature, marque os itens da Feature 3 em `planning/Fiapx-todo.md` e atualize este handover para o corte seguinte.
