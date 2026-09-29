namespace Processor.Domain.Processamento;

public interface IArmazenamentoProcessamento
{
    Task<Stream> BaixarAsync(string caminho, CancellationToken cancellationToken);

    Task<string> SalvarZipAsync(Guid id, Stream conteudo, CancellationToken cancellationToken);
}
