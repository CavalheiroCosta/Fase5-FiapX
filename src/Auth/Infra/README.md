# Auth.Infra

Persistência da Auth API. Projeto `src/Auth/Infra`.

Grava o usuário no PostgreSQL com EF Core. O Guid nasce na inserção. A senha fica no hash do PasswordHasher do .NET. Na subida, a conta `Adm` é gravada se ainda não existir.

O teste unitário não sobe o PostgreSQL. O host de teste usa o repositório em memória. O repositório EF é exercido com SQLite em memória.

O banco local sobe com o Compose na raiz do repositório. A connection string está em `src/Auth/Api/appsettings.json`.

O corte está em [`planning/auth.md`](../../../planning/auth.md). A decisão do Compose está em [`docs/adrs/ADR-003-postgres-usuarios-no-compose.md`](../../../docs/adrs/ADR-003-postgres-usuarios-no-compose.md).
