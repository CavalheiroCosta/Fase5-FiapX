using Auth.Domain.Tokens;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class TokenServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Emite_login_email_e_validade_de_trinta_minutos()
    {
        var assinatura = new AssinaturaFalsa();
        var servico = new TokenService(assinatura, new RelogioFixo(Agora));

        var token = servico.Emitir("ana", "ana@email.com");

        Assert.Equal("valido", token);
        Assert.Equal("ana", assinatura.Identidade!.Login);
        Assert.Equal("ana@email.com", assinatura.Identidade.Email);
        Assert.Equal(Agora.Add(TokenService.Validade), assinatura.ExpiraEm);
        Assert.Equal(TimeSpan.FromMinutes(30), TokenService.Validade);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Token_ausente_nao_produz_identidade(string? token)
    {
        var servico = new TokenService(new AssinaturaFalsa(), new RelogioFixo(Agora));

        Assert.Null(servico.Ler(token));
    }

    [Fact]
    public void Le_o_token_emitido_e_recusa_o_invalido()
    {
        var assinatura = new AssinaturaFalsa();
        var servico = new TokenService(assinatura, new RelogioFixo(Agora));
        var token = servico.Emitir("ana", "ana@email.com");

        var identidade = servico.Ler("  " + token + "  ");

        Assert.Equal("ana", identidade!.Login);
        Assert.Equal("ana@email.com", identidade.Email);
        Assert.Null(servico.Ler("invalido"));
    }
}
