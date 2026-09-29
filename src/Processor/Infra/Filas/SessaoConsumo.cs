using Processor.Application;

namespace Processor.Infra.Filas;

public interface ISessaoConsumo
{
    Task ExecutarAsync(CancellationToken cancellationToken);
}

public sealed class SessaoConsumo(ICanalConsumo canal, EntregaMensagem entrega) : ISessaoConsumo
{
    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        await canal.PrepararAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            var pacote = await canal.ReceberAsync(cancellationToken);
            if (pacote is null)
                return;

            var confirmacao = await entrega.TratarAsync(pacote.Corpo, cancellationToken);
            if (confirmacao == Confirmacao.Recolocar)
                await canal.RecolocarAsync(pacote.Etiqueta, CancellationToken.None);
            else
                await canal.ConfirmarAsync(pacote.Etiqueta, CancellationToken.None);
        }
    }
}
