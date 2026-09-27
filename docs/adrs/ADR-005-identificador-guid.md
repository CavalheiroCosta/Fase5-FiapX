# ADR-005 — Identificador Guid

## Status

Aceita.

## Contexto

Consultar, alterar e remover um usuário precisa apontar para um registro só. O login é único e não muda depois de criado. Ele continua sendo a credencial.

Faltava escolher se a API usa o login ou um identificador próprio nessas operações.

## Decisão

O identificador do usuário é um Guid gerado na gravação no PostgreSQL de usuários. Quem chama o cadastro não envia esse Guid.

A criação devolve o Guid. Consultar, alterar e remover usam esse Guid.

O login segue único, imutável e usado só na credencial.

## Consequências

O cliente guarda o Guid devolvido na criação para alterar ou remover aquele usuário.

Trocar o login no futuro não é este corte. O Guid permanece o identificador do registro.
