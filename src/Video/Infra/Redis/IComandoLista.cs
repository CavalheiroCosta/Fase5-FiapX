namespace Video.Infra.Redis;

public interface IComandoLista
{
    Task<string?> LerAsync(string chave, CancellationToken cancellationToken);

    Task GravarAsync(string chave, string valor, CancellationToken cancellationToken);
}
