# Auth.Application

Casos de uso do cadastro e do login da Auth API. Projeto `src/Auth/Application`.

Cria, consulta, lista, altera e remove usuários. O `LoginUseCase` confere a senha com `ISenhaHasher.Conferir` e, se estiver correta, pede o token ao `TokenService`. Senha errada e usuário inexistente devolvem a mesma recusa e não emitem token.

A persistência entra por `IUsuarioRepository`. A resposta não leva senha nem hash.

O corte está em [`planning/auth.md`](../../../planning/auth.md).
