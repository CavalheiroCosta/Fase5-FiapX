# ADR-001 — Camadas da Auth

## Status

Aceita.

## Contexto

A Auth API hoje é só o host em `src/Auth/Api`. O corte seguinte persiste usuário, credencial e acesso, cadastra usuários e, depois, emite e lê o token.

Na Fase 4, Billing e Execution separam o serviço em quatro projetos: Api, Application, Domain e Infra. A Api hospeda o HTTP. A Application concentra os casos de uso. O Domain guarda as regras. A Infra fala com o que está fora do processo, atrás de interface, para o teste unitário não depender de banco, fila ou serviço externo.

## Decisão

A Auth usa as mesmas quatro camadas.

| Camada | Projeto | Responsabilidade |
| --- | --- | --- |
| Api | `src/Auth/Api` | Host HTTP. Recebe a requisição e devolve a resposta. |
| Application | `src/Auth/Application` | Casos de uso do cadastro e do login. |
| Domain | `src/Auth/Domain` | Usuário, credencial, acesso, conta administradora e a regra do token. |
| Infra | `src/Auth/Infra` | PostgreSQL de usuários e a assinatura HMAC do token, atrás de interface. |

A imagem Docker continua publicando `Auth.Api`. Os outros três projetos entram por referência.

Cada projeto tem o próprio `README.md`. O `README.md` da raiz descreve o monorepo. As ADRs ficam em `docs/adrs`.

Video API e Video Processor API seguem esta divisão quando forem criadas.

## Consequências

O teste do cadastro e do login usa a interface de persistência. Não sobe PostgreSQL. A assinatura do token está em `docs/adrs/ADR-006-token-jwt-hmac.md`.

O workflow de cobertura já mede módulos `Auth.*`. Os projetos novos entram nessa medida sem mudar a trava de 80%.
