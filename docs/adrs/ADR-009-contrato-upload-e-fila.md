# ADR-009 — Contrato do envio e da fila

## Status

Aceita.

## Contexto

A Feature 1 recebe o vídeo, grava o binário no storage, registra o vídeo como aguardando e publica a referência. O binário não vai para o Postgres nem para a fila. Quem consome a fila ainda não existe.

Faltava fechar o HTTP, o caminho no storage, o corpo da mensagem e o que acontece quando um passo falha.

## Decisão

`POST /videos` recebe `multipart/form-data` no campo `arquivo`, com `Authorization: Bearer`. Arquivo ausente ou vazio responde 400. Token ausente, inválido ou expirado responde 401. Nada é gravado nesses casos.

O sucesso responde 201 com o identificador, o status `aguardando_processamento` e o caminho.

No local, o bucket é `videos`. A chave do objeto é `{id}/{nomeDoArquivo}`. O caminho publicado é `videos/{id}/{nomeDoArquivo}`.

A fila durável se chama `processamento`. A mensagem é JSON com `id` e `caminho`. Só isso.

A ordem é storage, registro, fila. Se o storage falha, não há linha no banco. Se o registro falha depois do arquivo, a API apaga o objeto e devolve erro. Se a fila falha, o arquivo e o registro permanecem, com status aguardando. Não há endpoint para enfileirar de novo neste corte.

O limite do corpo no local é 200 MB, lido da configuração.

## Consequências

O console do MinIO mostra o objeto no bucket `videos`. O painel do RabbitMQ mostra a mensagem na fila `processamento`.

O teste unitário usa storage e fila de mentira. Não sobe MinIO nem RabbitMQ.
