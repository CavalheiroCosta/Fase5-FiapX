using Auth.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infra.Persistence;

public sealed class UsuarioRepository(AuthDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Usuarios.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken) =>
        db.Usuarios.FirstOrDefaultAsync(item => item.Login == login, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Usuarios.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Usuarios.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        usuario.AtribuirId(Guid.NewGuid());
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task RemoverAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync(cancellationToken);
    }
}
