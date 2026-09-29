# Video.Tests

Testes da Video API. Projeto `tst/Video/Tests`.

`VideoHostTests` sobe o host sem PostgreSQL, MinIO nem RabbitMQ e espera 404 em `GET /`. O status é coberto no caso de uso, com repositório de mentira. O consumidor da fila `status` não abre conexão nesse host.

O envio é coberto no caso de uso, com storage, fila e repositório falsos, e no endpoint, com as implementações em memória. O repositório EF é exercido com SQLite em memória. O leitor HMAC aceita um token no formato da Auth e recusa token expirado ou sem claim.

A cobertura de linhas da Video permanece em pelo menos 80%.

O corte está em [`planning/planning.md`](../../../planning/planning.md).
