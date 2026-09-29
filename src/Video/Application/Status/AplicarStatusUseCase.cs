using Video.Domain.Videos;

namespace Video.Application.Status;

public enum Confirmacao
{
    Confirmar,
    Recolocar
}

public sealed class AplicarStatusUseCase(IVideoRepository repositorio, IListaVideos lista, MetricasVideo metricas)
{
    public async Task<Confirmacao> ExecutarAsync(ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken)
    {
        var mensagem = MensagemStatus.Ler(corpo.Span);
        if (!mensagem.Legivel)
        {
            metricas.Registrar(MomentoStatus.Ignorado);
            return Confirmacao.Confirmar;
        }

        try
        {
            var video = await repositorio.ObterAsync(mensagem.Id, cancellationToken);
            if (video is null)
            {
                metricas.Registrar(MomentoStatus.Ignorado);
                return Confirmacao.Confirmar;
            }

            var efeito = video.Aplicar(mensagem.Momento, mensagem.Caminho);
            if (efeito == EfeitoStatus.Alterado)
            {
                await repositorio.AtualizarAsync(video, cancellationToken);
                try
                {
                    await lista.AtualizarAsync(video.Login, ItemLista.De(video), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _ = ex;
                }
            }

            metricas.Registrar(efeito == EfeitoStatus.Alterado ? mensagem.Momento! : MomentoStatus.Ignorado);
            return Confirmacao.Confirmar;
        }
        catch (OperationCanceledException)
        {
            return Confirmacao.Recolocar;
        }
        catch (Exception ex)
        {
            _ = ex;
            metricas.Registrar(MomentoStatus.Ignorado);
            return Confirmacao.Confirmar;
        }
    }
}
