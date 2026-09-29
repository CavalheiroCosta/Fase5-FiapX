# Como verificar o monitoramento

Prometheus coleta o que já existe. O Grafana mostra isso num painel só. Publicação da imagem e deploy ficam fora. O painel está em `compose/monitoramento/grafana/dashboards/fiapx.json`.

```powershell
docker compose up -d --build
```

| Onde | Endereço | O que confere |
| --- | --- | --- |
| Prometheus | `http://localhost:9090/targets` | Cada alvo em UP |
| Auth | `http://localhost:5298/metrics` | Texto de métrica, sem token |
| Video API | `http://localhost:5299/metrics` | Texto de métrica, sem token |
| Processor | `http://localhost:5300/metrics` | Texto de métrica, sem token |
| Grafana | `http://localhost:3000` | Usuário `fiapx`, senha `fiapx`, dashboard `FIAP X` |
| Mailpit | `http://localhost:8025` | E-mail de erro. SMTP na porta `1025`. A mensagem não sai da máquina |
| Redis | `localhost:6379` | Senha `fiapx`. A chave `marca:{id}` só existe enquanto o trabalho dura. A chave `lista:{login}` guarda a listagem |

Os alvos em UP são `auth`, `video`, `processor`, `postgres-usuarios`, `postgres-videos`, `minio`, `minio-bucket`, `rabbitmq` e `redis`.

## O que cada painel mostra

O intervalo do gráfico é de 1 minuto. O scrape do Prometheus é de 15 segundos. Uma chamada HTTP aparece e depois sai. O contador do processor e a fila `status` permanecem.

- **Auth — requisições por rota.** A série `/metrics` aparece sozinha, porque o Prometheus raspa a Auth. `POST http://localhost:5298/login` com `{"login":"Adm","senha":"Adm"}` faz surgir `login`. `GET /usuarios` com o token faz surgir `usuarios`.
- **Video — envio.** Fica vazio até um `POST http://localhost:5299/videos` com o campo `arquivo` e `Authorization: Bearer`. A série é `videos`.
- **Postgres usuários** e **Postgres vídeos.** `no ar` quando o exportador alcança o banco. `fora` quando não alcança.
- **Tamanho dos bancos.** Bytes de `fiapx_usuarios` e `fiapx_videos`.
- **MinIO.** `saudável` quando o cluster responde.
- **MinIO — bytes recebidos no bucket videos.** Sobe no envio e de novo quando o ZIP é gravado.
- **RabbitMQ — fila processamento.** Quantidade de mensagens na fila `processamento`. Sobe no envio e esvazia quando o processor confirma a mensagem.
- **Processor — resultados.** Contador `fiapx_processor_resultados_total` por `momento` (`comecou`, `sucesso`, `erro` ou `ignorado`). Permanece depois do trabalho.
- **RabbitMQ — fila status.** Quantidade de mensagens na fila `status`. Sobe com `comecou` e depois `sucesso` ou `erro`, e esvazia quando a Video API confirma a mensagem.
- **Processor — em andamento.** Gauge `fiapx_processor_em_andamento`. Vale 1 enquanto o ffmpeg roda. O scrape pode não pegar um vídeo curto.
- **Redis.** `no ar` quando o exporter alcança o Redis.
- **Video — status aplicado.** Contador `fiapx_video_status_total` por `momento` (`comecou`, `sucesso`, `erro` ou `ignorado`). Permanece depois do trabalho.
- **Video — listagem.** Contador `fiapx_video_listagem_total` por `origem` (`redis` ou `postgres`). A primeira leitura de um login vem do Postgres e preenche o Redis. A seguinte vem do Redis. Permanece depois da chamada.
- **Video — download.** Contador `fiapx_video_download_total` por `resultado` (`entregue` ou `recusado`). Sobe no `GET /videos/{id}/download`. Permanece depois da chamada.
- **Video — e-mail de erro.** Contador `fiapx_video_email_total` por `resultado` (`enviado` ou `falhou`). Sobe quando a Video grava `erro` e tenta avisar o dono. Permanece depois do envio.

## Prova do processor

Gere um mp4 curto. Não versionar o arquivo.

```powershell
ffmpeg -f lavfi -i testsrc=duration=2:size=160x120:rate=1 -pix_fmt yuv420p amostra.mp4
```

1. Login `Adm` / `Adm` em `http://localhost:5298/login`.
2. `POST http://localhost:5299/videos` com o campo `arquivo` e `Authorization: Bearer`.
3. No console do MinIO (`http://localhost:9001`, usuário `fiapx`, senha `fiapxfiapx`), o bucket `videos` mostra `{id}/{id}.zip`.
4. No painel do RabbitMQ (`http://localhost:15672`, usuário `fiapx`, senha `fiapx`), a fila `processamento` esvazia. A fila `status` mostra `comecou` e depois `sucesso`, com o caminho `videos/{id}/{id}.zip`.
5. Espere um scrape. No Grafana, o contador do processor sobe. A fila `status` esvazia. O painel `Video — status aplicado` mostra `comecou` e `sucesso`.
6. Envie um arquivo que não é vídeo. A fila `status` recebe `erro` e esvazia, o ZIP não aparece e a mensagem não volta para `processamento`. A linha em `fiapx_videos` fica `erro`. No Mailpit (`http://localhost:8025`), a mensagem aparece para o e-mail do dono, com o assunto `FIAP X: o vídeo não foi processado`. O painel `Video — e-mail de erro` marca `enviado`.
7. No sucesso, a linha em `fiapx_videos` fica `concluido` e `caminho_zip` deixa de ser nulo.
8. `GET http://localhost:5299/videos` com `Authorization: Bearer` devolve o vídeo desse login. No `concluido`, o item traz `caminhoZip`. Sem token, a resposta é 401. A primeira chamada marca `postgres` no painel `Video — listagem`. A seguinte marca `redis`.
9. `GET http://localhost:5299/videos/{id}/download` com o mesmo token devolve o ZIP quando o status é `concluido`. O painel `Video — download` marca `entregue`. Sem token, a resposta é 401 e o painel marca `recusado`. Vídeo de outro login, inexistente ou ainda não `concluido` responde 404.

A lista fica no Redis depois dessa leitura:

```powershell
docker exec (docker compose ps -q redis) redis-cli -a fiapx --no-auth-warning KEYS "lista:*"
```

A marca some no sucesso e no erro. Para vê-la durante o trabalho, use um vídeo mais longo e, enquanto o ffmpeg roda:

```powershell
docker exec (docker compose ps -q redis) redis-cli -a fiapx --no-auth-warning KEYS "marca:*"
```

## Fora deste painel

Publicação da imagem e deploy. A decisão do que é raspado está em `docs/adrs/ADR-010-monitoramento-no-compose.md`, `docs/adrs/ADR-012-metricas-do-processor.md`, `docs/adrs/ADR-013-aplica-status-no-registro.md`, `docs/adrs/ADR-014-listagem-no-redis.md`, `docs/adrs/ADR-015-download-do-zip.md` e `docs/adrs/ADR-016-email-de-erro.md`.
