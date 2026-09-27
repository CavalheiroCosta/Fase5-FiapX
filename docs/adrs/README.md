# ADRs

Decisões de arquitetura deste monorepo. O desenho do sistema continua em `planning/planning.md`. O corte da Auth está em `planning/auth.md`.

| ADR | Tema |
| --- | --- |
| [ADR-001](./ADR-001-camadas-auth.md) | Auth em Api, Application, Domain e Infra |
| [ADR-002](./ADR-002-conta-administradora.md) | Conta `Adm` cadastra os usuários |
| [ADR-003](./ADR-003-postgres-usuarios-no-compose.md) | PostgreSQL de usuários no Compose, com o cadastro |
| [ADR-004](./ADR-004-senha-com-passwordhasher.md) | Senha guardada com o PasswordHasher do .NET |
| [ADR-005](./ADR-005-identificador-guid.md) | Identificador do usuário é um Guid |

ADR nova entra nesta tabela no mesmo pull request em que a decisão é escrita.
