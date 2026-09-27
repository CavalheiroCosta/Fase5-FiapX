using Auth.Domain.Usuarios;

namespace Auth.Application.Usuarios;

public sealed class RemoverUsuarioUseCase(IUsuarioRepository repositorio)
{
    public async Task<Resultado<bool>> ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return Resultado<bool>.Erro(CodigosFalha.NaoEncontrado, "Usuário não encontrado.");

        var remocao = usuario.Remover();
        if (!remocao.Sucesso)
            return remocao;

        await repositorio.RemoverAsync(usuario, cancellationToken);
        return Resultado<bool>.Ok(true);
    }
}
