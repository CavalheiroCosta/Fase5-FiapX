# ADR-015 — O download entrega o ZIP do storage

## Status

Aceita.

## Contexto

O Postgres já guarda `caminho_zip` quando o vídeo está `concluido`. A listagem mostra esse caminho. O arquivo ainda não saía por HTTP. Faltava decidir de onde sai a referência, o que a resposta carrega e o que acontece quando o vídeo não pode ser baixado.

## Decisão

`GET /videos/{id}/download` exige o token. A referência sai do Postgres, não da lista no Redis. O binário sai do storage por essa referência.

- Sem token válido, responde 401 e o arquivo não sai.
- Vídeo de outro login, vídeo inexistente ou status diferente de `concluido` respondem 404 com o mesmo corpo: `Vídeo não encontrado.` A resposta não distingue os três casos.
- Com `concluido` e `caminho_zip`, a resposta é o ZIP, `Content-Type: application/zip`, nome `{id}.zip`.
- Falha ao ler o storage responde 503. O vídeo original não é entregue.

O contador `fiapx_video_download` sai no `GET /metrics` que já existe, com o rótulo `resultado` (`entregue` ou `recusado`). No Prometheus ele aparece como `fiapx_video_download_total`. O dashboard `FIAP X` ganha o painel desse contador. Não nasce outro stack nem outro serviço no Compose.

## Consequências

O dono baixa o ZIP depois que o status está `concluido`. Outro login recebe o mesmo 404 de um vídeo que não existe.

O teste unitário usa repositório e storage de mentira. Não sobe PostgreSQL, MinIO, RabbitMQ, Redis, Prometheus nem Grafana.

O vídeo original e o e-mail de erro continuam fora.
