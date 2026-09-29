# Video.Infra

Persistência, storage e fila da Video API. Projeto `src/Video/Infra`.

Grava o vídeo no PostgreSQL de vídeos com EF Core. O Guid já nasceu no envio. Na subida, o schema é criado se ainda não existe, o bucket `videos` é garantido no MinIO e a fila `processamento` é declarada no RabbitMQ. O consumidor lê a fila `status` e atualiza o status. O caminho em memória não abre o RabbitMQ.

O arquivo vai para o MinIO pela API do S3. A fila recebe só o identificador e o caminho, em JSON. A leitura do JWT HMAC fica atrás de `ILeitorToken`. A chave local está em `src/Video/Api/appsettings.json`, a mesma da Auth.

O teste unitário não sobe PostgreSQL, MinIO nem RabbitMQ. O host de teste usa repositório, storage e fila em memória. O repositório EF é exercido com SQLite em memória.

O banco, o MinIO e o RabbitMQ sobem com o Compose na raiz. As portas estão no `compose.yaml` e no README da raiz.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O banco está em [`docs/adrs/ADR-007-postgres-videos-no-compose.md`](../../../docs/adrs/ADR-007-postgres-videos-no-compose.md). O token está em [`docs/adrs/ADR-008-video-le-jwt-local.md`](../../../docs/adrs/ADR-008-video-le-jwt-local.md). O envio está em [`docs/adrs/ADR-009-contrato-upload-e-fila.md`](../../../docs/adrs/ADR-009-contrato-upload-e-fila.md). O status está em [`docs/adrs/ADR-013-aplica-status-no-registro.md`](../../../docs/adrs/ADR-013-aplica-status-no-registro.md).
