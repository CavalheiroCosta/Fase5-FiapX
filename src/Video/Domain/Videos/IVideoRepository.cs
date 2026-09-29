namespace Video.Domain.Videos;

public interface IVideoRepository
{
    Task AdicionarAsync(Video video, CancellationToken cancellationToken);

    Task<Video?> ObterAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> ListarPorLoginAsync(string login, CancellationToken cancellationToken);

    Task AtualizarAsync(Video video, CancellationToken cancellationToken);
}
