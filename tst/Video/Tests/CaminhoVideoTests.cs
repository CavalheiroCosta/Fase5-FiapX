using Video.Domain.Videos;

namespace Video.Tests;

public class CaminhoVideoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    public void Nome_inseguro_nao_entra_no_caminho(string? nome)
    {
        Assert.Null(CaminhoVideo.NomeSeguro(nome));
    }

    [Fact]
    public void Monta_caminho_com_o_nome_do_arquivo()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var caminho = CaminhoVideo.Montar(CaminhoVideo.BucketPadrao, id, "pasta/aula.mp4");

        Assert.Equal($"videos/{id:D}/aula.mp4", caminho);
        Assert.Equal($"{id:D}/aula.mp4", CaminhoVideo.Chave(CaminhoVideo.BucketPadrao, caminho));
    }

    [Fact]
    public void Caminho_invalido_falha()
    {
        Assert.Throws<ArgumentException>(() => CaminhoVideo.Montar("", Guid.NewGuid(), "aula.mp4"));
        Assert.Throws<ArgumentException>(() => CaminhoVideo.Montar("videos", Guid.Empty, "aula.mp4"));
        Assert.Throws<ArgumentException>(() => CaminhoVideo.Chave("videos", "outro/aula.mp4"));
        Assert.Throws<ArgumentException>(() => CaminhoVideo.Chave("videos", "videos/"));
    }
}
