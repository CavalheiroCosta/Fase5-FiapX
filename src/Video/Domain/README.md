# Video.Domain

Regras do vídeo enviado. Projeto `src/Video/Domain`.

Guarda o vídeo, o status e o caminho no storage. O Guid nasce aqui quando o envio é aceito. O dono é o login e o e-mail, não um Guid lido na Auth. `comecou`, `sucesso` e `erro` mudam o status aqui. Vídeo `concluido` ou `erro` não reabre. O item da listagem leva o caminho do ZIP só quando o status é `concluido`. O download usa essa referência no Postgres e abre o objeto no storage. O e-mail de erro entra por `IEnviadorEmail` e sai depois que o status já está `erro`. O repositório, o storage, a fila e a lista entram por interface. A leitura do token entra por `ILeitorToken`. Esta camada não fala com PostgreSQL, MinIO, RabbitMQ, Redis nem HTTP.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O banco está em [`docs/adrs/ADR-007-postgres-videos-no-compose.md`](../../../docs/adrs/ADR-007-postgres-videos-no-compose.md). O token está em [`docs/adrs/ADR-008-video-le-jwt-local.md`](../../../docs/adrs/ADR-008-video-le-jwt-local.md). A listagem está em [`docs/adrs/ADR-014-listagem-no-redis.md`](../../../docs/adrs/ADR-014-listagem-no-redis.md). O download está em [`docs/adrs/ADR-015-download-do-zip.md`](../../../docs/adrs/ADR-015-download-do-zip.md). O e-mail está em [`docs/adrs/ADR-016-email-de-erro.md`](../../../docs/adrs/ADR-016-email-de-erro.md).
