# Auth.Tests

Testes da Auth API. Projeto `tst/Auth/Tests`.

`AuthHostTests` sobe o host de `Auth.Api` e espera 404 em `GET /`.

O cadastro e o login serão cobertos por testes unitários. A persistência entra por interface, sem PostgreSQL. A cobertura de linhas da Auth permanece em pelo menos 80%.

O que cada passo precisa cobrir está em [`planning/auth.md`](../../../planning/auth.md).
