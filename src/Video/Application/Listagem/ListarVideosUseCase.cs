using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Application.Listagem;

public sealed class ListarVideosUseCase(
    ILeitorToken leitor,
    IListaVideos lista,
    IVideoRepository repositorio,
    MetricasVideo metricas)
{
    public async Task<Resultado<IReadOnlyList<ItemLista>>> ExecutarAsync(string? token, CancellationToken cancellationToken)
    {
        if (leitor.Ler(token) is not { } identidade)
            return Resultado<IReadOnlyList<ItemLista>>.Erro(CodigosFalha.AcessoRecusado, "Acesso recusado.");

        IReadOnlyList<ItemLista>? itens;
        try
        {
            itens = await lista.ObterAsync(identidade.Login, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            itens = null;
        }

        if (itens is null)
        {
            var videos = await repositorio.ListarPorLoginAsync(identidade.Login, cancellationToken);
            itens = videos.Select(ItemLista.De).ToArray();
            try
            {
                await lista.GuardarAsync(identidade.Login, itens, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ = ex;
            }

            metricas.RegistrarListagem(MetricasVideo.OrigemPostgres);
        }
        else
        {
            metricas.RegistrarListagem(MetricasVideo.OrigemRedis);
        }

        return Resultado<IReadOnlyList<ItemLista>>.Ok(itens.Select(SemZipForaDeConcluido).ToArray());
    }

    private static ItemLista SemZipForaDeConcluido(ItemLista item) =>
        item.Status == StatusVideo.Concluido ? item : item with { CaminhoZip = null };
}
