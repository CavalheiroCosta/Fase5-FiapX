# Auth.Domain

Regras do usuário da Auth API. Projeto `src/Auth/Domain`.

Guarda o usuário, a credencial, o acesso e a conta administradora. O repositório entra por `IUsuarioRepository`. Esta camada não fala com PostgreSQL nem com HTTP.

O corte está em [`planning/auth.md`](../../../planning/auth.md).
