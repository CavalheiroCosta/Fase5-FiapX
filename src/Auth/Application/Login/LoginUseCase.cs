using Auth.Application.Senhas;
using Auth.Domain.Tokens;
using Auth.Domain.Usuarios;

namespace Auth.Application.Login;

public sealed record LoginResposta(string Token);

public sealed class LoginUseCase(IUsuarioRepository repositorio, ISenhaHasher hasher, TokenService tokens)
{
    public async Task<Resultado<LoginResposta>> ExecutarAsync(
        string? login,
        string? senha,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
            return Resultado<LoginResposta>.Erro(CodigosFalha.Validacao, "Login e senha são obrigatórios.");

        var usuario = await repositorio.ObterPorLoginAsync(login.Trim(), cancellationToken);
        if (usuario is null || !hasher.Conferir(senha, usuario.SenhaHash))
            return Recusa();

        return Resultado<LoginResposta>.Ok(new LoginResposta(tokens.Emitir(usuario.Login, usuario.Email)));
    }

    private static Resultado<LoginResposta> Recusa() =>
        Resultado<LoginResposta>.Erro(CodigosFalha.CredencialInvalida, "Login ou senha inválidos.");
}
