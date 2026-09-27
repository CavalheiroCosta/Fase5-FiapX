# ADR-003 — PostgreSQL de usuários no Compose

## Status

Aceita.

## Contexto

A Auth persiste login, nome, e-mail, credencial e acesso no PostgreSQL de usuários. O teste unitário fala com essa persistência por uma interface e não sobe banco.

O planejamento prevê um Docker Compose com PostgreSQL, Redis, RabbitMQ, MinIO, Prometheus e Grafana. Nada disso existe no repositório ainda. Fila, storage, Redis e monitoria não fazem parte do cadastro.

## Decisão

O Docker Compose nasce com o cadastro da Auth e sobe só o PostgreSQL de usuários. A Auth API grava nesse banco. A conta `Adm` é criada nele na subida, quando ainda não existe.

O banco de vídeos entra no mesmo Compose quando a Video API existir. Redis, RabbitMQ, MinIO, Prometheus e Grafana continuam fora até os contextos que os usam.

O teste unitário do cadastro não depende do container.

## Consequências

Dá para subir a API e gravar um usuário sem a infraestrutura inteira do processamento de vídeo.

O CI continua sem publicar imagem e sem exigir o Compose para a cobertura. A prova do cadastro usa a interface de persistência.
