using Microsoft.Extensions.DependencyInjection;
using Video.Application.Status;

namespace Video.Infra.Filas;

public interface ISessaoStatus
{
    Task ExecutarAsync(CancellationToken cancellationToken);
}

public sealed class SessaoStatus(ICanalStatus canal, IServiceScopeFactory escopos) : ISessaoStatus
{
    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await canal.PrepararAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                var pacote = await canal.ReceberAsync(cancellationToken);
                if (pacote is null)
                    return;

                using var escopo = escopos.CreateScope();
                var caso = escopo.ServiceProvider.GetRequiredService<AplicarStatusUseCase>();
                var confirmacao = await caso.ExecutarAsync(pacote.Corpo, cancellationToken);
                if (confirmacao == Confirmacao.Recolocar)
                    await canal.RecolocarAsync(pacote.Etiqueta, CancellationToken.None);
                else
                    await canal.ConfirmarAsync(pacote.Etiqueta, CancellationToken.None);
            }
        }
        finally
        {
            if (canal is IAsyncDisposable descartavel)
                await descartavel.DisposeAsync();
        }
    }
}
