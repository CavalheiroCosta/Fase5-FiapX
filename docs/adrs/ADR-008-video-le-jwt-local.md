# ADR-008 — A Video API lê o JWT sozinha

## Status

Aceita.

## Contexto

O envio do vídeo exige o token da Auth. Sem token válido, o arquivo não entra. A Video API precisa do login e do e-mail, e não pode reler o usuário no banco da Auth.

Faltava decidir se a Video API chama a Auth, referencia os projetos dela ou confere o JWT no próprio processo.

## Decisão

A Video API não referencia os projetos da Auth e não chama a Auth na requisição. A Infra confere o JWT no processo: HMAC-SHA256, claims `login` e `email`, validade de 30 minutos. A chave é a mesma `Token:Chave` da Auth, vinda da configuração.

Qualquer token válido envia vídeo. A conta `Adm` não é exigida neste endpoint. Token ausente, inválido ou expirado não produz identidade.

A leitura fica atrás de interface, para o teste não depender da biblioteca JWT.

## Consequências

Os dois serviços precisam da mesma chave no ambiente local. Ela está na configuração, no mesmo espírito da connection string. Não é segredo de produção.

Trocar o formato do token exige mudar a Auth e a Video API juntas. A decisão do formato continua em `docs/adrs/ADR-006-token-jwt-hmac.md`.
