# Auth.Domain

Regras do usuário e do token da Auth API. Projeto `src/Auth/Domain`.

Guarda o usuário, a credencial, o acesso e a conta administradora. O `TokenService` emite o JWT com login, e-mail e validade de 30 minutos, e lê esse token de volta. Token ausente, inválido ou expirado não produz identidade. A assinatura entra por `IAssinaturaToken`. O repositório entra por `IUsuarioRepository`. Esta camada não fala com PostgreSQL, não referencia a biblioteca JWT e não fala com HTTP.

O corte está em [`planning/auth.md`](../../../planning/auth.md).
