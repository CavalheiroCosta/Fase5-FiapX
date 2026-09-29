namespace Video.Domain.Videos;

public interface IVideoRepository
{
    Task AdicionarAsync(Video video, CancellationToken cancellationToken);
}
