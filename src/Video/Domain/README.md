# Video.Domain

Regras do vídeo enviado. Projeto `src/Video/Domain`.

Guarda o vídeo, o status e o caminho no storage. O Guid nasce aqui quando o envio é aceito. O dono é o login e o e-mail, não um Guid lido na Auth. `comecou`, `sucesso` e `erro` mudam o status aqui. Vídeo `concluido` ou `erro` não reabre. O repositório, o storage e a fila entram por interface. A leitura do token entra por `ILeitorToken`. Esta camada não fala com PostgreSQL, MinIO, RabbitMQ nem HTTP.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O banco está em [`docs/adrs/ADR-007-postgres-videos-no-compose.md`](../../../docs/adrs/ADR-007-postgres-videos-no-compose.md). O token está em [`docs/adrs/ADR-008-video-le-jwt-local.md`](../../../docs/adrs/ADR-008-video-le-jwt-local.md).
