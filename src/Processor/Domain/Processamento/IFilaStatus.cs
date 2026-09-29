namespace Processor.Domain.Processamento;

public interface IFilaStatus
{
    Task PublicarAsync(Guid id, string momento, string? caminho, CancellationToken cancellationToken);
}
