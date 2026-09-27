# Auth.Tests

Testes da Auth API. Projeto `tst/Auth/Tests`.

`AuthHostTests` sobe o host sem PostgreSQL e espera 404 em `GET /`.

O cadastro é coberto nos casos de uso, com repositório falso, e nos endpoints, com o repositório em memória. O repositório EF é exercido com SQLite em memória. A conta `Adm` nasce na subida desse host.

O login cobre a emissão do token, a recusa quando a senha está errada ou o usuário não existe, e a leitura. O cadastro nos endpoints exige o token da conta `Adm`. A chave usada no teste não é segredo de produção.

A cobertura de linhas da Auth permanece em pelo menos 80%.

O que cada passo precisa cobrir está em [`planning/auth.md`](../../../planning/auth.md).
