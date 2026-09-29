using Video.Domain.Videos;
using VideoEnviado = Video.Domain.Videos.Video;

namespace Video.Tests;

public class VideoRegistroTests
{
    [Fact]
    public void Registra_aguardando_sem_zip()
    {
        var id = Guid.NewGuid();
        var resultado = VideoEnviado.Registrar(id, " ana ", " ana@email.com ", " videos/x/a.mp4 ");

        var video = resultado.Valor!;
        Assert.Equal(id, video.Id);
        Assert.Equal("ana", video.Login);
        Assert.Equal("ana@email.com", video.Email);
        Assert.Equal(StatusVideo.AguardandoProcessamento, video.Status);
        Assert.Equal("videos/x/a.mp4", video.CaminhoOriginal);
        Assert.Null(video.CaminhoZip);
    }

    [Theory]
    [InlineData(true, "ana", "ana@email.com", "videos/x/a.mp4")]
    [InlineData(false, "", "ana@email.com", "videos/x/a.mp4")]
    [InlineData(false, "ana", " ", "videos/x/a.mp4")]
    [InlineData(false, "ana", "ana@email.com", "")]
    public void Recusa_identificador_dono_ou_caminho_vazio(bool idVazio, string login, string email, string caminho)
    {
        var id = idVazio ? Guid.Empty : Guid.NewGuid();
        var resultado = VideoEnviado.Registrar(id, login, email, caminho);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
    }
}
