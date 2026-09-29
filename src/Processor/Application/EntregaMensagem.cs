using Processor.Domain.Processamento;

namespace Processor.Application;

public enum Confirmacao
{
    Confirmar,
    Recolocar
}

public sealed class EntregaMensagem(ProcessarVideoUseCase caso)
{
    public async Task<Confirmacao> TratarAsync(ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken)
    {
        var mensagem = MensagemEntrada.Ler(corpo.Span);
        if (!mensagem.Legivel)
            return Confirmacao.Confirmar;

        try
        {
            await caso.ExecutarAsync(mensagem.Id, mensagem.Caminho, cancellationToken);
            return Confirmacao.Confirmar;
        }
        catch (OperationCanceledException)
        {
            return Confirmacao.Recolocar;
        }
    }
}
