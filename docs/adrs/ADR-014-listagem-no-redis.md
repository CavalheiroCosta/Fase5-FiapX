# ADR-014 — A listagem sai do Redis

## Status

Aceita.

## Contexto

O Postgres de vídeos já guarda o status. A listagem que o usuário vê ainda lia o registro direto, e o endpoint não existia. O Redis do Compose já guarda a marca `marca:{id}` do processor. Faltava decidir a chave da lista, o que acontece quando ela não está lá e o que acontece se o Redis falha no envio ou no status.

## Decisão

A Video API é dona da listagem. O processor não escreve essa chave. O Postgres continua o registro.

`GET /videos` exige o token. Sem token válido, responde 401 e não devolve lista. Com token, devolve só os vídeos daquele login: `id`, `status` e, quando `concluido`, `caminhoZip`.

A chave é `lista:{login}`. O valor é o JSON da lista inteira. Chave ausente é cache miss. Lista vazia gravada é hit e não volta ao Postgres.

- No miss, a Video API lê o Postgres de vídeos daquele login e grava a lista de novo.
- O envio e a aplicação de status atualizam a entrada só se a chave já existir. Chave ausente não recebe lista parcial: o próximo `GET` reconstrói pelo Postgres.
- Falha ao ler ou gravar o Redis no `GET` não esconde a lista do Postgres. A resposta sai e o contador marca `postgres`.
- Falha do Redis no envio não apaga o arquivo, não desfaz o registro e não impede a fila.
- Falha do Redis ao aplicar status confirma a mensagem. Ela não volta para `status`. O cancelamento do host recoloca.

O contador `fiapx_video_listagem` sai no `GET /metrics` que já existe, com o rótulo `origem` (`redis` ou `postgres`). No Prometheus ele aparece como `fiapx_video_listagem_total`. O dashboard `FIAP X` ganha o painel desse contador. Não nasce outro stack.

## Consequências

`GET /videos` com o token do dono mostra a lista. Outro login não vê esses vídeos. No Redis, a chave `lista:{login}` aparece depois da primeira listagem.

O teste unitário usa lista e repositório de mentira. Não sobe PostgreSQL, MinIO, RabbitMQ, Redis, Prometheus nem Grafana.

Download e e-mail continuam fora. O download usa a referência do ZIP que esta lista já mostra quando o status é `concluido`.
