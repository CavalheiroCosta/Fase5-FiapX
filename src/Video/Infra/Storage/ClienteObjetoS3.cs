using Amazon.S3;
using Amazon.S3.Model;

namespace Video.Infra.Storage;

public sealed class ClienteObjetoS3 : IClienteObjeto, IDisposable
{
    private readonly IAmazonS3 _cliente;

    public ClienteObjetoS3(string serviceUrl, string accessKey, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new ArgumentException("A URL do storage é obrigatória.", nameof(serviceUrl));

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        };
        _cliente = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        try
        {
            await _cliente.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            return;
        }
    }

    public Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken)
    {
        var pedido = new PutObjectRequest
        {
            BucketName = bucket,
            Key = chave,
            InputStream = conteudo,
            AutoCloseStream = false
        };
        return _cliente.PutObjectAsync(pedido, cancellationToken);
    }

    public async Task<Stream> LerAsync(string bucket, string chave, CancellationToken cancellationToken)
    {
        using var resposta = await _cliente.GetObjectAsync(bucket, chave, cancellationToken);
        var memoria = new MemoryStream();
        await resposta.ResponseStream.CopyToAsync(memoria, cancellationToken);
        memoria.Position = 0;
        return memoria;
    }

    public Task ApagarAsync(string bucket, string chave, CancellationToken cancellationToken) =>
        _cliente.DeleteObjectAsync(bucket, chave, cancellationToken);

    public void Dispose() => _cliente.Dispose();
}
