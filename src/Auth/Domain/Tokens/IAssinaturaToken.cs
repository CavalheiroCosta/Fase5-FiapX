namespace Auth.Domain.Tokens;

public interface IAssinaturaToken
{
    string Assinar(IdentidadeAutenticada identidade, DateTimeOffset emitidoEm, DateTimeOffset expiraEm);

    IdentidadeAutenticada? Ler(string token, DateTimeOffset agora);
}
