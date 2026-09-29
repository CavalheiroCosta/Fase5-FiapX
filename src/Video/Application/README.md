# Video.Application

Caso de uso do envio de vídeo. Projeto `src/Video/Application`.

O `EnviarVideoUseCase` exige uma identidade lida do token. Grava o arquivo no storage, registra o vídeo como aguardando e só então publica o identificador e o caminho. Se a fila falha, o registro permanece aguardando. O `AplicarStatusUseCase` lê a fila `status` e grava `em_processamento`, `concluido` ou `erro`. Vídeo já fechado não reabre. O `ListarVideosUseCase` lê a lista do login no Redis e, se a chave não existe, preenche de novo a partir do repositório. O envio e o status atualizam essa entrada quando a chave já existe. O `BaixarZipUseCase` lê o registro no Postgres e abre o ZIP no storage quando o status é `concluido`.

Storage, fila, repositório, lista e leitura do token entram por interface. A resposta do envio não leva o binário. O download leva o ZIP.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O envio está em [`docs/adrs/ADR-009-contrato-upload-e-fila.md`](../../../docs/adrs/ADR-009-contrato-upload-e-fila.md). O status está em [`docs/adrs/ADR-013-aplica-status-no-registro.md`](../../../docs/adrs/ADR-013-aplica-status-no-registro.md). A listagem está em [`docs/adrs/ADR-014-listagem-no-redis.md`](../../../docs/adrs/ADR-014-listagem-no-redis.md). O download está em [`docs/adrs/ADR-015-download-do-zip.md`](../../../docs/adrs/ADR-015-download-do-zip.md).
