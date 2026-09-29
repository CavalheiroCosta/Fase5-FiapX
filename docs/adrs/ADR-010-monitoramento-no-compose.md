# ADR-010 — Monitoramento no Compose

## Status

Aceita.

## Contexto

A Feature 2 mostra, no local, a Auth, o envio do vídeo, os dois PostgreSQL, o MinIO e o RabbitMQ. O processor ainda não existe. O teste unitário não sobe essa stack.

O Docker Hub não publica mais `minio/minio`. A imagem que o Compose usava deixou de poder ser baixada.

## Decisão

A Auth e a Video expõem `GET /metrics` sem token. A instrumentação é a do ASP.NET Core no OpenTelemetry. O exportador é `OpenTelemetry.Exporter.Prometheus.AspNetCore` `1.19.1-beta.1`, a versão que acompanha o OpenTelemetry `1.19`. Não há versão estável desse exportador.

O Prometheus raspa, na rede do Compose:

- `auth:8080/metrics` e `video:8080/metrics`
- um `postgres-exporter` para cada banco, na porta `9187`
- `minio:9000/minio/v2/metrics/cluster` e `minio:9000/minio/v2/metrics/bucket`
- `rabbitmq:15692/metrics/per-object`, que é onde a fila `processamento` aparece

O Grafana sobe no mesmo Compose, com datasource e o dashboard `FIAP X` em arquivo. A feature seguinte acrescenta painel nesse arquivo. Não nasce outro stack.

No host, o Prometheus fica em `http://localhost:9090` e o Grafana em `http://localhost:3000`, usuário `fiapx`, senha `fiapx`. O passo a passo está em `docs/monitoramento.md`.

O MinIO local passa a ser `alpine/minio:RELEASE.2025-10-15T17-29-55Z`. A métrica do MinIO é pública. O processo roda como root para gravar o volume. A saúde usa `wget` em `/minio/health/live`, porque essa imagem não traz o `mc`.

O teste pede `/metrics` e não sobe Prometheus, Grafana, banco, MinIO nem RabbitMQ.

## Consequências

Dá para ver API, banco, storage e fila sem o processor. A cobertura continua sem depender do Compose.
