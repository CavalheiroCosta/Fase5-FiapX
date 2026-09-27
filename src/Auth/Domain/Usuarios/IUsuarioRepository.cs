namespace Auth.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken);

    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken);

    Task RemoverAsync(Usuario usuario, CancellationToken cancellationToken);
}
