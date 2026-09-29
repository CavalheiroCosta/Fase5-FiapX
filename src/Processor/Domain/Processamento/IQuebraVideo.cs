namespace Processor.Domain.Processamento;

public interface IQuebraVideo
{
    Task<Stream> QuebrarAsync(Stream video, CancellationToken cancellationToken);
}
