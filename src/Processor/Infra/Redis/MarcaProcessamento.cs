using Processor.Domain.Processamento;

namespace Processor.Infra.Redis;

public sealed class MarcaProcessamento(IComandoMarca comando) : IMarcaVideo
{
    public static string Chave(Guid id) => $"marca:{id:D}";

    public Task<bool> TentarAsync(Guid id, CancellationToken cancellationToken) =>
        comando.DefinirSeAusenteAsync(Chave(id), cancellationToken);

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken) =>
        comando.RemoverAsync(Chave(id), cancellationToken);
}
