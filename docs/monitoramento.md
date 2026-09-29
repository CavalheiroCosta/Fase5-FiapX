# Como verificar o monitoramento

Prometheus coleta o que já existe. O Grafana mostra isso num painel só. O processor, o Redis, a fila `status`, a listagem, o download e o e-mail ainda não entram aqui. A feature seguinte acrescenta painel no mesmo dashboard, em `compose/monitoramento/grafana/dashboards/fiapx.json`.

```powershell
docker compose up -d
```

| Onde | Endereço | O que confere |
| --- | --- | --- |
| Prometheus | `http://localhost:9090/targets` | Cada alvo em UP |
| Auth | `http://localhost:5298/metrics` | Texto de métrica, sem token |
| Video API | `http://localhost:5299/metrics` | Texto de métrica, sem token |
| Grafana | `http://localhost:3000` | Usuário `fiapx`, senha `fiapx`, dashboard `FIAP X` |

Os alvos em UP são `auth`, `video`, `postgres-usuarios`, `postgres-videos`, `minio`, `minio-bucket` e `rabbitmq`.

## O que cada painel mostra

O intervalo do gráfico é de 1 minuto. Uma chamada aparece e depois sai.

- **Auth — requisições por rota.** A série `/metrics` aparece sozinha, porque o Prometheus raspa a Auth. `POST http://localhost:5298/login` com `{"login":"Adm","senha":"Adm"}` faz surgir `login`. `GET /usuarios` com o token faz surgir `usuarios`.
- **Video — envio.** Fica vazio até um `POST http://localhost:5299/videos` com o campo `arquivo` e `Authorization: Bearer`. A série é `videos`.
- **Postgres usuários** e **Postgres vídeos.** `no ar` quando o exportador alcança o banco. `fora` quando não alcança.
- **Tamanho dos bancos.** Bytes de `fiapx_usuarios` e `fiapx_videos`.
- **MinIO.** `saudável` quando o cluster responde.
- **MinIO — bytes recebidos no bucket videos.** Sobe no envio. O contador é o tráfego recebido pelo bucket `videos`.
- **RabbitMQ — fila processamento.** Quantidade de mensagens na fila `processamento`. Sobe depois do envio e fica, porque ninguém consome essa fila neste corte.

## Fora deste painel

Processor, Redis, fila `status`, listagem, download e e-mail. A decisão do que é raspado está em `docs/adrs/ADR-010-monitoramento-no-compose.md`.
