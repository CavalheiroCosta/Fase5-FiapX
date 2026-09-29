# Video.Application

Caso de uso do envio de vídeo. Projeto `src/Video/Application`.

O `EnviarVideoUseCase` exige uma identidade lida do token. Grava o arquivo no storage, registra o vídeo como aguardando e só então publica o identificador e o caminho. Se a fila falha, o registro permanece aguardando.

Storage, fila, repositório e leitura do token entram por interface. A resposta não leva o binário.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O contrato está em [`docs/adrs/ADR-009-contrato-upload-e-fila.md`](../../../docs/adrs/ADR-009-contrato-upload-e-fila.md).
