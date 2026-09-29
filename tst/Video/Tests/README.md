# Video.Tests

Testes da Video API. Projeto `tst/Video/Tests`.

`VideoHostTests` sobe o host sem PostgreSQL, MinIO, RabbitMQ nem Redis e espera 404 em `GET /`. O status é coberto no caso de uso, com repositório de mentira. O consumidor da fila `status` não abre conexão nesse host.

O envio é coberto no caso de uso, com storage, fila, repositório e lista falsos, e no endpoint, com as implementações em memória. A listagem cobre o hit no Redis, o miss que preenche a partir do Postgres e a recusa sem token. O download cobre o ZIP de um vídeo `concluido` do próprio login e a recusa sem token, de outro login, inexistente ou ainda não concluído. O e-mail cobre o aviso só depois do `erro` gravado e a falha do envio que mantém esse status. O repositório EF é exercido com SQLite em memória. O leitor HMAC aceita um token no formato da Auth e recusa token expirado ou sem claim.

A cobertura de linhas da Video permanece em pelo menos 80%.

O corte está em [`planning/planning.md`](../../../planning/planning.md).
