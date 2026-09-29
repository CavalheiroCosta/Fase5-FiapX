using Processor.Domain.Processamento;

namespace Processor.Infra.Redis;

public sealed class MarcaMemoria : IMarcaVideo
{
    private readonly Lock _trava = new();
    private readonly HashSet<Guid> _marcas = [];

    public Task<bool> TentarAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult(_marcas.Add(id));
    }

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_trava)
            _marcas.Remove(id);

        return Task.CompletedTask;
    }

    public bool Contem(Guid id)
    {
        lock (_trava)
            return _marcas.Contains(id);
    }
}
