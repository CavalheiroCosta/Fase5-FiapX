using Auth.Application.Senhas;
using Auth.Domain.Tokens;
using Auth.Domain.Usuarios;

namespace Auth.Infra.Persistence;

public sealed class AdministradorSeed(IUsuarioRepository repositorio, ISenhaHasher hasher)
{
    public const string Login = ContaAdministradora.Login;
    public const string Senha = "Adm";
    public const string Nome = "Administrador";
    public const string Email = "adm@adm.com";

    public async Task GarantirAsync(CancellationToken cancellationToken)
    {
        if (await repositorio.ObterPorLoginAsync(Login, cancellationToken) is not null)
            return;

        var criado = Usuario.Criar(Login, Nome, Email, hasher.GerarHash(Senha), administrador: true);
        if (!criado.Sucesso || criado.Valor is null)
            throw new InvalidOperationException(criado.Falha?.Mensagem ?? "Não foi possível gravar a conta administradora.");

        await repositorio.AdicionarAsync(criado.Valor, cancellationToken);
    }
}
