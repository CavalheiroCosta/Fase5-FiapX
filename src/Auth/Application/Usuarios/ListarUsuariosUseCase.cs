using Auth.Domain.Usuarios;

namespace Auth.Application.Usuarios;

public sealed class ListarUsuariosUseCase(IUsuarioRepository repositorio)
{
    public async Task<IReadOnlyList<UsuarioResposta>> ExecutarAsync(CancellationToken cancellationToken)
    {
        var usuarios = await repositorio.ListarAsync(cancellationToken);
        return usuarios.Select(CriarUsuarioUseCase.ParaResposta).ToArray();
    }
}
