using Video.Domain.Videos;
using Video.Infra;

namespace Video.Infra.Storage;

public sealed class ArmazenamentoS3(IClienteObjeto cliente, string bucket) : IArmazenamentoVideo, IPreparacaoExterna
{
    public async Task<string> SalvarAsync(Guid id, string nomeArquivo, Stream conteudo, CancellationToken cancellationToken)
    {
        var caminho = CaminhoVideo.Montar(bucket, id, nomeArquivo);
        await cliente.GravarAsync(bucket, CaminhoVideo.Chave(bucket, caminho), conteudo, cancellationToken);
        return caminho;
    }

    public Task RemoverAsync(string caminho, CancellationToken cancellationToken) =>
        cliente.ApagarAsync(bucket, CaminhoVideo.Chave(bucket, caminho), cancellationToken);

    public Task PrepararAsync(CancellationToken cancellationToken) =>
        cliente.GarantirBucketAsync(bucket, cancellationToken);
}
