# Processor.Domain

Regras do processamento do vídeo. Projeto `src/Processor/Domain`.

Lê a mensagem da fila `processamento` (`id` e `caminho`) e monta o caminho do ZIP. A marca, o storage, a fila de status e a quebra do vídeo entram por interface. Esta camada não fala com Redis, MinIO, RabbitMQ, ffmpeg nem HTTP.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O contrato está em [`docs/adrs/ADR-011-contrato-fila-status.md`](../../../docs/adrs/ADR-011-contrato-fila-status.md).
