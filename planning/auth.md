# Auth API

Guia do corte da Auth. O desenho do sistema está em `planning/planning.md`. A ordem do trabalho está em `planning/Fiapx-todo.md`. As decisões deste serviço estão em `docs/adrs`.

A Auth API cadastra os usuários, autentica e emite o token. O resto do sistema só atende quem esta API reconhece. Ela não processa vídeo, não guarda arquivo e não acompanha status de processamento.

## O que já existe

- Solution .NET 10 `Fase5-FiapX.slnx`, com Api, Application, Domain e Infra.
- Projeto `src/Auth/Api` (`Auth.Api`). O login emite o token. O cadastro cria, consulta, lista, altera e remove usuários e exige o token da conta `Adm`.
- Testes em `tst/Auth/Tests`. Cobrem o cadastro, o login, a recusa, a leitura do token e o host. O host de teste não sobe PostgreSQL.
- Imagem em `src/Auth/Api/Dockerfile`. O CI compila a tag local `fase5-auth:ci` e não publica.
- Workflow `.github/workflows/ci.yml`: restore, build Release, testes OpenCover da Auth, falha abaixo de 80% de linhas, SonarCloud com quality gate e build da imagem.
- O script `.github/scripts/check-auth-coverage.ps1` lê o OpenCover e mede os módulos da Auth (`Auth.Api` e o que começa com `Auth.`). O Sonar exclui `Program.cs` da cobertura. A trava de 80% do workflow não depende do Sonar.
- `compose.yaml` sobe o PostgreSQL de usuários. Não há fila, MinIO, Redis nem monitoria neste repositório.

## Conta administradora

O serviço nasce com uma conta administradora. A decisão está em `docs/adrs/ADR-002-conta-administradora.md`.

- Login: `Adm`
- Senha: `Adm`
- Nome: `Administrador`
- E-mail: `adm@adm.com`

Essa conta cadastra os demais usuários. Não há autocadastro. Não há outro papel neste corte: ou é a conta `Adm`, ou é um usuário criado por ela.

A conta é gravada na subida quando ainda não existe. O cadastro não a remove e não troca o login dela.

## Cadastro

O administrador informa, para cada usuário:

- usuário (o login)
- senha
- nome
- e-mail

O login identifica a pessoa na credencial. O nome é o nome da pessoa. O e-mail segue com a identidade e será usado pela Video API quando um processamento falha. O identificador do registro é um Guid gerado na gravação. A decisão está em `docs/adrs/ADR-005-identificador-guid.md`.

O cadastro faz o seguinte:

- criar o usuário
- consultar um usuário
- listar os usuários
- alterar nome, e-mail e senha
- remover o usuário

A criação não recebe o Guid. A resposta devolve o Guid gerado. Consultar, alterar e remover usam esse Guid.

O login não muda depois de criado. A senha não volta na consulta, na lista nem na alteração. Login repetido ou e-mail repetido não grava outro usuário. Remover a conta `Adm` não acontece.

Esses endpoints exigem o token da conta `Adm` no cabeçalho `Authorization: Bearer`. Sem token válido, ou com token de outro usuário, o cadastro não acontece. `POST /login` continua sem token.

## Ordem

1. Cadastro, sem token na requisição. A persistência fica atrás de uma interface e grava no PostgreSQL de usuários. O Docker Compose deste passo sobe só esse banco. A decisão está em `docs/adrs/ADR-003-postgres-usuarios-no-compose.md`. Feito.
2. Login, que confere usuário e senha e emite o token. Feito.
3. Leitura do token, que recupera o usuário e o e-mail. Feito.
4. Os endpoints de cadastro exigem o token da conta `Adm`. Feito.

O token é um JWT assinado com HMAC. Vale 30 minutos. O conteúdo é o login e o e-mail. Não é criptografado e não fica gravado no banco. Não há revogação: expirado, o usuário autentica de novo. A conta `Adm` é reconhecida pelo login `Adm` carregado no token. A decisão está em `docs/adrs/ADR-006-token-jwt-hmac.md`.

## Conceitos

- **Usuário:** pessoa que usa o sistema. É a dona dos vídeos que enviar. Tem Guid, login, nome e e-mail. O Guid identifica o registro. O e-mail é usado pela Video API quando um processamento falha.
- **Credencial:** login e senha usados para provar a identidade.
- **Acesso:** permissão daquele usuário para entrar e usar o sistema. Neste corte, o usuário cadastrado tem acesso. A conta `Adm` também tem.
- **Administrador:** a conta `Adm`. Só ela cadastra usuários.
- **Identidade autenticada:** confirmação de quem está agindo depois de um acesso válido. Os outros contextos usam essa identidade para saber de quem é cada vídeo.
- **Token:** JWT assinado com HMAC que o usuário envia em cada requisição depois do login. Carrega a identidade autenticada, o login e o e-mail, e vale 30 minutos.

Fora da Auth, autorização continua sendo acesso válido ou não. O papel de administrador vale para o cadastro, dentro desta API.

## Login

O login é `POST /login`. Recebe a credencial e devolve o token. Não exige token na requisição.

Senha errada ou usuário inexistente respondem a mesma recusa e não geram token. A senha não volta na resposta. A leitura usa `Authorization: Bearer`. Token ausente, inválido ou expirado não produz identidade.

Envio, listagem e download, na Video API, exigem esse token. Esta API não implementa esses endpoints.

## Persistência

O PostgreSQL de usuários é desta API. A Video API não lê nem escreve nele. Ela recebe só a identidade autenticada.

A base guarda o Guid, o login, o nome, o e-mail, a credencial e o acesso, inclusive a conta `Adm`. O Guid nasce na gravação. Só a Auth API escreve nela.

O Docker Compose do cadastro sobe o PostgreSQL de usuários. A Auth API grava nele. O teste unitário não sobe esse banco: a persistência fica atrás de uma interface, para a prova do cadastro, do login e da recusa não depender do container.

A senha é guardada com o PasswordHasher do .NET (PBKDF2). A decisão está em `docs/adrs/ADR-004-senha-com-passwordhasher.md`. Nenhuma resposta devolve a senha nem o hash.

## Camadas

O serviço da Auth usa as quatro camadas da Fase 4. A decisão está em `docs/adrs/ADR-001-camadas-auth.md`.

- **Api** — host HTTP, em `src/Auth/Api`.
- **Application** — casos de uso do cadastro e do login. O login confere a senha e pede o token.
- **Domain** — usuário, credencial, acesso, conta administradora e o `TokenService`, que emite e lê o token.
- **Infra** — persistência no PostgreSQL de usuários e a assinatura HMAC, atrás das interfaces que o teste usa.

A imagem continua publicando `Auth.Api`. Application, Domain e Infra entram como projetos referenciados por ela.

## Documentação

- `README.md` na raiz descreve o monorepo.
- Cada projeto tem o próprio `README.md`. Projeto novo nasce com esse arquivo.
- As ADRs ficam em `docs/adrs`. O índice está em `docs/adrs/README.md`.

## Fora deste corte

Não implementar:

- Autocadastro.
- Revogação de token. Expirado, basta autenticar de novo.
- Envio, listagem, download e status de vídeo.
- Processamento dos frames e geração do ZIP.
- E-mail de erro. Quem avisa é a Video API.
- Banco de vídeos, fila, MinIO, Redis, monitoria, publicação da imagem e deploy.

## Testes

No passo do cadastro, cobrir criar, consultar, listar, alterar e remover, e as recusas: login repetido, e-mail repetido e remover a conta `Adm`.

No login, cobrir a emissão do token, a recusa quando a senha está errada ou o usuário não existe, a leitura e a exigência do token da conta `Adm` no cadastro.

A cobertura de linhas da Auth permanece em pelo menos 80%. O CI já barra o pull request abaixo disso.
