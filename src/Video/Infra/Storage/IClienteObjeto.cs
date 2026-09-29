namespace Video.Infra.Storage;

public interface IClienteObjeto
{
    Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken);

    Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken);

    Task<Stream> LerAsync(string bucket, string chave, CancellationToken cancellationToken);

    Task ApagarAsync(string bucket, string chave, CancellationToken cancellationToken);
}
