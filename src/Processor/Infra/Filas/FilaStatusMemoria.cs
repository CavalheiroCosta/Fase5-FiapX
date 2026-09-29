using Processor.Domain.Processamento;

namespace Processor.Infra.Filas;

public sealed record StatusPublicado(Guid Id, string Momento, string? Caminho);

public sealed class FilaStatusMemoria : IFilaStatus
{
    private readonly Lock _trava = new();
    private readonly List<StatusPublicado> _mensagens = [];

    public IReadOnlyList<StatusPublicado> Mensagens
    {
        get
        {
            lock (_trava)
                return _mensagens.ToArray();
        }
    }

    public Task PublicarAsync(Guid id, string momento, string? caminho, CancellationToken cancellationToken)
    {
        lock (_trava)
            _mensagens.Add(new StatusPublicado(id, momento, caminho));

        return Task.CompletedTask;
    }
}
