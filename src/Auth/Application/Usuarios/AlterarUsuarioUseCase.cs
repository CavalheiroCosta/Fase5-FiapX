using Auth.Application.Senhas;
using Auth.Domain.Usuarios;

namespace Auth.Application.Usuarios;

public sealed class AlterarUsuarioUseCase(IUsuarioRepository repositorio, ISenhaHasher hasher)
{
    public async Task<Resultado<UsuarioResposta>> ExecutarAsync(
        Guid id,
        string? nome,
        string? email,
        string? senha,
        CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.NaoEncontrado, "Usuário não encontrado.");

        if (string.IsNullOrWhiteSpace(senha))
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.Validacao, "Senha é obrigatória.");

        var emailNormalizado = email?.Trim() ?? "";
        var outro = await repositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (outro is not null && outro.Id != usuario.Id)
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.EmailRepetido, "E-mail já cadastrado.");

        var alterado = usuario.Alterar(nome, email, hasher.GerarHash(senha));
        if (!alterado.Sucesso)
            return Resultado<UsuarioResposta>.Erro(alterado.Falha!.Codigo, alterado.Falha.Mensagem);

        await repositorio.AtualizarAsync(usuario, cancellationToken);
        return Resultado<UsuarioResposta>.Ok(CriarUsuarioUseCase.ParaResposta(usuario));
    }
}
