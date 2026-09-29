# Processor.Tests

Testes da Video Processor API. Projeto `tst/Processor/Tests`.

`ProcessorHostTests` sobe o host sem Redis, MinIO, RabbitMQ nem ffmpeg e espera 404 em `GET /`. `GET /metrics` responde sem token.

O processamento é coberto no caso de uso, com marca, storage, fila e quebra falsos. A entrega confirma a mensagem no erro e no sucesso e recoloca no cancelamento. O ZIP do ffmpeg é exercido com um executor falso, que grava um JPEG no lugar do processo.

A cobertura de linhas do processor permanece em pelo menos 80%.

O corte está em [`planning/planning.md`](../../../planning/planning.md).
