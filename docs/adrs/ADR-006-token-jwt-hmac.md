# ADR-006 — Token JWT com HMAC

## Status

Aceita.

## Contexto

O login confere usuário e senha e emite o token. A leitura recupera o usuário e o e-mail. O cadastro passa a exigir o token da conta `Adm`, reconhecida pelo login carregado no token.

O formato estava em aberto: se o token é assinado ou criptografado, onde a regra mora e se um acesso pode ser revogado antes de expirar.

## Decisão

O token é um JWT assinado com HMAC-SHA256. Não é criptografado e não fica gravado no banco. Vale 30 minutos. O conteúdo é o login e o e-mail. Não há papel extra.

A conta `Adm` é reconhecida pelo login `Adm` carregado no token.

Não há revogação. Expirado, o usuário autentica de novo.

A emissão e a leitura ficam num `TokenService` no Domain. A assinatura HMAC fica na Infra, atrás de uma interface, para o Domain não depender da biblioteca JWT e o teste não depender de um segredo de produção.

A chave local de desenvolvimento fica na configuração da Auth, no mesmo espírito da connection string do Compose.

O login é `POST /login`, com login e senha, sem token na requisição. Senha errada e usuário inexistente respondem a mesma recusa e não devolvem token. A resposta de sucesso devolve o token e não devolve a senha. A leitura usa o cabeçalho `Authorization: Bearer`.

Os endpoints de cadastro exigem o token da conta `Adm`. Sem token válido, ou com token de outro usuário, o cadastro não acontece.

## Consequências

O Domain guarda a regra dos 30 minutos, do login e do e-mail. A Infra assina e confere o HMAC.

Token ausente, inválido ou expirado não produz identidade. A leitura não consulta o banco.

A chave em `appsettings.json` serve ao ambiente local. Não é segredo de produção.
