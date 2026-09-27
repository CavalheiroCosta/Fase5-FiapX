using Auth.Domain.Tokens;
using Auth.Infra.Tokens;

namespace Auth.Tests;

public class AssinaturaHmacTests
{
    private const string Chave = "0123456789abcdef0123456789abcdef";

    private static readonly DateTimeOffset Agora = DateTimeOffset.FromUnixTimeSeconds(1_758_000_000);

    [Fact]
    public void Le_login_e_email_e_recusa_token_invalido_ou_expirado()
    {
        var assinatura = new AssinaturaHmac(Chave);
        var token = assinatura.Assinar(new IdentidadeAutenticada("ana", "ana@email.com"), Agora, Agora.AddMinutes(30));

        var identidade = assinatura.Ler(token, Agora);

        Assert.Equal("ana", identidade!.Login);
        Assert.Equal("ana@email.com", identidade.Email);
        Assert.Null(assinatura.Ler(token[..^1] + (token[^1] == 'a' ? "b" : "a"), Agora));
        Assert.Null(assinatura.Ler("nao-e-jwt", Agora));
        Assert.Null(assinatura.Ler(token, Agora.AddMinutes(31)));
        Assert.NotNull(assinatura.Ler(token, Agora.AddMinutes(30).AddSeconds(-1)));
        Assert.Null(assinatura.Ler(token, Agora.AddMinutes(30)));
    }

    [Fact]
    public void Recusa_chave_curta()
    {
        var erro = Assert.Throws<ArgumentException>(() => new AssinaturaHmac("curta"));
        Assert.Equal("chave", erro.ParamName);
    }
}
