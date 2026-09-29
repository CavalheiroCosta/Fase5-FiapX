using Processor.Domain.Processamento;

namespace Processor.Infra.Filas;

public sealed class FilaStatus(IPublicadorFila publicador, string nome) : IFilaStatus
{
    public Task PublicarAsync(Guid id, string momento, string? caminho, CancellationToken cancellationToken) =>
        publicador.PublicarAsync(nome, MensagemStatus.Serializar(id, momento, caminho), cancellationToken);
}
