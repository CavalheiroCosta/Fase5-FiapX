# ADR-004 — Senha com o PasswordHasher do .NET

## Status

Aceita.

## Contexto

O cadastro grava a credencial no PostgreSQL de usuários. Nenhuma resposta devolve a senha. A forma de guardá-la estava em aberto.

O login, no passo seguinte, confere a senha digitada com o valor gravado. Senha errada não emite token.

## Decisão

A senha é guardada com o PasswordHasher do .NET, que usa PBKDF2. O valor no banco é o hash produzido por ele, com sal e iterações. A senha em texto não é persistida.

A aplicação pede o hash na gravação e a conferência no login. A Infra usa o PasswordHasher. O teste unitário não sobe PostgreSQL.

## Consequências

Duas contas com a mesma senha não ficam com o mesmo valor gravado.

O cadastro e o login não devolvem a senha nem o hash.
