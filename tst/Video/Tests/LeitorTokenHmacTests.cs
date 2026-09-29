using Video.Infra.Tokens;
using Video.Tests.Suporte;

namespace Video.Tests;

public class LeitorTokenHmacTests
{
    private const string Chave = "0123456789abcdef0123456789abcdef";

    private static readonly DateTimeOffset Agora = DateTimeOffset.FromUnixTimeSeconds(1_758_000_000);

    [Fact]
    public void Le_login_e_email_no_formato_da_auth()
    {
        var leitor = new LeitorTokenHmac(Chave, new RelogioFixo(Agora));
        var token = TokenDeTeste.Emitir(Chave, "ana", "ana@email.com", Agora, Agora.AddMinutes(30));

        var identidade = leitor.Ler(token);

        Assert.Equal("ana", identidade!.Login);
        Assert.Equal("ana@email.com", identidade.Email);
        Assert.Null(leitor.Ler(null));
        Assert.Null(leitor.Ler("  "));
        Assert.Null(leitor.Ler("nao-e-jwt"));
        Assert.Null(leitor.Ler(token[..^1] + (token[^1] == 'a' ? "b" : "a")));
        Assert.Null(new LeitorTokenHmac(Chave, new RelogioFixo(Agora.AddMinutes(31))).Ler(token));
        Assert.NotNull(new LeitorTokenHmac(Chave, new RelogioFixo(Agora.AddMinutes(30).AddSeconds(-1))).Ler(token));
        Assert.Null(new LeitorTokenHmac(Chave, new RelogioFixo(Agora.AddMinutes(30))).Ler(token));
    }

    [Fact]
    public void Recusa_token_sem_claim_ou_assinado_com_outra_chave()
    {
        var leitor = new LeitorTokenHmac(Chave, new RelogioFixo(Agora));
        var semEmail = TokenDeTeste.Emitir(Chave, "ana", null, Agora, Agora.AddMinutes(30));
        var semLogin = TokenDeTeste.Emitir(Chave, null, "ana@email.com", Agora, Agora.AddMinutes(30));
        var emBranco = TokenDeTeste.Emitir(Chave, " ", "ana@email.com", Agora, Agora.AddMinutes(30));
        var outraChave = TokenDeTeste.Emitir(
            "abcdef0123456789abcdef0123456789",
            "ana",
            "ana@email.com",
            Agora,
            Agora.AddMinutes(30));

        Assert.Null(leitor.Ler(semEmail));
        Assert.Null(leitor.Ler(semLogin));
        Assert.Null(leitor.Ler(emBranco));
        Assert.Null(leitor.Ler(outraChave));
    }

    [Fact]
    public void Recusa_chave_curta()
    {
        var erro = Assert.Throws<ArgumentException>(() => new LeitorTokenHmac("curta", TimeProvider.System));
        Assert.Equal("chave", erro.ParamName);
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
