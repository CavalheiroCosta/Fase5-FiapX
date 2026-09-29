# Processor.Infra

Fila, storage, Redis e ffmpeg do processor. Projeto `src/Processor/Infra`.

Consome a fila durável `processamento` com prefetch 1 e publica na fila durável `status`. As duas são declaradas na subida. Se a conexão cai, o consumidor tenta de novo. Erro de processamento confirma a mensagem. O cancelamento do host recoloca a mensagem e a marca sai antes.

O vídeo e o ZIP ficam no MinIO pela API do S3. A marca `marca:{id}` fica no Redis com `SET NX`. O ffmpeg extrai um frame por segundo e o ZIP é montado em memória. Na subida, o bucket `videos` é garantido.

O teste unitário não sobe Redis, MinIO, RabbitMQ nem o binário do ffmpeg. O host de teste usa marca, storage, fila e quebra em memória.

O Redis, o MinIO e o RabbitMQ sobem com o Compose na raiz. As portas estão no `compose.yaml` e no README da raiz.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O contrato está em [`docs/adrs/ADR-011-contrato-fila-status.md`](../../../docs/adrs/ADR-011-contrato-fila-status.md).
