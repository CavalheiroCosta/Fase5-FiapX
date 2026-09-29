namespace Video.Infra;

public interface IPreparacaoExterna
{
    Task PrepararAsync(CancellationToken cancellationToken);
}
