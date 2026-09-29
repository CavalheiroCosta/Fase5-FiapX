using Video.Domain.Videos;
using VideoEnviado = Video.Domain.Videos.Video;

namespace Video.Infra.Persistence;

public sealed class VideoRepositorioMemoria : IVideoRepository
{
    private readonly Lock _trava = new();
    private readonly List<VideoEnviado> _videos = [];

    public IReadOnlyList<VideoEnviado> Listar()
    {
        lock (_trava)
            return _videos.ToArray();
    }

    public Task AdicionarAsync(VideoEnviado video, CancellationToken cancellationToken)
    {
        if (video.Id == Guid.Empty)
            throw new InvalidOperationException("O identificador do vídeo nasce antes da gravação.");

        lock (_trava)
            _videos.Add(video);

        return Task.CompletedTask;
    }
}
