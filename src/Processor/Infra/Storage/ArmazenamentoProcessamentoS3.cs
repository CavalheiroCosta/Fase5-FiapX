using Processor.Domain.Processamento;

namespace Processor.Infra.Storage;

public sealed class ArmazenamentoProcessamentoS3(IClienteObjeto cliente, string bucket) : IArmazenamentoProcessamento
{
    public Task<Stream> BaixarAsync(string caminho, CancellationToken cancellationToken) =>
        cliente.BaixarAsync(bucket, CaminhoZip.Chave(bucket, caminho), cancellationToken);

    public async Task<string> SalvarZipAsync(Guid id, Stream conteudo, CancellationToken cancellationToken)
    {
        var caminho = CaminhoZip.Montar(bucket, id);
        await cliente.GravarAsync(bucket, CaminhoZip.Chave(bucket, caminho), conteudo, cancellationToken);
        return caminho;
    }
}
