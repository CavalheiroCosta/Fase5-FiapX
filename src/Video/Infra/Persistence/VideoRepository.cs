using Microsoft.EntityFrameworkCore;
using Video.Domain.Videos;
using VideoEnviado = Video.Domain.Videos.Video;

namespace Video.Infra.Persistence;

public sealed class VideoRepository(VideoDbContext db) : IVideoRepository
{
    public async Task AdicionarAsync(VideoEnviado video, CancellationToken cancellationToken)
    {
        if (video.Id == Guid.Empty)
            throw new InvalidOperationException("O identificador do vídeo nasce antes da gravação.");

        db.Videos.Add(video);
        await db.SaveChangesAsync(cancellationToken);
    }
}
