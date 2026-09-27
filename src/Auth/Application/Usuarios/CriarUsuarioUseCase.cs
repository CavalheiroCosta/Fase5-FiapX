using Auth.Application.Senhas;
using Auth.Domain.Usuarios;

namespace Auth.Application.Usuarios;

public sealed class CriarUsuarioUseCase(IUsuarioRepository repositorio, ISenhaHasher hasher)
{
    public async Task<Resultado<UsuarioResposta>> ExecutarAsync(
        string? login,
        string? senha,
        string? nome,
        string? email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(senha))
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.Validacao, "Senha é obrigatória.");

        var criado = Usuario.Criar(login, nome, email, hasher.GerarHash(senha));
        if (!criado.Sucesso || criado.Valor is null)
            return Resultado<UsuarioResposta>.Erro(criado.Falha!.Codigo, criado.Falha.Mensagem);

        if (await repositorio.ObterPorLoginAsync(criado.Valor.Login, cancellationToken) is not null)
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.LoginRepetido, "Login já cadastrado.");

        if (await repositorio.ObterPorEmailAsync(criado.Valor.Email, cancellationToken) is not null)
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.EmailRepetido, "E-mail já cadastrado.");

        await repositorio.AdicionarAsync(criado.Valor, cancellationToken);
        return Resultado<UsuarioResposta>.Ok(ParaResposta(criado.Valor));
    }

    internal static UsuarioResposta ParaResposta(Usuario usuario) =>
        new(usuario.Id, usuario.Login, usuario.Nome, usuario.Email);
}
