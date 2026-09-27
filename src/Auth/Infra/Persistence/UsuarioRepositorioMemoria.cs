using Auth.Domain.Usuarios;

namespace Auth.Infra.Persistence;

public sealed class UsuarioRepositorioMemoria : IUsuarioRepository
{
    private readonly Lock _trava = new();
    private readonly List<Usuario> _usuarios = [];

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult(_usuarios.FirstOrDefault(item => item.Id == id));
    }

    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult(_usuarios.FirstOrDefault(item => item.Login == login));
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult(_usuarios.FirstOrDefault(item => item.Email == email));
    }

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.ToArray());
    }

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            usuario.AtribuirId(Guid.NewGuid());
            _usuarios.Add(usuario);
        }

        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task RemoverAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        lock (_trava)
            _usuarios.RemoveAll(item => item.Id == usuario.Id);

        return Task.CompletedTask;
    }
}
