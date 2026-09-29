namespace Processor.Infra.Redis;

public interface IComandoMarca
{
    Task<bool> DefinirSeAusenteAsync(string chave, CancellationToken cancellationToken);

    Task RemoverAsync(string chave, CancellationToken cancellationToken);
}
