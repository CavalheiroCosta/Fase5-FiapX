# Guia do agente

Monorepo do processamento de vídeos da FIAP X. Três serviços .NET: Auth API, Video API e Video Processor API.

Antes de mudar arquitetura ou fluxo, leia `planning/planning.md`. O corte da Auth API está em `planning/auth.md`. As decisões estão em `docs/adrs`. O enunciado está em `planning/POSTECH - SOAT - Fase 5 - Hacka.pdf`. A visão geral do repositório está no `README.md`. Cada projeto tem o próprio `README.md`.

## Git

- Trabalhe sempre em branch. Não faça commit na `main`.
- A `main` recebe código pelo merge da branch.
- Mensagem: `Tag(projeto): O que foi feito`. Tags: `Feat`, `Fix`, `Hotfix`, `Chore`. Um projeto por commit: `Arq`, `Video`, `Auth`, `Processor`, `Teste` ou `Doc`. A regra completa está em `.cursor/rules/commits.mdc`.
- Pull request da branch para a `main`, com o mesmo formato no título. O corpo segue `.github/PULL_REQUEST_TEMPLATE.md`. A regra está em `.cursor/rules/pull-requests.mdc`.
- Não copie para este repositório a pasta de referências da Fase 4. Ela fica fora, em `Hacka/Planning/Referencias`.

## Fluxo

- A conta `Adm` cadastra os usuários. O cadastro começa sem token e, depois da leitura do token, passa a exigir o token dessa conta.
- A Auth API confere usuário e senha e emite o token. O login não exige token.
- Envio, listagem e download exigem o token na requisição. O token traz o usuário e o e-mail.
- A Video API grava o vídeo no MinIO, o registro no PostgreSQL de vídeos e publica na fila de processamento só o identificador e o caminho.
- O processor consome essa fila, marca o vídeo no Redis, quebra o vídeo, grava o ZIP no MinIO e publica o resultado na fila de status.
- A Video API aplica o status no PostgreSQL e no Redis. No erro, ela envia o e-mail. O processor não escreve no banco e não envia e-mail.
- Download do ZIP só com status concluído.

## Ambiente local

O Docker Compose sobe PostgreSQL (banco de usuários e banco de vídeos), Redis, RabbitMQ, MinIO, Prometheus e Grafana. Os serviços apontam para esse compose e o sistema roda sem nuvem.

A fila na nuvem usa o mesmo contrato. O produto da nuvem ainda não foi escolhido.

## Testes

Teste unitário em tudo. Cobertura mínima de 80%. Isole fila, storage, Redis, e-mail e ffmpeg atrás de interfaces para o teste não depender deles.

## CI/CD

O pipeline entra na demonstração. O modelo está nos repositórios da Fase 4, fora deste repo. O foco atual é o ambiente local.
