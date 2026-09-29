using Processor.Domain.Processamento;

namespace Processor.Application;

public sealed class ProcessarVideoUseCase(
    IMarcaVideo marca,
    IFilaStatus fila,
    IArmazenamentoProcessamento armazenamento,
    IQuebraVideo quebra,
    MetricasProcessor metricas)
{
    public async Task<string> ExecutarAsync(Guid id, string? caminho, CancellationToken cancellationToken)
    {
        if (!await marca.TentarAsync(id, cancellationToken))
        {
            metricas.Registrar(MomentoStatus.Ignorado);
            return MomentoStatus.Ignorado;
        }

        metricas.Entrar();
        try
        {
            try
            {
                await fila.PublicarAsync(id, MomentoStatus.Comecou, null, cancellationToken);
                metricas.Registrar(MomentoStatus.Comecou);

                await using var video = await armazenamento.BaixarAsync(caminho ?? "", cancellationToken);
                await using var zip = await quebra.QuebrarAsync(video, cancellationToken);
                var caminhoZip = await armazenamento.SalvarZipAsync(id, zip, cancellationToken);
                await fila.PublicarAsync(id, MomentoStatus.Sucesso, caminhoZip, cancellationToken);
                metricas.Registrar(MomentoStatus.Sucesso);
                return MomentoStatus.Sucesso;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _ = ex;
                await PublicarErroAsync(id, cancellationToken);
                return MomentoStatus.Erro;
            }
        }
        finally
        {
            metricas.Sair();
            await RemoverMarcaAsync(id);
        }
    }

    private async Task PublicarErroAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await fila.PublicarAsync(id, MomentoStatus.Erro, null, cancellationToken);
            metricas.Registrar(MomentoStatus.Erro);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _ = ex;
            metricas.Registrar(MomentoStatus.Erro);
        }
    }

    private async Task RemoverMarcaAsync(Guid id)
    {
        try
        {
            await marca.RemoverAsync(id, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
