# ADR-012 — Métricas do processor no Compose

## Status

Aceita.

## Contexto

A Feature 2 já raspa Auth, Video, os dois PostgreSQL, MinIO e RabbitMQ. O processor e o Redis entram agora. O painel continua o mesmo Grafana. O teste unitário não sobe essa stack.

## Decisão

O processor expõe `GET /metrics` sem token. A instrumentação é a do ASP.NET Core no OpenTelemetry, com o mesmo exportador da Auth e da Video, mais o medidor `FiapX.Processor`.

O contador do .NET se chama `fiapx_processor_resultados`. No Prometheus ele aparece como `fiapx_processor_resultados_total`, com o rótulo `momento` (`comecou`, `sucesso`, `erro` ou `ignorado`). O gauge `fiapx_processor_em_andamento` conta o trabalho que ainda não terminou.

O Prometheus passa a raspar, na rede do Compose:

- `processor:8080/metrics`
- `redis-exporter:9121`, que publica `redis_up`

O Redis local é `redis:7-alpine`, com senha, na porta `6379` do host. O exporter é `oliver006/redis_exporter:v1.67.0`.

O dashboard `FIAP X` ganha os painéis do processor, do Redis e da fila `status`. Não nasce outro stack. A decisão do que já era raspado continua em `docs/adrs/ADR-010-monitoramento-no-compose.md`.

## Consequências

Em `http://localhost:9090/targets`, os jobs `processor` e `redis` ficam UP junto dos que já existiam. O contador e a fila `status` permanecem depois do trabalho. O scrape é de 15 segundos. A chave `marca:{id}` só existe enquanto o ffmpeg roda.

O teste pede `/metrics` e não sobe Prometheus, Grafana, Redis, MinIO nem RabbitMQ.
