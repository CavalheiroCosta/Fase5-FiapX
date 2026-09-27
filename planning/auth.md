# Auth API

Guia do corte da Auth. O desenho do sistema está em `planning/planning.md`. A ordem do trabalho está em `planning/Fiapx-todo.md`. As decisões deste serviço estão em `docs/adrs`.

A Auth API cadastra os usuários, autentica e emite o token. O resto do sistema só atende quem esta API reconhece. Ela não processa vídeo, não guarda arquivo e não acompanha status de processamento.

## O que já existe

- Solution .NET 10 `Fase5-FiapX.slnx`.
- Projeto `src/Auth/Api` (`Auth.Api`). O host sobe e ainda não tem cadastro nem login.
- Testes em `tst/Auth/Tests`. `AuthHostTests` sobe o host e espera `404` em `GET /`.
- Imagem em `src/Auth/Api/Dockerfile`. O CI compila a tag local `fase5-auth:ci` e não publica.
- Workflow `.github/workflows/ci.yml`: restore, build Release, testes OpenCover da Auth, falha abaixo de 80% de linhas, SonarCloud com quality gate e build da imagem.
- O script `.github/scripts/check-auth-coverage.ps1` lê o OpenCover e mede os módulos da Auth (`Auth.Api` e o que começa com `Auth.`). O Sonar exclui `Program.cs` da cobertura. A trava de 80% do workflow não depende do Sonar.
- Não há Docker Compose, PostgreSQL, fila, MinIO, Redis nem monitoria neste repositório.
- Ainda não existem os projetos Application, Domain e Infra.

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

A primeira entrega desses endpoints não exige token.

## Ordem

1. Cadastro, sem token na requisição. A persistência fica atrás de uma interface e grava no PostgreSQL de usuários. O Docker Compose deste passo sobe só esse banco. A decisão está em `docs/adrs/ADR-003-postgres-usuarios-no-compose.md`.
2. Login, que confere usuário e senha e emite o token.
3. Leitura do token, que recupera o usuário e o e-mail.
4. Os endpoints de cadastro passam a exigir o token da conta `Adm`.

O formato do token continua em aberto, inclusive se ele é assinado ou criptografado. O que já está definido é o conteúdo: usuário e e-mail. No passo 4, a conta `Adm` é reconhecida pelo usuário carregado no token.

## Conceitos

- **Usuário:** pessoa que usa o sistema. É a dona dos vídeos que enviar. Tem Guid, login, nome e e-mail. O Guid identifica o registro. O e-mail é usado pela Video API quando um processamento falha.
- **Credencial:** login e senha usados para provar a identidade.
- **Acesso:** permissão daquele usuário para entrar e usar o sistema. Neste corte, o usuário cadastrado tem acesso. A conta `Adm` também tem.
- **Administrador:** a conta `Adm`. Só ela cadastra usuários.
- **Identidade autenticada:** confirmação de quem está agindo depois de um acesso válido. Os outros contextos usam essa identidade para saber de quem é cada vídeo.
- **Token:** o que o usuário envia em cada requisição depois do login. Carrega a identidade autenticada, inclusive o usuário e o e-mail.

Fora da Auth, autorização continua sendo acesso válido ou não. O papel de administrador vale para o cadastro, dentro desta API.

## Login

O login recebe a credencial e devolve o token. Não exige token na requisição. Entra no passo 2, depois do cadastro.

Senha errada ou usuário inexistente não gera token. A senha não volta na resposta.

Envio, listagem e download, na Video API, exigem esse token. Esta API não implementa esses endpoints.

## Persistência

O PostgreSQL de usuários é desta API. A Video API não lê nem escreve nele. Ela recebe só a identidade autenticada.

A base guarda o Guid, o login, o nome, o e-mail, a credencial e o acesso, inclusive a conta `Adm`. O Guid nasce na gravação. Só a Auth API escreve nela.

O Docker Compose do cadastro sobe o PostgreSQL de usuários. A Auth API grava nele. O teste unitário não sobe esse banco: a persistência fica atrás de uma interface, para a prova do cadastro, do login e da recusa não depender do container.

A senha é guardada com o PasswordHasher do .NET (PBKDF2). A decisão está em `docs/adrs/ADR-004-senha-com-passwordhasher.md`. Nenhuma resposta devolve a senha nem o hash.

## Camadas

O serviço da Auth usa as quatro camadas da Fase 4. A decisão está em `docs/adrs/ADR-001-camadas-auth.md`.

- **Api** — host HTTP, em `src/Auth/Api`.
- **Application** — casos de uso do cadastro e, nos passos seguintes, do login e da leitura do token.
- **Domain** — usuário, credencial, acesso e conta administradora.
- **Infra** — persistência no PostgreSQL de usuários, atrás da interface que o teste usa.

A imagem continua publicando `Auth.Api`. Application, Domain e Infra entram como projetos referenciados por ela.

## Documentação

- `README.md` na raiz descreve o monorepo.
- Cada projeto tem o próprio `README.md`. Projeto novo nasce com esse arquivo.
- As ADRs ficam em `docs/adrs`. O índice está em `docs/adrs/README.md`.

## Fora deste corte

Não implementar:

- Autocadastro.
- Encerrar ou revogar o acesso. Enquanto isso estiver aberto, basta autenticar de novo.
- Exigir token no cadastro antes do passo 4.
- Envio, listagem, download e status de vídeo.
- Processamento dos frames e geração do ZIP.
- E-mail de erro. Quem avisa é a Video API.
- Banco de vídeos, fila, MinIO, Redis, monitoria, publicação da imagem e deploy.

## Testes

No passo do cadastro, cobrir criar, consultar, listar, alterar e remover, e as recusas: login repetido, e-mail repetido e remover a conta `Adm`.

No passo do login, cobrir a emissão do token e a recusa quando a senha está errada ou o usuário não existe.

A cobertura de linhas da Auth permanece em pelo menos 80%. O CI já barra o pull request abaixo disso.
