namespace Video.Domain.Videos;

public interface IFilaProcessamento
{
    Task PublicarAsync(Guid id, string caminho, CancellationToken cancellationToken);
}
