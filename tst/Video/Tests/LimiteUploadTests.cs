using Microsoft.Extensions.Configuration;

namespace Video.Tests;

public class LimiteUploadTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public void Usa_o_padrao_quando_o_limite_nao_serve(string? valor)
    {
        var dados = new Dictionary<string, string?>();
        if (valor is not null)
            dados["Upload:LimiteBytes"] = valor;

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(dados).Build();

        Assert.Equal(Video.Application.Envio.EnviarVideoUseCase.LimitePadraoBytes, LimiteUpload.Ler(configuracao));
    }

    [Fact]
    public void Le_o_limite_configurado()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Upload:LimiteBytes"] = "42"
        }).Build();

        Assert.Equal(42, LimiteUpload.Ler(configuracao));
    }
}
