using Auth.Domain.Usuarios;

namespace Auth.Application.Usuarios;

public sealed class ObterUsuarioUseCase(IUsuarioRepository repositorio)
{
    public async Task<Resultado<UsuarioResposta>> ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return Resultado<UsuarioResposta>.Erro(CodigosFalha.NaoEncontrado, "Usuário não encontrado.");

        return Resultado<UsuarioResposta>.Ok(CriarUsuarioUseCase.ParaResposta(usuario));
    }
}
