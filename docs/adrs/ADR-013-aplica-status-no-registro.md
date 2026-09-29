# ADR-013 — A Video API aplica o status no registro

## Status

Aceita.

## Contexto

A Feature 3 publica na fila `status`. O Postgres de vídeos continuava em `aguardando_processamento`. A listagem, o download e o e-mail ainda não entram. Faltava decidir quem grava o status e o que acontece quando a mesma mensagem chega de novo.

## Decisão

A Video API consome a fila durável `status`. O processor não escreve no Postgres.

O corpo continua o do ADR-011: `id`, `momento` (`comecou`, `sucesso` ou `erro`) e, no `sucesso`, `caminho`.

- `comecou` passa `aguardando_processamento` para `em_processamento`. Não grava ZIP.
- `sucesso` passa para `concluido` e grava `caminho_zip`, mesmo se o `comecou` não tiver sido aplicado. Sem `caminho`, a mensagem é confirmada e o vídeo não muda.
- `erro` passa para `erro`, mesmo sem o `comecou`.
- Vídeo já `concluido` ou `erro` não reabre. A mensagem é confirmada.
- Corpo ilegível ou vídeo inexistente é confirmado e não altera outro registro.
- Falha ao gravar confirma a mensagem. Ela não volta para `status`. O cancelamento do host recoloca.

O contador `fiapx_video_status` sai no `GET /metrics` que já existe, com o rótulo `momento`. No Prometheus ele aparece como `fiapx_video_status_total`. O dashboard `FIAP X` ganha o painel desse contador. Não nasce outro stack.

## Consequências

No banco `fiapx_videos`, o status muda. No sucesso, `caminho_zip` deixa de ser nulo. A fila `status` esvazia.

O teste unitário usa repositório e fila de mentira. Não sobe PostgreSQL, MinIO, RabbitMQ, Redis, Prometheus nem Grafana.

Listagem no Redis, download e e-mail continuam fora. O e-mail só sai depois que o status já está `erro`.
