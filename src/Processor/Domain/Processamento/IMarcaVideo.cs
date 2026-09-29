namespace Processor.Domain.Processamento;

public interface IMarcaVideo
{
    Task<bool> TentarAsync(Guid id, CancellationToken cancellationToken);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken);
}
