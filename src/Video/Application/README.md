# Video.Application

Caso de uso do envio de vídeo. Projeto `src/Video/Application`.

O `EnviarVideoUseCase` exige uma identidade lida do token. Grava o arquivo no storage, registra o vídeo como aguardando e só então publica o identificador e o caminho. Se a fila falha, o registro permanece aguardando. O `AplicarStatusUseCase` lê a fila `status` e grava `em_processamento`, `concluido` ou `erro`. Vídeo já fechado não reabre.

Storage, fila, repositório e leitura do token entram por interface. A resposta não leva o binário.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O envio está em [`docs/adrs/ADR-009-contrato-upload-e-fila.md`](../../../docs/adrs/ADR-009-contrato-upload-e-fila.md). O status está em [`docs/adrs/ADR-013-aplica-status-no-registro.md`](../../../docs/adrs/ADR-013-aplica-status-no-registro.md).
