namespace Video.Domain.Videos;

public interface IEnviadorEmail
{
    Task EnviarErroAsync(string destinatario, Guid id, CancellationToken cancellationToken);
}
