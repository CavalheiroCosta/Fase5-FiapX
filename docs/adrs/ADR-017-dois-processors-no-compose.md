# ADR-017 — Dois processors na mesma fila

## Status

Aceita.

## Contexto

A marca `marca:{id}` já impede que o mesmo vídeo seja processado duas vezes. O Compose ainda subia um único consumer da fila `processamento`. Faltava decidir como o ambiente local mostra dois processors sem disputar a porta `5300`.

## Decisão

`docker compose up -d` sobe dois serviços com a mesma imagem e o mesmo ambiente: `processor` e `processor-2`. Os dois consomem a fila `processamento`. Cada um pede uma mensagem por vez.

- `processor` publica `5300:8080`.
- `processor-2` publica `5301:8080`.

Vídeos diferentes seguem em paralelo. Se a mesma mensagem chega nos dois, o segundo vê a marca `marca:{id}` do ADR-011, confirma a mensagem e não publica outro `comecou`.

O job `processor` do Prometheus raspa `processor:8080` e `processor-2:8080`. O painel de resultados continua somando os dois. O painel **Processor — em andamento** soma o gauge e vale 0, 1 ou 2.

## Consequências

O painel do RabbitMQ mostra dois consumidores na fila `processamento`. Os dois alvos `processor` ficam UP no Prometheus.

Não há terceiro processor. Publicação da imagem e deploy continuam fora.
