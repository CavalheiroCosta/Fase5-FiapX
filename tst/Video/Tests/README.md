# Video.Tests

Testes da Video API. Projeto `tst/Video/Tests`.

`VideoHostTests` sobe o host sem PostgreSQL, MinIO, RabbitMQ nem Redis e espera 404 em `GET /`. O status é coberto no caso de uso, com repositório de mentira. O consumidor da fila `status` não abre conexão nesse host.

O envio é coberto no caso de uso, com storage, fila, repositório e lista falsos, e no endpoint, com as implementações em memória. A listagem cobre o hit no Redis, o miss que preenche a partir do Postgres e a recusa sem token. O repositório EF é exercido com SQLite em memória. O leitor HMAC aceita um token no formato da Auth e recusa token expirado ou sem claim.

A cobertura de linhas da Video permanece em pelo menos 80%.

O corte está em [`planning/planning.md`](../../../planning/planning.md).
