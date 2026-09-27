# Auth.Api

Host HTTP da Auth API. Projeto `src/Auth/Api`, alvo `net10.0`.

Hoje o host sobe e `GET /` responde 404. Ainda não há cadastro nem login.

Este projeto referencia Application e Infra quando essas camadas existirem. As regras ficam no Domain. A persistência no PostgreSQL de usuários fica na Infra, atrás de uma interface.

O corte está em [`planning/auth.md`](../../../planning/auth.md). A divisão em camadas está em [`docs/adrs/ADR-001-camadas-auth.md`](../../../docs/adrs/ADR-001-camadas-auth.md).

A imagem em `Dockerfile` publica este projeto. O CI gera a tag local `fase5-auth:ci` e não publica.
