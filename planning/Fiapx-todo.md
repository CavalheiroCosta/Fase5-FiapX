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

O pull request 3 deixou a solution `Fase5-FiapX.slnx` com `src/Auth/Api` e `tst/Auth/Tests`. O workflow `.github/workflows/ci.yml` restaura, compila em Release, gera o OpenCover e `.github/scripts/check-auth-coverage.ps1` barra a cobertura de linhas da Auth abaixo de 80%. Esse job passou no pull request.

### Em seguida, no mesmo workflow

- [ ] SonarCloud com quality gate, no modelo da Fase 4. Exige `SONAR_TOKEN` e as variáveis do projeto.
- [ ] Build da imagem Docker da Auth no CI, sem publicar. A tag local basta para provar que a imagem nasce.

### Fora deste corte

- Publicar a imagem no registro.
- Deploy.

## Auth API

Serviço .NET. Cuida do acesso do usuário e emite o token. Não processa vídeo.

O projeto da API já existe e o host sobe no teste. Login, persistência e token ainda não.

- [ ] Persistir usuário, e-mail, credencial e acesso no PostgreSQL de usuários.
- [ ] Login com usuário e senha. Esse endpoint não exige token.
- [ ] Senha errada ou usuário inexistente não emite token.
- [ ] Token válido carrega o usuário e o e-mail.
- [ ] Testes unitários do login e da recusa, dentro da cobertura de 80%.

### Ainda sem decisão

Não implementar até fechar:

- O usuário se cadastra sozinho, ou o acesso é criado para ele?
- O acesso pode ser encerrado ou revogado, ou basta autenticar de novo?
