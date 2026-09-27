# Auth.Api

Host HTTP da Auth API. Projeto `src/Auth/Api`, alvo `net10.0`.

O cadastro não exige token:

- `POST /usuarios` cria e devolve o Guid
- `GET /usuarios` lista
- `GET /usuarios/{id}` consulta
- `PUT /usuarios/{id}` altera nome, e-mail e senha
- `DELETE /usuarios/{id}` remove

`GET /` responde 404. A senha não volta na resposta. A conta `Adm` não é removida.

Este projeto referencia Application e Infra. As regras ficam no Domain. O PostgreSQL de usuários fica na Infra.

O corte está em [`planning/auth.md`](../../../planning/auth.md). A divisão em camadas está em [`docs/adrs/ADR-001-camadas-auth.md`](../../../docs/adrs/ADR-001-camadas-auth.md).

A imagem em `Dockerfile` publica este projeto. O CI gera a tag local `fase5-auth:ci` e não publica.
