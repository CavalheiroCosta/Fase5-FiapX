# ADR-007 — PostgreSQL de vídeos no Compose

## Status

Aceita.

## Contexto

A Feature 1 grava o vídeo no storage e o registro no Postgres de vídeos. A Auth continua dona do Postgres de usuários. A Video API não lê esse banco: o dono do vídeo é o login e o e-mail que vieram no token.

O planejamento prevê um banco por contexto. O banco de usuários já sobe no Compose. Faltava decidir como o banco de vídeos entra e quando o Guid do vídeo nasce, porque o caminho no storage usa esse Guid antes da linha existir.

## Decisão

O Compose ganha o serviço `postgres-videos`, com o banco `fiapx_videos`. No host, a porta é `5433`, para não disputar a `5432` dos usuários. A Video API grava só nesse banco.

O Guid do vídeo nasce na Video API quando o envio é aceito, antes da chave do objeto. Quem chama não envia esse Guid. O Postgres de vídeos guarda o mesmo valor, junto com o login, o e-mail, o status e o caminho. A referência do ZIP fica nula enquanto o vídeo aguarda processamento.

## Consequências

Dá para subir o cadastro da Auth sem o banco de vídeos, e a Video API sem ler `fiapx_usuarios`.

O teste unitário da Video API não sobe PostgreSQL. A persistência fica atrás de interface. O repositório EF é exercido com SQLite em memória.
