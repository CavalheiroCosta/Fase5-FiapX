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

    [Fact]
    public void Comecou_passa_para_em_processamento()
    {
        var video = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/a.mp4").Valor!;

        Assert.Equal(EfeitoStatus.Alterado, video.Aplicar(MomentoStatus.Comecou, null));
        Assert.Equal(StatusVideo.EmProcessamento, video.Status);
        Assert.Null(video.CaminhoZip);
        Assert.Equal(EfeitoStatus.Ignorado, video.Aplicar(MomentoStatus.Comecou, null));
    }

    [Fact]
    public void Sucesso_fecha_mesmo_sem_comecou_e_grava_o_zip()
    {
        var video = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/a.mp4").Valor!;

        Assert.Equal(EfeitoStatus.Alterado, video.Aplicar(MomentoStatus.Sucesso, " videos/id/id.zip "));

        Assert.Equal(StatusVideo.Concluido, video.Status);
        Assert.Equal("videos/id/id.zip", video.CaminhoZip);
        Assert.Equal(EfeitoStatus.Ignorado, video.Aplicar(MomentoStatus.Erro, null));
        Assert.Equal(StatusVideo.Concluido, video.Status);
    }

    [Fact]
    public void Erro_fecha_mesmo_sem_comecou()
    {
        var video = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/a.mp4").Valor!;

        Assert.Equal(EfeitoStatus.Alterado, video.Aplicar(MomentoStatus.Erro, "videos/id/id.zip"));

        Assert.Equal(StatusVideo.Erro, video.Status);
        Assert.Null(video.CaminhoZip);
        Assert.Equal(EfeitoStatus.Ignorado, video.Aplicar(MomentoStatus.Sucesso, "videos/id/id.zip"));
        Assert.Equal(StatusVideo.Erro, video.Status);
        Assert.Null(video.CaminhoZip);
    }

    [Fact]
    public void Sucesso_sem_caminho_nao_conclui()
    {
        var video = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/a.mp4").Valor!;

        Assert.Equal(EfeitoStatus.Ignorado, video.Aplicar(MomentoStatus.Sucesso, " "));
        Assert.Equal(StatusVideo.AguardandoProcessamento, video.Status);
        Assert.Equal(EfeitoStatus.Ignorado, video.Aplicar("outro", null));
    }
}
