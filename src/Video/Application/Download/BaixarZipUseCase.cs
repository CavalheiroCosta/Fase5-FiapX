using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Application.Download;

public sealed record ZipBaixado(Stream Conteudo, string NomeArquivo);

public sealed class BaixarZipUseCase(
    ILeitorToken leitor,
    IVideoRepository repositorio,
    IArmazenamentoVideo armazenamento,
    MetricasVideo metricas)
{
    public async Task<Resultado<ZipBaixado>> ExecutarAsync(string? token, Guid id, CancellationToken cancellationToken)
    {
        if (leitor.Ler(token) is not { } identidade)
            return Recusar(CodigosFalha.AcessoRecusado, "Acesso recusado.");

        var video = await repositorio.ObterAsync(id, cancellationToken);
        if (video is null
            || video.Login != identidade.Login
            || video.Status != StatusVideo.Concluido
            || string.IsNullOrWhiteSpace(video.CaminhoZip))
            return Recusar(CodigosFalha.NaoEncontrado, "Vídeo não encontrado.");

        try
        {
            var conteudo = await armazenamento.AbrirAsync(video.CaminhoZip, cancellationToken);
            metricas.RegistrarDownload(MetricasVideo.ResultadoEntregue);
            return Resultado<ZipBaixado>.Ok(new ZipBaixado(conteudo, $"{id:D}.zip"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            return Recusar(CodigosFalha.Storage, "Não foi possível ler o arquivo.");
        }
    }

    private Resultado<ZipBaixado> Recusar(string codigo, string mensagem)
    {
        metricas.RegistrarDownload(MetricasVideo.ResultadoRecusado);
        return Resultado<ZipBaixado>.Erro(codigo, mensagem);
    }
}
