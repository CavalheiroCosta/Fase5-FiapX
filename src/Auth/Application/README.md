# Auth.Application

Casos de uso do cadastro da Auth API. Projeto `src/Auth/Application`.

Cria, consulta, lista, altera e remove usuários. A senha entra por `ISenhaHasher`. A persistência entra por `IUsuarioRepository`. A resposta não leva senha nem hash.

O corte está em [`planning/auth.md`](../../../planning/auth.md).
