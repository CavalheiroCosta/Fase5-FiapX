# Processor.Application

Caso de uso do processamento. Projeto `src/Processor/Application`.

O `ProcessarVideoUseCase` marca o vídeo, publica `comecou`, lê o arquivo, quebra em frames, grava o ZIP e só então publica `sucesso`. Se a quebra ou a gravação falha, publica `erro`. A marca sai no fim, inclusive quando o host é cancelado. Se a marca já existe, o vídeo é ignorado.

Marca, storage, fila e ffmpeg entram por interface. O medidor `FiapX.Processor` conta o momento e quantos vídeos estão em andamento.

O corte está em [`planning/planning.md`](../../../planning/planning.md). O contrato está em [`docs/adrs/ADR-011-contrato-fila-status.md`](../../../docs/adrs/ADR-011-contrato-fila-status.md). As métricas estão em [`docs/adrs/ADR-012-metricas-do-processor.md`](../../../docs/adrs/ADR-012-metricas-do-processor.md).
