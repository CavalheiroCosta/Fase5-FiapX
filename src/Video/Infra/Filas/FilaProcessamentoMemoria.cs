using Video.Domain.Videos;

namespace Video.Infra.Filas;

public sealed record MensagemEnfileirada(Guid Id, string Caminho);

public sealed class FilaProcessamentoMemoria : IFilaProcessamento
{
    private readonly Lock _trava = new();
    private readonly List<MensagemEnfileirada> _mensagens = [];

    public IReadOnlyList<MensagemEnfileirada> Mensagens
    {
        get
        {
            lock (_trava)
                return _mensagens.ToArray();
        }
    }

    public Task PublicarAsync(Guid id, string caminho, CancellationToken cancellationToken)
    {
        lock (_trava)
            _mensagens.Add(new MensagemEnfileirada(id, caminho));

        return Task.CompletedTask;
    }
}
