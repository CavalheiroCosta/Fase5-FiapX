# TODO — FIAP X

O que vamos fazer, na ordem. O desenho completo está em `planning/planning.md`.

Neste momento o foco é o CI e a Auth API. Video API, processor, filas, MinIO, Redis e monitoria ficam para depois.

## CI

Um workflow no monorepo, em `.github/workflows/ci.yml`. A referência é o job de CI da Fase 4 (Billing e Execution): build, teste com cobertura, SonarCloud e build da imagem. Publicar a imagem e fazer deploy só acontecem no push da `main`, e ficam fora deste corte.

O primeiro corte prova a Auth. O pull request quebra se ela não compila ou se a cobertura fica abaixo de 80%. Sonar e imagem entram depois, no mesmo workflow.

### Agora

- [x] Solution .NET 10, como na Fase 4, com o projeto da Auth API e o projeto de testes dela.
- [x] O workflow dispara em pull request para a `main`, em push na `main` e por `workflow_dispatch`.
- [x] Passos: checkout, setup do .NET 10, restore e build em Release.
- [x] Testes da Auth com cobertura no formato OpenCover.
- [x] O próprio workflow lê o relatório e falha se a cobertura de linhas da Auth for menor que 80%. Não depende do Sonar para barrar o pull request.
- [x] Esse job termina antes do merge.

### Em seguida, no mesmo workflow

- [x] SonarCloud com quality gate, no modelo da Fase 4. Exige `SONAR_TOKEN` e as variáveis do projeto. O workflow espera o quality gate. A cobertura de linhas da Auth continua barrando o pull request sem depender do Sonar.
- [x] Build da imagem Docker da Auth no CI, sem publicar. A tag local `fase5-auth:ci` prova que a imagem nasce. Publicação e deploy continuam fora.

### Fora deste corte

- Publicar a imagem no registro.
- Deploy.

## Auth API

Serviço .NET. Cadastra os usuários, cuida do acesso e emite o token. Não processa vídeo. O guia deste corte está em `planning/auth.md`.

A conta administradora é login `Adm`, senha `Adm`, nome `Administrador` e e-mail `adm@adm.com`. Ela cadastra os demais usuários, com login, senha, nome e e-mail. Não há autocadastro.

### Agora

- [ ] Camadas Api, Application, Domain e Infra, no modelo da Fase 4.
- [ ] Cadastro sem token: criar, consultar, listar, alterar e remover.
- [ ] Compose com o PostgreSQL de usuários. A Auth grava nele. Banco de vídeos, fila, MinIO, Redis e monitoria ficam fora. A decisão está em `docs/adrs/ADR-003-postgres-usuarios-no-compose.md`.
- [ ] Persistir Guid, login, nome, e-mail, hash da senha e acesso nesse banco, atrás de uma interface. O Guid nasce na gravação. A senha usa o PasswordHasher do .NET. A conta `Adm` nasce com o serviço.
- [ ] Testes unitários do cadastro e das recusas, dentro da cobertura de 80%.

### Em seguida

- [ ] Login com usuário e senha. Esse endpoint não exige token.
- [ ] Senha errada ou usuário inexistente não emite token.
- [ ] Token válido carrega o usuário e o e-mail.
- [ ] Leitura do token, recuperando o usuário e o e-mail.
- [ ] Cadastro passa a exigir o token da conta `Adm`.
- [ ] Testes unitários do login, da recusa e da leitura do token, dentro da cobertura de 80%.

### Ainda sem decisão

- O formato do token entra numa ADR antes da emissão. O cadastro não espera essa escolha.
- O acesso pode ser encerrado ou revogado, ou basta autenticar de novo? Não implementar revogação enquanto isso estiver aberto.
