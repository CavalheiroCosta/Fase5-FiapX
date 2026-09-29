# ADRs

Decisões de arquitetura deste monorepo. O desenho do sistema continua em `planning/planning.md`. O corte da Auth está em `planning/auth.md`.

| ADR | Tema |
| --- | --- |
| [ADR-001](./ADR-001-camadas-auth.md) | Auth em Api, Application, Domain e Infra |
| [ADR-002](./ADR-002-conta-administradora.md) | Conta `Adm` cadastra os usuários |
| [ADR-003](./ADR-003-postgres-usuarios-no-compose.md) | PostgreSQL de usuários no Compose, com o cadastro |
| [ADR-004](./ADR-004-senha-com-passwordhasher.md) | Senha guardada com o PasswordHasher do .NET |
| [ADR-005](./ADR-005-identificador-guid.md) | Identificador do usuário é um Guid |
| [ADR-006](./ADR-006-token-jwt-hmac.md) | Token JWT assinado com HMAC |
| [ADR-007](./ADR-007-postgres-videos-no-compose.md) | PostgreSQL de vídeos no Compose |
| [ADR-008](./ADR-008-video-le-jwt-local.md) | A Video API lê o JWT sozinha |
| [ADR-009](./ADR-009-contrato-upload-e-fila.md) | Contrato do envio e da fila de processamento |
| [ADR-010](./ADR-010-monitoramento-no-compose.md) | Prometheus e Grafana no Compose |
| [ADR-011](./ADR-011-contrato-fila-status.md) | Contrato da fila de status, da marca e do ZIP |
| [ADR-012](./ADR-012-metricas-do-processor.md) | Métricas do processor no Compose |
| [ADR-013](./ADR-013-aplica-status-no-registro.md) | A Video API aplica o status no registro |
| [ADR-014](./ADR-014-listagem-no-redis.md) | A listagem sai do Redis |
| [ADR-015](./ADR-015-download-do-zip.md) | O download entrega o ZIP do storage |
| [ADR-016](./ADR-016-email-de-erro.md) | O e-mail de erro sai depois do status |
| [ADR-017](./ADR-017-dois-processors-no-compose.md) | Dois processors na mesma fila |

ADR nova entra nesta tabela no mesmo pull request em que a decisão é escrita.
