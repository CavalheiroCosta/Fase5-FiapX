# Handover — próxima feature

Preparo para quem continua o monorepo. A ordem do trabalho está em `planning/Fiapx-todo.md`. O desenho está em `planning/planning.md`. As decisões já tomadas estão em `docs/adrs`. O guia do agente está em `AGENTS.md`.

A próxima feature é a **Feature 2 — Monitoramento**. O upload do vídeo já existe.

## O que já está pronto

- CI em `.github/workflows/ci.yml`: build Release, testes OpenCover da Auth e da Video, falha abaixo de 80% de linhas em cada uma, SonarCloud e imagens locais `fase5-auth:ci` e `fase5-video:ci`. As imagens não são publicadas. Deploy fica fora.
- Auth API em `src/Auth`. Login emite JWT HMAC por 30 minutos, com login e e-mail. O cadastro exige o token da conta `Adm`.
- Video API em `src/Video`, nas quatro camadas. `POST /videos` exige o token. A ordem é MinIO, registro como aguardando no Postgres de vídeos, fila `processamento` com identificador e caminho. Se a fila falha, o vídeo continua pendente. As decisões estão em `docs/adrs/ADR-007-postgres-videos-no-compose.md`, `docs/adrs/ADR-008-video-le-jwt-local.md` e `docs/adrs/ADR-009-contrato-upload-e-fila.md`.
- O Compose sobe o PostgreSQL de usuários, o PostgreSQL de vídeos, a Auth, a Video API, o MinIO e o RabbitMQ.
- A coleção em `Hacka/postman`, fora deste repositório, faz o login e o envio. O environment `Hacka-FiapX-Local` guarda `baseUrl` da Auth, `videoBaseUrl` da Video API e o `token`. O request `Enviar vídeo` usa o campo `arquivo` e grava `videoId` e `caminho`.

```powershell
docker compose up -d
```

| Serviço | Endereço |
| --- | --- |
| Auth | `http://localhost:5298`, banco `fiapx_usuarios` |
| Video API | `http://localhost:5299`, banco `fiapx_videos` em `localhost:5433` |
| MinIO | API `http://localhost:9000`, console `http://localhost:9001`, usuário `fiapx`, senha `fiapxfiapx`, bucket `videos` |
| RabbitMQ | painel `http://localhost:15672`, usuário `fiapx`, senha `fiapx`, fila `processamento` |

Fora do Compose, em desenvolvimento, a Auth grava em `localhost:5432` e a Video API usa as portas do host acima.

Os testes não sobem PostgreSQL, MinIO nem RabbitMQ. Storage e fila ficam atrás de interface. A cobertura de linhas da Auth e a da Video ficam em pelo menos 80%.

## O que a Feature 2 entrega

Prometheus e Grafana entram no mesmo Compose e passam a mostrar o que já existe: Auth API, upload do vídeo, PostgreSQL, MinIO e RabbitMQ. O painel não espera o processor.

Cada feature nova acrescenta métrica nesse mesmo Grafana. O monitoramento não recomeça.

- A Auth API e a Video API expõem métricas.
- O Grafana mostra API, banco, storage e fila do que esta entrega já tem.

## O que não entra nesta feature

Não implemente:

- Consumir a fila, quebrar o vídeo, gerar o ZIP ou gravar o ZIP.
- Fila de status, listagem, download, e-mail de erro e Redis.
- Revogação de token, autocadastro e papel extra no JWT.
- Publicação da imagem e deploy.
- A pasta `Hacka/Planning/Referencias`. Ela fica fora deste repositório.

## Como continuar

- Trabalhe em branch. Não faça commit na `main`. A `main` recebe código pelo merge do pull request.
- Mensagem: `Tag(projeto): O que foi feito`. Um projeto por commit. Monitoria que atravessa serviços usa `Arq`. Métrica dentro da Auth usa `Auth`. Métrica dentro da Video usa `Video`. Documentação de `planning/` usa `Doc`.
- O título do pull request usa o mesmo formato. O corpo segue `.github/PULL_REQUEST_TEMPLATE.md`.
- Decisão nova de arquitetura vira ADR em `docs/adrs` e uma linha em `docs/adrs/README.md`, no mesmo pull request.
- Ao fechar a feature, marque os itens da Feature 2 em `planning/Fiapx-todo.md` e atualize este handover para o corte seguinte.
