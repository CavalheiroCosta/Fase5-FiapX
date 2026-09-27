namespace Auth.Domain.Tokens;

public sealed class TokenService(IAssinaturaToken assinatura, TimeProvider relogio)
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);

    public string Emitir(string login, string email)
    {
        var agora = relogio.GetUtcNow();
        return assinatura.Assinar(new IdentidadeAutenticada(login, email), agora, agora.Add(Validade));
    }

    public IdentidadeAutenticada? Ler(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return assinatura.Ler(token.Trim(), relogio.GetUtcNow());
    }
}
