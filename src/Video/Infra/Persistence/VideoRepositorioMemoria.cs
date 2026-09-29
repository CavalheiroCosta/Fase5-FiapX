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

    public Task<VideoEnviado?> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult(_videos.FirstOrDefault(video => video.Id == id));
    }

    public Task<IReadOnlyList<VideoEnviado>> ListarPorLoginAsync(string login, CancellationToken cancellationToken)
    {
        lock (_trava)
            return Task.FromResult<IReadOnlyList<VideoEnviado>>(_videos.Where(video => video.Login == login).OrderBy(video => video.Id).ToArray());
    }

    public Task AtualizarAsync(VideoEnviado video, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
