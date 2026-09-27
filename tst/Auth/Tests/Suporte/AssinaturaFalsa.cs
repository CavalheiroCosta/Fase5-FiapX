using Auth.Domain.Tokens;

namespace Auth.Tests.Suporte;

public sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

public sealed class AssinaturaFalsa : IAssinaturaToken
{
    public int Emissoes { get; private set; }

    public IdentidadeAutenticada? Identidade { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public string Assinar(IdentidadeAutenticada identidade, DateTimeOffset emitidoEm, DateTimeOffset expiraEm)
    {
        Emissoes++;
        Identidade = identidade;
        ExpiraEm = expiraEm;
        return "valido";
    }

    public IdentidadeAutenticada? Ler(string token, DateTimeOffset agora) =>
        token == "valido" ? Identidade : null;
}
