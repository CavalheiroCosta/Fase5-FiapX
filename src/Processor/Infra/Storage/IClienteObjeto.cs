namespace Processor.Infra.Storage;

public interface IClienteObjeto
{
    Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken);

    Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken);

    Task<Stream> BaixarAsync(string bucket, string chave, CancellationToken cancellationToken);
}
