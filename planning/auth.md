# Auth API

Guia do corte da Auth. O desenho do sistema está em `planning/planning.md`. A ordem do trabalho está em `planning/Fiapx-todo.md`.

A Auth API autentica o usuário e emite o token. O resto do sistema só atende quem esta API reconhece. Ela não processa vídeo, não guarda arquivo e não acompanha status de processamento.

## O que já existe

- Solution .NET 10 `Fase5-FiapX.slnx`.
- Projeto `src/Auth/Api` (`Auth.Api`). O host sobe e ainda não tem login.
- Testes em `tst/Auth/Tests`. `AuthHostTests` sobe o host e espera `404` em `GET /`.
- Imagem em `src/Auth/Api/Dockerfile`. O CI compila a tag local `fase5-auth:ci` e não publica.
- Workflow `.github/workflows/ci.yml`: restore, build Release, testes OpenCover da Auth, falha abaixo de 80% de linhas, SonarCloud com quality gate e build da imagem.
- O script `.github/scripts/check-auth-coverage.ps1` lê o OpenCover e mede o módulo `Auth.Api`. O Sonar exclui `Program.cs` da cobertura. A trava de 80% do workflow não depende do Sonar.
- Não há Docker Compose, PostgreSQL, fila, MinIO, Redis nem monitoria neste repositório.

## O que este corte entrega

- Persistir usuário, e-mail, credencial e acesso no PostgreSQL de usuários.
- Login com usuário e senha. Esse endpoint não exige token.
- Senha errada ou usuário inexistente não emite token.
- Token válido carrega o usuário e o e-mail.
- Testes unitários do login e da recusa, dentro da cobertura de 80%.

## Conceitos

- **Usuário:** pessoa que usa o sistema. É a dona dos vídeos que enviar. Tem um e-mail, usado pela Video API quando um processamento falha.
- **Credencial:** usuário e senha usados para provar a identidade.
- **Acesso:** permissão daquele usuário para entrar e usar o sistema.
- **Identidade autenticada:** confirmação de quem está agindo depois de um acesso válido. Os outros contextos usam essa identidade para saber de quem é cada vídeo.
- **Token:** o que o usuário envia em cada requisição depois do login. Carrega a identidade autenticada, inclusive o usuário e o e-mail.

Autorização, neste momento, significa acesso válido ou não. Não há papéis.

## Login

O login recebe a credencial e devolve o token. Não exige token na requisição.

Senha errada ou usuário inexistente não gera token. A senha não volta na resposta.

O formato do token não foi escolhido. O que já está definido é o conteúdo: usuário e e-mail. Envio, listagem e download, na Video API, exigem esse token. Esta API não implementa esses endpoints.

## Persistência

O PostgreSQL de usuários é desta API. A Video API não lê nem escreve nele. Ela recebe só a identidade autenticada.

A base guarda o usuário, o e-mail, a credencial e o acesso. Só a Auth API escreve nela.

O teste unitário do login não sobe PostgreSQL. A persistência fica atrás de uma interface, para a prova do login e da recusa não depender do banco. A forma de guardar a senha não foi escolhida. A resposta do login não devolve a senha.

## Fora deste corte

Não implementar até fechar a decisão:

- Autocadastro.
- Acesso criado por fora.
- Encerrar ou revogar o acesso. Enquanto isso estiver aberto, basta autenticar de novo.

Também ficam fora:

- Envio, listagem, download e status de vídeo.
- Processamento dos frames e geração do ZIP.
- E-mail de erro. Quem avisa é a Video API.
- Fila, MinIO, Redis, monitoria, publicação da imagem e deploy.

## Testes

Cobrir o login que emite o token e a recusa quando a senha está errada ou o usuário não existe. A cobertura de linhas da Auth permanece em pelo menos 80%. O CI já barra o pull request abaixo disso.
