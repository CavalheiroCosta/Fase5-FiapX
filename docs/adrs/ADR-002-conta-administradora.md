# ADR-002 — Conta administradora

## Status

Aceita.

## Contexto

O planning deixava em aberto se o usuário se cadastra sozinho ou se o acesso é criado para ele. Também registrava que não havia papéis.

O corte da Auth precisa de alguém que grave os usuários antes do login. O token, quando existir, carrega o usuário e o e-mail. O formato do token e a forma de guardar a senha continuam sem escolha.

## Decisão

O serviço nasce com uma conta administradora.

- Login: `Adm`
- Senha: `Adm`
- Nome: `Administrador`
- E-mail: `adm@adm.com`

Essa conta cadastra os demais usuários. Não há autocadastro. O cadastro pede usuário (login), senha, nome e e-mail. Quem nasce por esse cadastro não é administrador.

A conta `Adm` é gravada na subida quando ainda não existe. O cadastro não a remove e não troca o login dela.

A ordem do corte:

1. Cadastro sem token na requisição.
2. Emissão do token no login.
3. Leitura do token, recuperando usuário e e-mail.
4. Cadastro passa a exigir o token da conta `Adm`, reconhecida pelo usuário carregado no token.

Fora desta API, autorização continua sendo acesso válido ou não. O papel de administrador vale só para o cadastro.

Encerrar ou revogar um acesso continua em aberto. Enquanto isso, basta autenticar de novo.

## Consequências

A senha `Adm` é a senha inicial desta conta no ambiente do trabalho. Não é segredo de produção.

Nenhuma resposta de cadastro ou de login devolve a senha. O hash usa o PasswordHasher do .NET, em `docs/adrs/ADR-004-senha-com-passwordhasher.md`. O registro é um Guid, em `docs/adrs/ADR-005-identificador-guid.md`.
