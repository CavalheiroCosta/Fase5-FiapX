using Auth.Application.Senhas;
using Auth.Domain.Usuarios;

namespace Auth.Tests.Suporte;

public sealed class RepositorioFalso : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = [];

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_usuarios.FirstOrDefault(item => item.Id == id));

    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken) =>
        Task.FromResult(_usuarios.FirstOrDefault(item => item.Login == login));

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_usuarios.FirstOrDefault(item => item.Email == email));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.ToArray());

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        usuario.AtribuirId(Guid.NewGuid());
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task RemoverAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        _usuarios.RemoveAll(item => item.Id == usuario.Id);
        return Task.CompletedTask;
    }
}

public sealed class HasherFalso : ISenhaHasher
{
    public string GerarHash(string senha) => "hash:" + senha;

    public bool Conferir(string senha, string senhaHash) => senhaHash == GerarHash(senha);
}
